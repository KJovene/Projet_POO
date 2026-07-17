# Locatic — DevOps WEB2

## Objectif

Ce dépôt reprend l'application **Locatic** (agence de location de voitures, ASP.NET Core MVC + SQLite, réalisée en POO) pour y bâtir une chaîne DevOps complète : Pull Requests protégées, intégration continue GitHub Actions, publication d'image Docker, infrastructure locale Terraform, orchestration Ansible et déploiement Kubernetes sur **minikube**, avec Nginx en reverse proxy et une supervision Prometheus/Grafana.

GitHub Actions s'arrête volontairement après la publication de l'image : le déploiement cible votre machine locale (minikube), un runner GitHub ne peut pas y accéder. Le détail de cette limite est expliqué dans [`docs/ci-cd.md`](docs/ci-cd.md).

## Lien avec le projet de POO

Le code applicatif (`Locatic/`) est celui du projet de POO Locatic : entités `CarBrand` → `CarModel` → `Car`, `Client`, `Reservation`, persistées en SQLite via EF Core, contrôleurs CRUD, vues Razor. Aucune fonctionnalité métier n'a été réécrite ; les seuls ajouts concernent l'exploitation en conteneur :

- endpoint de santé `/health` et endpoint de métriques `/metrics` (`Locatic/Program.cs`),
- chaîne de connexion SQLite surchargeable par variable d'environnement (`ConnectionStrings__DefaultConnection`),
- `Locatic/Dockerfile` pour la conteneurisation,
- suite de tests `Locatic.Tests/` (contrôleurs + helpers d'affichage),
- tout ce qui est sous `.github/`, `infra/` et `docs/`.

## Structure du dépôt

```
.
├── Locatic/                     # Application ASP.NET Core MVC (code métier du projet POO)
├── Locatic.Tests/                # Tests automatisés (xUnit)
├── Locatic.sln
├── .github/
│   ├── workflows/                # ci.yml (pipeline) + reusable-docker.yml (build/publish image)
│   └── actions/setup-tools/      # action composite d'installation d'outils
├── infra/
│   ├── kubernetes/terraform/     # Terraform actif : namespace + PVC SQLite sur minikube
│   ├── terraform/                # Variante Docker Compose locale (hors minikube, voir docs/terraform.md)
│   ├── helm/
│   │   ├── devops-app-chart/     # Chart Helm de l'application (app + Nginx + monitoring)
│   │   ├── terraform/            # Variante Terraform pilotant Helm directement (non utilisée par le pipeline actif)
│   │   └── values*.yaml          # Overlays d'environnement (dev/prod)
│   ├── ansible/                  # Playbook d'orchestration du déploiement local
│   └── monitoring/               # Stack Prometheus/Grafana/Alertmanager host-level (complémentaire, optionnelle)
├── docs/                         # Documentation détaillée (voir ci-dessous)
└── mini-project.md               # Cahier des charges du mini-projet
```

## Prérequis locaux

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (build de l'image + minikube driver)
- [minikube](https://minikube.sigs.k8s.io/) et [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Terraform](https://developer.hashicorp.com/terraform/downloads) ≥ 1.5
- [Ansible](https://docs.ansible.com/) (`ansible-playbook`)
- [Helm](https://helm.sh/) ≥ 3

Vérification rapide :

```bash
dotnet --version && docker --version && minikube version && kubectl version --client && terraform --version && ansible --version && helm version
```

## Démarrage rapide

### 1. Lancer l'application en local (sans conteneur)

```bash
cd Locatic
dotnet restore
dotnet build
dotnet run
```

L'application applique les migrations EF Core et seed la base au démarrage (`Program.cs`). Elle écoute par défaut sur le port configuré par `launchSettings.json`.

### 2. Lancer les tests

```bash
dotnet test Locatic.sln
```

### 3. Construire et lancer l'image Docker

```bash
docker build -t locatic:local ./Locatic
docker run --rm -p 8080:8080 locatic:local
curl http://localhost:8080/health
```

### 4. Déployer sur minikube (Terraform → Ansible → Kubernetes)

Résumé (détail complet dans [`docs/deploiement-local.md`](docs/deploiement-local.md)) :

```bash
minikube start
docker build -t locatic:local ./Locatic

cd infra/kubernetes/terraform
terraform init && terraform apply

cd ../../ansible
ansible-playbook site.yml
```

Ansible déploie ensuite Nginx, l'application (derrière Nginx, avec son volume SQLite) et la stack Prometheus/Grafana sur le cluster minikube.

## Documentation

| Fichier | Contenu |
| --- | --- |
| [`docs/architecture.md`](docs/architecture.md) | Vue d'ensemble de l'architecture et rôle de chaque composant |
| [`docs/ci-cd.md`](docs/ci-cd.md) | Règles de branche, Pull Requests, pipeline GitHub Actions, limites |
| [`docs/deploiement-local.md`](docs/deploiement-local.md) | Ordre exact des actions locales, de l'image publiée au déploiement minikube |
| [`docs/terraform.md`](docs/terraform.md) | Ressources Terraform, variables, outputs, gestion de l'état |
| [`docs/ansible.md`](docs/ansible.md) | Rôle du playbook, rôles orchestrés, dépendance aux outputs Terraform |
| [`docs/kubernetes.md`](docs/kubernetes.md) | Ressources Kubernetes, services exposés, stockage SQLite, Nginx |
| [`docs/helm.md`](docs/helm.md) | Structure du chart Helm, valeurs configurables, releases |
| [`docs/monitoring.md`](docs/monitoring.md) | Services monitorés, métriques, accès Prometheus/Grafana, dashboard |
| [`docs/exploitation.md`](docs/exploitation.md) | Vérifications post-déploiement, logs, rollback, limites connues |
| [`docs/preuves/`](docs/preuves/) | Captures et extraits de logs des étapes clés |

## Limites connues

Voir la section dédiée dans [`docs/exploitation.md`](docs/exploitation.md#limites-connues).
