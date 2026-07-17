# Déploiement local — de l'image publiée à minikube

Ce document donne l'**ordre exact** des actions à réaliser sur votre machine, une fois qu'une image a été publiée par la CI (ou construite localement). Chaque étape indique pourquoi elle est nécessaire avant la suivante.

## 0. Prérequis

Voir [`README.md`](../README.md#prérequis-locaux) pour la liste des outils. Vérifier en particulier que Docker tourne (minikube en dépend par défaut sur macOS/Linux).

## 1. Démarrer minikube

```bash
minikube start
minikube status
```

Sans cluster actif, ni Terraform (provider Kubernetes) ni Ansible (`prereqs`) ne peuvent aller plus loin — le rôle `prereqs` du playbook échoue volontairement tôt et explicitement si ce n'est pas le cas.

## 2. Obtenir l'image applicative

Deux options équivalentes pour le déploiement local (voir la justification dans [`ansible.md`](ansible.md#pourquoi-charger-limage-sans-registry)) :

```bash
# Option A — build local (identique à ce que fait la CI)
docker build -t locatic:local ./Locatic

# Option B — récupérer l'image publiée par la CI sur main
docker pull ghcr.io/<owner>/locatic:<tag>
docker tag ghcr.io/<owner>/locatic:<tag> locatic:local
```

Le tag `locatic:local` doit correspondre à `image.repository`/`image.tag` dans les values Helm (`infra/helm/values.yaml`) — à ajuster si vous utilisez un autre tag.

## 3. Préparer l'infrastructure avec Terraform

```bash
cd infra/kubernetes/terraform
cp terraform.tfvars.example terraform.tfvars   # première fois seulement
terraform init
terraform plan
terraform apply
```

Cette étape crée le namespace et le `PersistentVolumeClaim` SQLite. Elle doit précéder Ansible : le rôle `terraform_output` lit directement les outputs produits ici. Détail complet dans [`terraform.md`](terraform.md).

## 4. Orchestrer le déploiement avec Ansible

```bash
cd ../../ansible
ansible-playbook site.yml
```

Le playbook, dans l'ordre : revérifie les prérequis, relit les outputs Terraform, installe/actualise `kube-prometheus-stack` (monitoring), charge l'image dans minikube puis installe/actualise la release Helm de l'application (Nginx + Locatic + volume). Détail des rôles dans [`ansible.md`](ansible.md).

## 5. Vérifier le déploiement

```bash
kubectl get all -n locatic-dev
kubectl get all -n monitoring
kubectl get pvc -n locatic-dev
```

Tous les pods doivent être `Running`/`Ready`. Voir [`exploitation.md`](exploitation.md) pour les vérifications détaillées et les commandes de diagnostic en cas de souci.

## 6. Accéder à l'application via Nginx

```bash
minikube service <release>-nginx-svc -n locatic-dev --url
# ou
kubectl port-forward -n locatic-dev svc/<release>-nginx-svc 18080:80
```

```bash
curl <url>/           # page d'accueil de Locatic
curl <url>/health     # doit répondre "Healthy"
```

L'application n'est **jamais** accessible directement (son `Service` est `ClusterIP`) : seul ce chemin via Nginx fonctionne, ce qui est la vérification que l'architecture reverse-proxy est bien en place.

## 7. Accéder au monitoring

Voir [`monitoring.md`](monitoring.md#accès-à-prometheus-et-grafana) pour les commandes `port-forward` vers Prometheus et Grafana, et la lecture du dashboard.

## Résumé du chemin complet

```
image publiée/construite
        │
        ▼
   minikube start
        │
        ▼
terraform apply (namespace + PVC)
        │
        ▼
ansible-playbook site.yml
        │
        ├─▶ kube-prometheus-stack (monitoring)
        └─▶ helm upgrade --install (Nginx + app + volume)
                │
                ▼
   accès utilisateur via Nginx (NodePort)
```

Redéployer après un changement de code : reconstruire l'image (étape 2), puis relancer uniquement `ansible-playbook site.yml` (étape 4) — Terraform n'a pas besoin d'être ré-appliqué tant que l'infrastructure ne change pas.
