# Ansible

## Rôle du playbook

`infra/ansible/site.yml` est le point d'entrée unique de l'orchestration locale. Il tourne exclusivement sur `localhost` (`hosts: localhost`, `ansible_connection: local` dans `inventory.yml`) : il n'y a pas de serveur distant à provisionner, seulement une séquence de commandes (`terraform`, `kubectl`, `helm`, `minikube`) à exécuter dans le bon ordre sur le poste du développeur. C'est le rôle qu'Ansible joue ici : pas de gestion de parc de machines, mais une orchestration reproductible et idempotente de la suite d'actions locales, avec des vérifications explicites à chaque étape plutôt qu'un enchaînement de scripts shell.

## Étapes orchestrées (`site.yml`)

```yaml
roles:
  - prereqs
  - terraform_output
  - deploy_monitoring
  - deploy_app
```

1. **`prereqs`** — vérifie que les binaires requis (`terraform`, `kubectl`, `helm`, `minikube`) sont installés, que minikube est démarré (`minikube status`), que `kubectl` a un contexte courant et peut effectivement joindre l'API du cluster (`kubectl cluster-info`). Chaque vérification échoue avec un message explicite plutôt que de laisser une erreur cryptique survenir plus loin dans le playbook.
2. **`terraform_output`** — exécute `terraform output -json handoff` dans `infra/kubernetes/terraform` et expose le résultat comme fait Ansible (`tf_handoff`) pour les rôles suivants. Échoue explicitement si aucun output n'est disponible, avec le message « avez-vous lancé `terraform apply` ? ».
3. **`deploy_monitoring`** — ajoute le dépôt Helm `prometheus-community`, installe/actualise la release `kube-prometheus-stack` dans le namespace `monitoring` avec les valeurs de `infra/monitoring/kube-prometheus-stack-values.yaml`, attend que l'opérateur Prometheus et Grafana soient prêts (`kubectl rollout status`).
4. **`deploy_app`** — charge l'image applicative construite localement dans le cluster (`minikube image load`, pas de registry pour le déploiement local — voir plus bas), installe/actualise la release Helm de l'application en réutilisant directement les valeurs issues de Terraform (namespace, PVC, chaîne de connexion), puis attend que les déploiements de l'application **et** de Nginx soient prêts.

## Dépendance aux outputs Terraform

Le rôle `deploy_app` ne connaît aucune valeur en dur : namespace, nom de la release, chemin du chart, fichiers de values, nom du PVC et chaîne de connexion SQLite proviennent tous de `tf_handoff` (calculé par le rôle `terraform_output`, lui-même dépendant d'un `terraform apply` déjà exécuté). C'est ce contrat qui garantit que l'application est toujours déployée dans le namespace et avec le PVC réellement préparés par Terraform, sans risque de désynchronisation entre les deux outils.

```bash
helm upgrade --install {{ tf_handoff.app_name }} {{ tf_handoff.chart_path }} \
  -n {{ tf_handoff.namespace }} --create-namespace \
  --kube-context {{ tf_handoff.kube_context }} \
  -f {{ tf_handoff.values_files | join(' -f ') }} \
  --set persistence.existingClaim={{ tf_handoff.sqlite_pvc }} \
  --set config.connectionString="Data Source={{ tf_handoff.sqlite_path }}"
```

Si Terraform n'a pas été appliqué au préalable, le playbook s'arrête dès le rôle `terraform_output` plutôt que d'échouer plus tard sur un `helm upgrade` incomplet.

## Pourquoi charger l'image sans registry

`group_vars/all.yml` fixe `app_image: "locatic:local"` et le rôle `deploy_app` utilise `minikube image load` : l'image construite localement (`docker build -t locatic:local ./Locatic`) est injectée directement dans le cluster minikube, sans passer par GHCR. C'est un choix délibéré pour le déploiement **local** : minikube tourne sur la même machine que le build, un aller-retour par une registry distante n'apporterait rien pour ce cas d'usage (l'image publiée par la CI sert, elle, à distribuer l'image en dehors de la machine du développeur — voir [`ci-cd.md`](ci-cd.md)).

## Exécuter le playbook

```bash
cd infra/ansible
ansible --version                 # vérifier l'installation
ansible-playbook site.yml --check # simulation (les modules command ne sont pas tous « check-mode safe »)
ansible-playbook site.yml         # exécution réelle
```

Variables consultables/modifiables avant exécution : `infra/ansible/group_vars/all.yml` (binaires requis, namespace de monitoring, nom de la release monitoring, image applicative).
