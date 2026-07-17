# Terraform

## Trois dossiers Terraform, un seul dans le flux actif

Le dépôt contient trois configurations Terraform, issues de trois itérations successives du projet. Pour éviter toute ambiguïté à la lecture, voici leur statut exact :

| Dossier | Statut | Rôle |
| --- | --- | --- |
| `infra/kubernetes/terraform/` | **Actif** — utilisé par Ansible | Prépare le namespace et le PVC SQLite sur minikube (étape 8 du sujet) |
| `infra/terraform/` | Non utilisé par le pipeline actif | Variante « tout Docker » (conteneurs `docker_container` + Nginx, sans Kubernetes) explorée avant de basculer sur minikube. Conservée à titre de référence, ne fait pas partie du chemin de déploiement décrit dans ce document. |
| `infra/helm/terraform/` | Non utilisé par le pipeline actif | Variante où Terraform pilote directement `helm_release`. Non retenue : c'est finalement Ansible qui pilote Helm (voir [`architecture.md`](architecture.md#rôle-dansible) pour la justification), pour pouvoir redéployer l'application sans toucher à l'état Terraform qui porte le PVC. Cette configuration contient un output `database_host = "postgres-svc"` qui est un reliquat sans rapport avec ce projet (SQLite uniquement, aucun Postgres) — à ignorer/supprimer, listé ici pour honnêteté plutôt que caché. |

La suite de ce document décrit uniquement `infra/kubernetes/terraform/`.

## Pourquoi limiter Terraform au namespace + au PVC

Terraform gère ce qui doit **survivre** aux déploiements applicatifs répétés : le namespace et le volume persistant SQLite. Si Terraform gérait aussi le déploiement de l'application (Deployment, Service...), chaque `terraform apply` risquerait de recréer ou de perturber des ressources qui doivent au contraire être mises à jour de façon incrémentale par Helm (nouvelle image, nouveau tag, montée en charge). Séparer les deux outils fait porter à chacun un cycle de vie différent : Terraform = infrastructure durable, Ansible/Helm = déploiement applicatif renouvelable.

## Ressources gérées

- `kubernetes_namespace_v1.app` — namespace de l'application, nommé `locatic-<environment>` par défaut (ex. `locatic-dev`).
- `kubernetes_persistent_volume_claim_v1.sqlite` — PVC `ReadWriteOnce` pour le fichier SQLite, taille et `storageClassName` configurables. `wait_until_bound = false` : le PVC est préparé sans attendre qu'un pod le consomme, puisque c'est Helm qui créera ce pod ensuite.

## Variables (`variables.tf`)

| Variable | Défaut | Rôle |
| --- | --- | --- |
| `kubeconfig_path` | `~/.kube/config` | Emplacement du kubeconfig local |
| `kube_context` | `minikube` | Contexte `kubectl` à utiliser |
| `environment` | `dev` | `dev` ou `prod`, dérive le nom du namespace |
| `namespace` | `""` | Namespace explicite (sinon dérivé de `environment`) |
| `app_name` | `locatic` | Nom logique de l'application |
| `sqlite_pvc_name` | `locatic-sqlite` | Nom du PVC |
| `sqlite_storage` | `1Gi` | Taille du volume |
| `sqlite_mount_path` | `/app/data` | Chemin de montage dans le conteneur |
| `sqlite_db_file` | `locatic.db` | Nom du fichier SQLite |
| `storage_class` | `""` | StorageClass (vide = classe par défaut du cluster ; `standard` sur minikube) |

Un exemple de valeurs se trouve dans `infra/kubernetes/terraform/terraform.tfvars.example` — à copier en `terraform.tfvars` (fichier ignoré par Git) avant de lancer `terraform apply`. Aucune valeur secrète n'y figure : SQLite ne nécessite ni mot de passe ni utilisateur.

## Outputs utiles

- `namespace` : nom du namespace créé.
- `sqlite_pvc_name` : nom du PVC, à donner à Helm (`persistence.existingClaim`).
- `sqlite_connection_string` : chaîne de connexion EF Core prête à l'emploi (`Data Source=/app/data/locatic.db`).
- `helm_deploy_command` : commande `helm upgrade --install` complète, prête à copier-coller.
- `handoff` : objet JSON regroupant toutes les informations ci-dessus, **c'est celui-ci qu'Ansible consomme** (`terraform output -json handoff`) pour poursuivre l'installation sans dupliquer la configuration.

## Gestion de l'état

L'état est **local** (pas de backend distant configuré, adapté à un usage mono-poste comme demandé par le sujet) et systématiquement ignoré par Git : `.gitignore` exclut `*.tfstate`, `*.tfstate.*`, `**/.terraform/*` et `*.tfvars` (sauf les fichiers `*.tfvars.example`). Aucun fichier d'état ni variable réelle n'est donc jamais commité.

## Initialiser, planifier, appliquer

```bash
cd infra/kubernetes/terraform
cp terraform.tfvars.example terraform.tfvars   # à adapter si besoin
terraform init
terraform validate
terraform plan
terraform apply
terraform output -json handoff                 # vérifier ce qu'Ansible va lire
```

Pour détruire l'infrastructure locale (⚠️ supprime aussi le PVC, donc les données SQLite) :

```bash
terraform destroy
```
