# Architecture

## Vue d'ensemble

```
Développeur ──PR──▶ GitHub ──▶ GitHub Actions ──▶ Registry (GHCR)
                                (test, build,          │
                                 scan sécurité)         │ image publiée
                                                         ▼
Poste local ──terraform apply──▶ namespace + PVC SQLite (minikube)
             ──ansible-playbook──▶ Helm (kube-prometheus-stack, puis app)
                                         │
                                         ▼
                          ┌─────────────────────────────┐
                          │         minikube             │
                          │  Nginx (NodePort, entrée)     │
                          │    └─▶ Locatic (ClusterIP)    │
                          │           └─▶ PVC SQLite      │
                          │  Prometheus + Grafana          │
                          │  (namespace monitoring)        │
                          └─────────────────────────────┘
```

Le principe directeur : **GitHub s'arrête à la publication de l'image**. Tout ce qui touche à minikube (une ressource propre à la machine du développeur) est déclenché localement, en deux temps — Terraform prépare l'infrastructure, Ansible orchestre le déploiement applicatif.

## Rôle de GitHub Actions

Le pipeline (`.github/workflows/ci.yml`) valide chaque Pull Request et chaque push sur `main`/`dev` : restauration, build, tests, build de l'image Docker, scan de sécurité Trivy, puis publication de l'image sur GitHub Container Registry **uniquement** lors d'un push sur `main`. Il ne connaît ni Terraform, ni Ansible, ni minikube : son rôle s'arrête à produire une image de confiance, prête à être déployée. Détails dans [`ci-cd.md`](ci-cd.md).

## Rôle de Terraform

Terraform (`infra/kubernetes/terraform/`) prépare la partie de l'infrastructure minikube qui doit exister **avant** que l'application ne soit déployée et qui doit **survivre** aux déploiements successifs : le namespace Kubernetes et le `PersistentVolumeClaim` qui portera le fichier SQLite. Terraform ne déploie pas l'application elle-même — ce n'est pas son rôle dans ce flux : un `helm upgrade` répété ne doit pas recréer le volume à chaque fois, d'où la séparation avec l'étape suivante. Terraform expose ensuite un output structuré (`handoff`) qui donne à Ansible tout ce dont il a besoin pour continuer. Détails dans [`terraform.md`](terraform.md).

## Rôle d'Ansible

Ansible (`infra/ansible/`) est la couche d'orchestration qui relie « l'infrastructure préparée par Terraform » à « l'application effectivement déployée ». Le playbook vérifie les prérequis locaux (binaires installés, minikube démarré, `kubectl` capable de joindre le cluster), lit les outputs Terraform, installe/actualise la stack de monitoring, charge l'image applicative dans minikube puis installe/actualise la release Helm de l'application. C'est délibérément Ansible qui pilote Helm plutôt que Terraform : cela permet de relancer un déploiement (nouvelle image, nouvelle config) sans toucher à l'état Terraform, et de garder une frontière claire entre « ressources durables » (Terraform) et « ce qui se met à jour à chaque déploiement » (Ansible/Helm). Détails dans [`ansible.md`](ansible.md).

## Rôle du déploiement Kubernetes

Le déploiement applicatif est packagé en chart Helm (`infra/helm/devops-app-chart/`) : un `Deployment` pour Locatic, un `Deployment` pour Nginx, leurs `Service` respectifs, une `ConfigMap` pour la configuration de l'application (dont la chaîne de connexion SQLite), un template de `Secret` (désactivé par défaut, l'application n'a pas de secret réel à stocker), les probes de santé, les requêtes/limites CPU/mémoire, et les ressources de monitoring (`PodMonitor`, `PrometheusRule`, dashboard Grafana). Tout est paramétrable via `values.yaml` et les overlays `values-dev.yaml`/`values-prod.yaml`. Détails dans [`kubernetes.md`](kubernetes.md) et [`helm.md`](helm.md).

## Rôle de Nginx

Nginx est le **seul point d'entrée utilisateur** : son `Service` est exposé en `NodePort`, alors que le `Service` de l'application est en `ClusterIP` (non routable depuis l'extérieur du cluster). Nginx fait un simple reverse proxy HTTP vers l'application (`proxy_pass` avec les en-têtes `X-Forwarded-*` standards), et expose en plus un endpoint `/nginx_status` restreint à `localhost`, utilisé par le sidecar `nginx-prometheus-exporter` pour alimenter Prometheus. Cette séparation garantit que l'application n'est jamais atteignable en contournant le reverse proxy.

## Rôle du volume SQLite

L'application utilise SQLite comme unique base de données (`postgresql.enabled: false` dans les values Helm — choix assumé, le sujet interdit toute base externe). Le fichier de base est monté dans le conteneur applicatif sur `/app/data` (configurable), sur un volume qui provient soit d'un `PersistentVolumeClaim` préparé par Terraform (mode réel, les données survivent aux redémarrages de pod et aux réinstallations Helm), soit d'un `emptyDir` de secours si aucun PVC n'est fourni (mode démo autonome, sans Terraform). La chaîne de connexion EF Core (`ConnectionStrings__DefaultConnection`) est injectée par variable d'environnement via la `ConfigMap`, ce qui évite de coder en dur un chemin dans l'application.

## Rôle du monitoring

Chaque composant important expose des métriques Prometheus : l'application via `prometheus-net` (`/metrics`, métriques HTTP), Nginx via un sidecar `nginx-prometheus-exporter`, l'état des pods/services via `kube-state-metrics`, et l'usage du volume SQLite via les métriques kubelet du PVC. La stack `kube-prometheus-stack` (Prometheus + Grafana + Alertmanager) est installée par Ansible dans un namespace `monitoring` dédié ; le chart applicatif fournit ses propres `PodMonitor`, `PrometheusRule` (alertes) et un dashboard Grafana auto-provisionné par sidecar. Détails dans [`monitoring.md`](monitoring.md).
