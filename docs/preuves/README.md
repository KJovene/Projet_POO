# Preuves d'exécution

Ce dossier rassemble les captures, extraits de logs et exports montrant que les étapes clés du projet ont réellement été exécutées, pas seulement écrites.

## À déposer ici

- [ ] Capture de la Pull Request mergée (checks CI verts, review obligatoire visible).
- [x] Capture de la ruleset GitHub sur `main` (`Settings → Rules → Rulesets`).
- [ ] Capture du run GitHub Actions complet (`test` → `build` → `security`) et de l'image publiée sur GHCR (`ghcr.io/<owner>/locatic`, onglet Packages du dépôt).
- [x] Sortie de `terraform apply` (`infra/kubernetes/terraform`) montrant le namespace et le PVC créés.
- [x] Sortie de `ansible-playbook site.yml` allant jusqu'au bout sans erreur.
- [x] `kubectl get all -n locatic-dev` et `kubectl get all -n monitoring` montrant tous les pods `Running`.
- [x] `curl <url>/health` réussi via l'URL Nginx (prouve le passage par le reverse proxy).
- [x] Preuve de persistance : contenu créé dans l'application, suppression du pod applicatif, contenu toujours présent après redémarrage (voir [`../exploitation.md`](../exploitation.md#vérifications-après-déploiement)).
- [x] Capture du dashboard Grafana « Locatic Overview » avec des données réelles sur chaque panneau.
- [x] Capture de Prometheus → Status → Targets montrant l'application et Nginx en `UP`.
- [x] Exemple d'alerte déclenchée puis résolue (ex. `LocaticAppDown` en arrêtant temporairement le déploiement applicatif).

Items 1 et 3 (captures GitHub) : non produits — aucun run CI ne s'est encore exécuté sur `main` (les pushes historiques ciblent `dev`) et le token `gh` local n'a pas le scope `read:packages`. Une preuve réelle nécessiterait de merger `dev` → `main`, ce qui a été volontairement laissé de côté pour l'instant.

## Convention de nommage

`NN-description-courte.png` (ou `.log`, `.txt`, `.json`), numéroté dans l'ordre du déroulé décrit dans [`../deploiement-local.md`](../deploiement-local.md) :

```
01-pr-checks.txt
02-ruleset-main.json
03-ci-pipeline.txt
05-terraform-apply.log
06-ansible-playbook.log
07-kubectl-get-all.txt
08-curl-health-via-nginx.txt
09-persistance-sqlite.log
10-grafana-dashboard.txt
11-prometheus-targets.json
12-alerte-declenchee.log
```

## Détail par étape

### 01/02 — Pull Request & ruleset `main`

Checks non disponibles via `gh pr checks` pour la PR #3 (historique trop ancien). La ruleset, elle, est entièrement vérifiée :

```bash
gh api repos/$REPO/rulesets/18429682 | jq .
```

→ `pull_request` (1 review requise), `required_status_checks` (`test`, `build / build`, `security`), `non_fast_forward`, `deletion` — voir [`02-ruleset-main.json`](02-ruleset-main.json).

### 03 — Run CI + image GHCR

Non réalisable en l'état : aucun run GitHub Actions ne s'est exécuté sur `main` depuis la mise en place du pipeline (tous les runs historiques sont déclenchés par des push sur `dev`). Nécessite un merge réel vers `main`.

### 05 — `terraform apply`

```bash
cd infra/kubernetes/terraform
terraform init
terraform apply
```

→ `kubernetes_namespace_v1.app` et `kubernetes_persistent_volume_claim_v1.sqlite` créés (namespace `locatic-dev`, PVC `locatic-sqlite`, 1Gi). Détail complet : [`05-terraform-apply.log`](05-terraform-apply.log).

### 06 — `ansible-playbook site.yml`

```bash
cd infra/ansible
ansible-playbook site.yml
```

→ `PLAY RECAP` : `ok=15 changed=3 unreachable=0 failed=0 skipped=4`. Détail complet : [`06-ansible-playbook.log`](06-ansible-playbook.log).

### 07 — État du cluster

```bash
kubectl get all -n locatic-dev
kubectl get all -n monitoring
```

→ Tous les pods applicatifs (`locatic`, `locatic-nginx`) et de monitoring (Prometheus, Grafana, Alertmanager, kube-state-metrics, node-exporter) en `Running`. Détail complet : [`07-kubectl-get-all.txt`](07-kubectl-get-all.txt).

