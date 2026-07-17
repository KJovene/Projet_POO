# CI/CD

## Règles de branche

`main` est protégée par une ruleset GitHub (`Settings → Rules → Rulesets`) qui interdit :

- la suppression de la branche,
- le push direct et le force-push (règle `non_fast_forward`),
- le merge sans Pull Request approuvée (au moins 1 review),
- le merge si les checks obligatoires ne sont pas au vert (`test`, `build / build`, `security`), avec exigence que la branche soit à jour avec `main` avant de merger (`strict_required_status_checks_policy`).

Concrètement : personne ne peut pousser directement sur `main`, et une PR reste bloquée tant que les tests, le build de l'image et le scan de sécurité n'ont pas réussi.

## Pull Requests

Le travail se fait sur des branches de fonctionnalité (`feature/...`) fusionnées via Pull Request vers `dev`, puis `dev` vers `main`. Chaque PR déclenche automatiquement le pipeline sur `refs/heads/main` en cible. L'historique du dépôt contient des PR mergées représentatives de ce fonctionnement (ex. `feature/seed`, `feature/client`).

## Jobs du pipeline

Le pipeline (`.github/workflows/ci.yml`) se déclenche sur push (`main`, `dev`) et sur Pull Request vers `main` :

| Job | Rôle | Dépend de |
| --- | --- | --- |
| `test` | `dotnet restore` / `build` / `test` sur `Locatic.sln`, résultats exportés en artefact (`.trx`) | — |
| `build` | Build de l'image Docker (workflow réutilisable `reusable-docker.yml`), publication conditionnelle | `test` |
| `security` | Scan Trivy du code source (`fs`) sur `./Locatic`, sévérités `HIGH`/`CRITICAL`, échoue le pipeline si une vulnérabilité non ignorée est trouvée | `build` |
| `deploy-dev` | Simulation d'un déploiement vers l'environnement `dev` (n'exécute rien de réel, illustre l'usage d'un environnement GitHub avec secrets masqués) | `build`, `security`, uniquement sur `refs/heads/dev` |
| `deploy-prod` | Idem pour `main` | `build`, `security`, uniquement sur `refs/heads/main` |

Le job `build` délègue au workflow réutilisable `reusable-docker.yml`, qui fait le `docker build` (avec cache GitHub Actions) puis, seulement si `push: true` lui est passé, se connecte à la registry et pousse l'image. Chaque étape est une dépendance explicite (`needs:`) de la suivante : un échec de `test` bloque `build`, un échec de `build` ou `security` bloque les jobs de déploiement simulé.

## Publication de l'image

L'image est construite à chaque exécution (pour valider le Dockerfile sur chaque PR), mais **publiée uniquement lors d'un push sur `main`** (`push: ${{ github.ref == 'refs/heads/main' }}` passé au workflow réutilisable). Elle est poussée sur **GitHub Container Registry** (`ghcr.io/<owner>/locatic`) en utilisant le `GITHUB_TOKEN` généré automatiquement pour le job (portée `packages: write`) — aucun secret de registry supplémentaire à créer ou à stocker. Les tags générés (`docker/metadata-action`) incluent le SHA du commit, le nom de la branche et, le cas échéant, une version semver.

Ce choix (GHCR + `GITHUB_TOKEN`) évite d'avoir à gérer un compte de registry tiers et ses identifiants comme secrets GitHub : c'est la registry la plus simple à sécuriser correctement dans ce contexte pédagogique.

## Limites du pipeline GitHub

- **Le pipeline ne déploie jamais sur minikube.** minikube tourne sur la machine du développeur ; un runner GitHub Actions (hébergé, éphémère) n'a aucun moyen de l'atteindre. Le pipeline s'arrête donc volontairement après la publication de l'image : Terraform et Ansible ne sont exécutés qu'en local (voir [`deploiement-local.md`](deploiement-local.md)).
- Les jobs `deploy-dev`/`deploy-prod` sont **simulés** (`echo`) : ils démontrent la gestion de secrets scopés par environnement GitHub (`DB_PASSWORD`, `API_KEY`, masqués dans les logs), mais ne provisionnent rien. Ils ne doivent pas être confondus avec un vrai déploiement.
- Le lint applicatif n'est pas intégré au pipeline (pas d'analyseur configuré sur le projet POO d'origine) ; seuls build et tests garantissent la qualité minimale.
- Le scan Trivy porte sur le système de fichiers du projet (dépendances NuGet), pas sur l'image Docker finale elle-même ; une CVE sans correctif amont est explicitement documentée et ignorée dans `.trivyignore` avec sa justification.
