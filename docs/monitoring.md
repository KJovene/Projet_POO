# Monitoring

## Stack installée

Ansible installe **kube-prometheus-stack** (chart officiel `prometheus-community`) dans un namespace dédié `monitoring`, avec les valeurs de `infra/monitoring/kube-prometheus-stack-values.yaml` :

- **Prometheus** (`prometheusSpec`) — configuré pour découvrir les `PodMonitor` et `PrometheusRule` de **tous les namespaces** (`podMonitorNamespaceSelector: {}`, `ruleNamespaceSelector: {}`), pas seulement celui où il est installé : c'est nécessaire pour qu'il détecte les ressources du chart applicatif, qui vivent dans le namespace `monitoring` mais ciblent des pods du namespace `locatic-dev`.
- **Grafana** — dashboards et datasources provisionnés automatiquement par sidecar (`sidecar.dashboards.enabled`, `searchNamespace: ALL`) : toute `ConfigMap` labellisée `grafana_dashboard: "1"` dans n'importe quel namespace est chargée sans action manuelle.
- **Alertmanager** — activé, reçoit les alertes définies par la `PrometheusRule` de l'application.
- **kube-state-metrics** et **node-exporter** — activés, fournissent respectivement l'état des objets Kubernetes (pods, deployments...) et les métriques de la machine hôte.

Ce choix (chart communautaire tout-en-un plutôt que des composants séparés) réduit fortement l'effort d'installation et garantit une intégration Prometheus/Grafana/Alertmanager cohérente dès le départ, adaptée à un contexte pédagogique mono-cluster.

## Ce qui est monitoré, service par service

| Service | Mécanisme | Métriques |
| --- | --- | --- |
| **Application Locatic** | `prometheus-net` exposé sur `/metrics` (`UseHttpMetrics()` + `MapMetrics()` dans `Program.cs`), scrapé via `PodMonitor` `<release>-app` | Taux de requêtes HTTP, durée des requêtes (histogramme, dont p95) |
| **Nginx** | Sidecar `nginx-prometheus-exporter` qui scrape `/nginx_status` en local, exposé sur `/metrics` (port 9113), scrapé via `PodMonitor` `<release>-nginx` | Requêtes/s, connexions actives |
| **Pods & Services Kubernetes** | `kube-state-metrics` (installé avec la stack) | État « ready » des pods, endpoints disponibles par service |
| **Stockage SQLite** | Métriques `kubelet_volume_stats_*` du PVC (exposées nativement par le kubelet, sans configuration supplémentaire) | Octets utilisés/capacité du PVC SQLite → pourcentage d'usage |
| **Machine hôte** | `node-exporter` (inclus dans la stack) | CPU, mémoire, disque de la VM/du conteneur minikube |

## Alertes (`prometheusrule.yaml`)

Trois alertes couvrent les composants critiques, chacune avec une fenêtre `for` pour éviter les faux positifs sur un blip ponctuel :

| Alerte | Condition | Fenêtre |
| --- | --- | --- |
| `LocaticAppDown` | Le pod applicatif ne répond plus au scrape Prometheus | 2 min |
| `NginxExporterDown` | Le sidecar exporter Nginx ne répond plus (donc plus de visibilité sur Nginx) | 2 min |
| `SQLiteVolumeUsageHigh` | Le PVC SQLite dépasse 80 % d'usage | 5 min |

Ces alertes sont volontairement simples mais couvrent les trois angles du sujet : disponibilité applicative, disponibilité du reverse proxy, et saturation du stockage — les trois scénarios qui, en pratique, cassent le service.

## Accès à Prometheus et Grafana

Aucun service de monitoring n'est exposé publiquement (choix cohérent avec « seul Nginx est le point d'entrée ») : l'accès se fait via `kubectl port-forward` depuis le poste local.

```bash
kubectl get svc -n monitoring                       # retrouver les noms exacts de service (suffixés par le nom de la release Helm)

kubectl port-forward -n monitoring svc/kube-prometheus-stack-grafana 3000:80
# → http://localhost:3000  (identifiants par défaut : admin / admin, voir limites connues)

kubectl port-forward -n monitoring svc/<service-prometheus> 9090:9090
# → http://localhost:9090
```

Vérifier que les cibles attendues sont bien scrapées :

```bash
# Depuis l'UI Prometheus : Status → Targets
# ou en ligne de commande, une fois le port-forward actif :
curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets[].labels.job'
```

## Lecture du dashboard Grafana

Le dashboard « Locatic Overview » (`grafana-dashboard.yaml`) est provisionné automatiquement et regroupe, sur un seul écran, un indicateur par service :

- **HTTP Request Rate** / **HTTP Duration P95** — santé et performance de l'application.
- **Nginx Requests** / **Nginx Active Connections** — santé du reverse proxy.
- **Pods Not Ready** — un coup d'œil suffit pour savoir si un pod du namespace applicatif est en défaut.
- **SQLite PVC Usage %** (jauge) — anticipe la saturation du volume avant qu'elle ne déclenche l'alerte.
- **Kubernetes Services Up** — nombre d'endpoints disponibles dans le namespace applicatif.

En pratique : si tous les panneaux sont « verts »/non nuls et que « Pods Not Ready » est à 0, Nginx, l'application, le stockage et les composants Kubernetes sont opérationnels. Un panneau à zéro ou une jauge qui grimpe indique précisément quel composant regarder ensuite (voir [`exploitation.md`](exploitation.md)).

## Stack complémentaire (optionnelle, hors minikube)

`infra/monitoring/docker-compose.yml` fournit une stack Prometheus/Grafana/Alertmanager/node-exporter **indépendante**, à lancer en Docker Compose sur la machine hôte (`docker compose -f infra/monitoring/docker-compose.yml up`). Elle ne fait pas partie du chemin minikube décrit ci-dessus : elle sert uniquement à superviser la machine locale elle-même (CPU/disque via `alerts.yml`) en dehors de tout cluster, par exemple pendant le développement. Elle n'est pas nécessaire pour satisfaire les exigences de monitoring du déploiement Kubernetes, qui sont entièrement couvertes par kube-prometheus-stack.