### 08 — `curl /health` via Nginx

Deux méthodes d'exposition testées avec succès :

```bash
kubectl port-forward -n locatic-dev svc/locatic-nginx-svc 18080:80 &
curl -i http://localhost:18080/health
```

```bash
URL=$(minikube service locatic-nginx-svc -n locatic-dev --url)
curl -i "$URL/health"
```

→ `HTTP/1.1 200 OK` / `Healthy` dans les deux cas. Détail complet : [`08-curl-health-via-nginx.txt`](08-curl-health-via-nginx.txt).

### 09 — Persistance SQLite

Un client (`Preuve Persistance-SQLite`) est créé via le vrai formulaire de l'application (récupération du token antiforgery puis `POST /Client/Create`), le pod applicatif est supprimé, puis le nouveau pod est interrogé :

```bash
kubectl delete pod -n locatic-dev -l app.kubernetes.io/component=app
kubectl wait --for=condition=Ready pod -n locatic-dev -l app.kubernetes.io/component=app --timeout=90s
curl -s http://localhost:18080/Client | grep "Persistance-SQLite"
```

→ Le client créé avant la suppression est toujours présent après le redémarrage du pod (le PVC persiste bien les données SQLite). Détail complet : [`09-persistance-sqlite.log`](09-persistance-sqlite.log).

### 10 — Dashboard Grafana

```bash
kubectl port-forward -n monitoring svc/kube-prometheus-stack-grafana 3000:80
curl -s -u admin:admin http://localhost:3000/api/search?query=Locatic
```

→ Dashboard « Locatic Overview » présent, 7 panneaux. Chaque requête PromQL sous-jacente a été rejouée directement contre Prometheus avec du trafic réel généré au préalable, pour confirmer que chaque panneau a des données non nulles. Un bug a été corrigé en cours de route : le panneau **Kubernetes Services Up** utilisait `kube_endpoint_address_available`, une métrique supprimée par les versions récentes de kube-state-metrics (remplacée par les métriques `kube_endpointslice_*`) — corrigé dans `infra/helm/devops-app-chart/templates/grafana-dashboard.yaml` (requête `sum(kube_endpointslice_endpoints{namespace="locatic-dev", ready="true"})`). Détail complet : [`10-grafana-dashboard.txt`](10-grafana-dashboard.txt).

Limite connue : le panneau **SQLite PVC Usage %** reste vide — les métriques `kubelet_volume_stats_*` ne sont pas exposées par le provisioner de stockage par défaut de minikube (limite d'environnement local, pas un bug applicatif).

### 11 — Prometheus Targets

```bash
kubectl port-forward -n monitoring svc/kube-prometheus-stack-prometheus 9090:9090
curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | {job: .labels.job, health}'
```

→ `monitoring/locatic-app` et `monitoring/locatic-nginx` en `up`. Un bug applicatif a été découvert et corrigé en cours de route : l'image `locatic:local` déployée datait du 02/07 alors que le code exposant `/metrics` (`app.MapMetrics`) n'a été ajouté que le 15/07 — l'image a été reconstruite (`docker build`), rechargée dans minikube (`minikube image rm` + `minikube image load`, l'`--overwrite` par défaut ne suffisant pas à lui seul) et redéployée (`ansible-playbook site.yml`). Détail complet : [`11-prometheus-targets.json`](11-prometheus-targets.json).

### 12 — Alerte déclenchée puis résolue

```bash
kubectl scale deployment/locatic -n locatic-dev --replicas=0
# ... 2 minutes plus tard ...
kubectl scale deployment/locatic -n locatic-dev --replicas=1
```

→ `LocaticAppDown` passe `inactive` → `pending` → `firing` (visible dans Prometheus et Alertmanager), puis revient à `inactive` une fois le pod recréé. Un bug a été corrigé en cours de route : la règle originale (`up{...} == 0`) ne se déclenchait jamais dans ce scénario, car `kubectl scale --replicas=0` retire complètement le pod de la découverte de service Kubernetes — la série Prometheus devient _absente_, pas égale à 0. Corrigé dans `infra/helm/devops-app-chart/templates/prometheusrule.yaml` en ajoutant `or absent(up{...})` aux règles `LocaticAppDown` et `NginxExporterDown`. Détail complet : [`12-alerte-declenchee.log`](12-alerte-declenchee.log).
