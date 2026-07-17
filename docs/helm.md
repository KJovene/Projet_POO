# Helm

Le chart `infra/helm/devops-app-chart/` (bonus réalisé) packages l'ensemble du déploiement applicatif : Locatic, Nginx, et les ressources de monitoring associées. C'est ce chart qu'Ansible installe/actualise à chaque exécution du playbook (voir [`ansible.md`](ansible.md)).

## Structure du chart

```
infra/helm/devops-app-chart/
├── Chart.yaml                     # métadonnées (nom, version 0.2.0, appVersion 1.0.0)
├── values.yaml                    # valeurs par défaut
└── templates/
    ├── _helpers.tpl                # helpers de nommage/labels communs
    ├── deployment.yaml              # Deployment de l'application
    ├── service.yaml                 # Service ClusterIP de l'application
    ├── configmap.yaml                # variables d'environnement de l'application
    ├── secret.yaml                   # template de Secret (désactivé par défaut)
    ├── nginx-deployment.yaml          # Deployment Nginx + sidecar exporter
    ├── nginx-service.yaml              # Service NodePort Nginx (point d'entrée)
    ├── nginx-configmap.yaml             # configuration reverse-proxy Nginx
    ├── podmonitor-app.yaml               # scrape Prometheus de l'application
    ├── podmonitor-nginx.yaml              # scrape Prometheus de l'exporter Nginx
    ├── prometheusrule.yaml                 # alertes (voir monitoring.md)
    ├── grafana-dashboard.yaml               # dashboard auto-provisionné
    └── NOTES.txt                             # instructions affichées après install
```

`infra/helm/values.yaml` (hors chart) est l'overlay commun à tous les environnements, et `infra/helm/values-dev.yaml`/`values-prod.yaml` les overlays spécifiques, appliqués en cascade (`-f values.yaml -f values-<env>.yaml`) — c'est exactement ce que fait Ansible via l'output `tf_handoff.values_files`.

## Valeurs configurables (extrait de `values.yaml`)

| Clé | Rôle |
| --- | --- |
| `replicaCount` | Nombre de répliques de l'application (1 en dev, 3 en prod via l'overlay) |
| `image.repository` / `image.tag` | Image applicative (`locatic:local` par défaut, chargée via `minikube image load`, pas de pull registry) |
| `service.type` / `service.port` | Type et port du `Service` applicatif (`ClusterIP` par défaut, volontairement non exposé directement) |
| `config.*` | Variables d'environnement injectées dans le conteneur applicatif, dont `config.connectionString` (chaîne SQLite) |
| `persistence.enabled` / `persistence.mountPath` / `persistence.existingClaim` | Volume SQLite : activation, chemin de montage, PVC à utiliser (sinon `emptyDir`) |
| `resources` | Requêtes/limites CPU-mémoire de l'application |
| `probes.readiness` / `probes.liveness` | Chemin et délais des probes de santé |
| `nginx.*` | Image, replicas, service (type/port/nodePort), probes et ressources de Nginx |
| `metrics.enabled` | Active les `PodMonitor`, la `PrometheusRule` et le dashboard Grafana |
| `monitoring.namespace` / `monitoring.releaseLabel` | Namespace et label de release attendus par l'opérateur Prometheus pour découvrir ces ressources |
| `secret.enabled` / `secret.data` | Active un vrai `Secret` Kubernetes si l'application venait à en avoir besoin |

Chaque valeur peut être surchargée en ligne de commande (`--set`) ou via un fichier de values additionnel, sans jamais modifier les templates.

## Procédure de release

Installation ou mise à jour manuelle (hors Ansible, pour du débogage) :

```bash
cd infra/helm
helm lint devops-app-chart
helm template locatic devops-app-chart -f values.yaml -f values-dev.yaml   # relire les manifests générés
helm upgrade --install locatic devops-app-chart \
  -n locatic-dev --create-namespace \
  -f values.yaml -f values-dev.yaml \
  --set persistence.existingClaim=<pvc-cree-par-terraform> \
  --set config.connectionString="Data Source=/app/data/locatic.db"

helm status locatic -n locatic-dev
helm history locatic -n locatic-dev
```

En usage normal, c'est le rôle Ansible `deploy_app` qui exécute cette commande avec les valeurs issues des outputs Terraform (voir [`ansible.md`](ansible.md)) — la commande manuelle ci-dessus sert surtout à isoler un problème de chart sans repasser par toute la chaîne.

### Rollback

```bash
helm rollback locatic <revision> -n locatic-dev
```

Voir [`exploitation.md`](exploitation.md) pour la procédure complète et un exemple démontré.
