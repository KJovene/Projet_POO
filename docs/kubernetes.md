# Kubernetes

Les ressources Kubernetes de l'application sont packagées en chart Helm (`infra/helm/devops-app-chart/`, détail de la structure dans [`helm.md`](helm.md)). Ce document décrit les ressources produites et leur rôle une fois déployées sur minikube.

## Ressources déployées

| Ressource | Nom | Rôle |
| --- | --- | --- |
| `Deployment` | `<release>` | Pod(s) applicatif Locatic. `RollingUpdate` (`maxSurge: 1`, `maxUnavailable: 0`) : jamais de coupure de service lors d'une mise à jour. |
| `Deployment` | `<release>-nginx` | Pod(s) Nginx (reverse proxy) + sidecar `nginx-prometheus-exporter`. |
| `Service` (ClusterIP) | `<release>-svc` | Expose l'application **à l'intérieur du cluster uniquement**. |
| `Service` (NodePort) | `<release>-nginx-svc` | Seul point d'entrée utilisateur, exposé sur le port `30080` par défaut. |
| `ConfigMap` | `<release>-config` | Variables d'environnement de l'application (`ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `ConnectionStrings__DefaultConnection`, niveau de log). |
| `ConfigMap` | `<release>-nginx-config` | Configuration `default.conf` de Nginx (reverse proxy + endpoint `/nginx_status`). |
| `Secret` (template) | `<release>-secrets` | Rendu uniquement si `secret.enabled: true`. Désactivé par défaut : l'application n'a aucun secret réel à stocker (pas de mot de passe DB avec SQLite). |
| `PersistentVolumeClaim` | préparé par Terraform | Voir [`terraform.md`](terraform.md) — consommé ici via `persistence.existingClaim`. |
| `PodMonitor` × 2, `PrometheusRule`, `ConfigMap` dashboard | — | Voir [`monitoring.md`](monitoring.md). |

## Services exposés

- **Nginx (`NodePort`, port `30080`)** : point d'entrée utilisateur unique. C'est la seule ressource routable depuis l'extérieur du cluster.
- **Application (`ClusterIP`, port `8080`)** : accessible uniquement depuis l'intérieur du cluster (donc depuis Nginx). Ce choix impose que tout accès utilisateur passe par le reverse proxy, conformément à la contrainte du sujet.

Le type de service est configurable (`service.type`, `nginx.service.type`) si l'on préfère un `LoadBalancer` (avec `minikube tunnel`) plutôt qu'un `NodePort`.

## Stockage SQLite

Le `Deployment` applicatif monte un volume nommé `data` sur `persistence.mountPath` (`/app/data` par défaut) :

- si `persistence.existingClaim` est renseigné (cas réel, valeur injectée automatiquement par Ansible depuis les outputs Terraform) → le volume est le `PersistentVolumeClaim` préparé par Terraform, donc **persistant** : les données survivent à la suppression/recréation du pod.
- si `persistence.existingClaim` est vide (déploiement de démo sans Terraform) → repli sur un `emptyDir`, donc **non persistant** : à utiliser uniquement pour un test rapide, jamais en usage réel.

La chaîne de connexion EF Core (`ConnectionStrings__DefaultConnection=Data Source=/app/data/locatic.db`) pointe vers ce même volume, injectée par la `ConfigMap` — ASP.NET Core surcharge automatiquement `appsettings.json` avec les variables d'environnement dont le nom respecte la convention `Section__Cle`.

## Configuration Nginx

`nginx-configmap.yaml` génère un `default.conf` minimal :

```nginx
location / {
    proxy_pass http://<release>-svc:8080;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

location /nginx_status {
    stub_status;
    allow 127.0.0.1;
    deny all;
}
```

Le `proxy_pass` cible le `Service` ClusterIP de l'application (résolution DNS interne au cluster, `<release>-svc`). L'endpoint `/nginx_status` est réservé à `localhost` : seul le sidecar exporter (dans le même pod, donc sur `127.0.0.1`) peut le scraper, il n'est jamais exposé publiquement.

## Probes de santé et ressources

- Application : `readinessProbe`/`livenessProbe` HTTP sur `/health` (délais et périodes configurables).
- Nginx : probes HTTP sur `/`.
- Chaque conteneur (app, nginx, exporter) déclare des `requests`/`limits` CPU et mémoire dimensionnés pour minikube (l'application est plus généreuse que Nginx/l'exporter, qui sont volontairement légers).

## Configuration facilement modifiable

Tout passe par les values Helm (voir [`helm.md`](helm.md) pour le détail complet) : nom/tag de l'image, nombre de replicas, variables d'environnement, type d'exposition du service, chemin de stockage SQLite, configuration Nginx, activation du monitoring — rien n'est codé en dur dans les templates.
