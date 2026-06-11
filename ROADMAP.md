# Roadmap — Jelly Crowd

Plugin Jellyfin (Overseerr-like) : catalogue TMDB + requêtes en file admin + quotas disque par utilisateur.
Cible : **Jellyfin 10.11.x / .NET 9**. Implémentation milestone par milestone.

Légende : ☐ à faire · ☑ fait · ◐ en cours

---

## 📍 État actuel (point de reprise) — au 2026-06-11

- **Version publiée** : releases auto sur GitHub `Klakh/jelly-crowd` (dernière `v0.1.x`). Branche `main`, CI **verte**.
- **Fait (code)** : M0, **M1** (catalogue + modal détails + Plugin Pages), **M2** (requêtes : store JSON, RequestsController, bouton Demander, « Mes requêtes », file admin), **M3** (`LibraryMatcher` + flag `Available` + `ReconcileTask`) et **M4** (quotas disque par user : `QuotaService`, enforcement 403, barre d'usage, overrides admin).
- **En cours / prochaine action** : **M5 — Finition**. FAITS : nav directe, onglets, manifest de dépôt, **notifications Discord+email**, **catalogue enrichi** (filtres genres/années/notes en double-sliders, tri, survol des affiches, modal détaillé avec liens TMDB/IMDb). Reste : passe de thème/UX.
- **Bloqué côté agent (à faire par l'utilisateur)** : **vérifier M3/M4/M5 sur une instance live**. M1+M2 validés ; barre d'usage + liste users vus. Reste : M3 (badge « Disponible » + réconciliation), M4 (refus 403 au-delà du quota), M5 (install via dépôt + nav directe + onglets).

### Ce qui tourne déjà (vérifié en CI)
- Pipeline complet : **CI** (`build.yml` : restore → build Release → `dotnet test` → tests JS `node --test` → package `.zip`) + **Release** (`release.yml` : versionning auto par mot-clé de commit `[major]`/`[minor]`/patch → tag + GitHub Release).
- Backend TMDB : `TmdbClient`/`ITmdbClient`, `TmdbResponseParser`, `CatalogController` (`/JellyCrowd/Catalog/Trending|Search|Details`), DI via `PluginServiceRegistrator`.
- Frontend : `Web/catalog.html|js|css` + `Web/strings/{en,fr}.json`, servis par `WebController` (`/JellyCrowd/Web/...`), logique pure testée dans `Web/catalog.lib.js` (+ `tests/js/`).
- Enregistrement Plugin Pages : `PluginPageRegistrationService` (réflexion, sans dépendance NuGet).

### Faits à se rappeler en reprenant (IMPORTANT)
- **Pas de SDK .NET sur la machine de dev** → on ne build/teste PAS en local. On valide en **poussant sur GitHub et en lisant Actions** (gh CLI absent → API REST ; le token est dans l'URL du remote, ne pas l'afficher).
- **Toujours `git pull --ff-only` après un push** : la Release pousse un commit `chore(release): vX.Y.Z [skip ci]`.
- **Analyseurs très stricts** (`TreatWarningsAsErrors`, `AllEnabledByDefault`, StyleCop, Nullable) → écrire défensivement du premier coup (chaque itération = un aller-retour CI).
- **Plugin Pages** : on l'intègre **par réflexion** (le NuGet `Jellyfin.Plugin.PluginPages` embarque un générateur `Referenceable` qui ne compile pas sous nos réglages stricts). Reproduire ce choix pour tout autre plugin d'IAmParadox27.
- ⚠️ Un **token GitHub** (`ghp_…`) est exposé dans la config git du remote — à révoquer si besoin.
- Règles projet (anglais, indentation 2, i18n suit la langue Jellyfin, tests obligatoires par fonctionnalité) : voir `CLAUDE.md`.
- M1 n'a pas eu son bump `[minor]` (le commit `[minor]` avait échoué au build, le correctif est passé en patch). Pour marquer M1 → faire un commit `[minor]` (donnerait `v0.2.0`).

---

## M0 — Scaffolding & build  ☑

Objectif : un plugin vide qui **compile et se charge** dans Jellyfin 10.11, avec une page de config admin.

- ☑ Structure du dépôt + fichiers racine (`CLAUDE.md`, `ROADMAP.md`, `README.md`, `LICENSE`, `.gitignore`).
- ☑ Projet .NET : `.sln`, `.csproj` (net9.0), `Directory.Build.props`, `.editorconfig`, `jellyfin.ruleset`.
- ☑ `build.yaml` (manifest plugin : guid, `targetAbi 10.11.0.0`, framework `net9.0`, artefact dll).
- ☑ `Plugin.cs` (`BasePlugin<PluginConfiguration>`, `IHasWebPages`) + `PluginConfiguration.cs` + `configPage.html`.
- ☑ CI GitHub : build `Release` + tests + package `.zip` + workflow Release (versionning auto).
- ☑ **Vérif** : build Release OK en CI ; release `v0.1.x` produite. *(Chargement réel dans le Dashboard : à confirmer avec la vérif M1 live.)*

## M1 — Catalogue TMDB  ◐ (code fait, reste la vérif live)

Objectif : parcourir et chercher le catalogue TMDB depuis une page user.

- ☑ `TmdbClient`/`ITmdbClient` (HttpClient, clé API depuis la config) + `TmdbResponseParser` (testé).
- ☑ `CatalogController` : `Trending`, `Search`, `Details/{type}/{id}` (auth, langue, 400/404/503) + tests.
- ☑ Champ clé API TMDB dans la page de config admin.
- ☑ Assets page user `catalog` (HTML/JS/CSS) + i18n en/fr, servis par `WebController` (testé).
- ☑ **Enregistrement Plugin Pages** (`PluginPageRegistrationService` par réflexion, tolérant à l'absence) + logique JS pure testée (`node:test`).
- ☐ **Vérif (instance live — à faire par l'utilisateur)** :
  1. Installer **Plugin Pages** + **File Transformation** (dépôt `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`).
  2. Installer Jelly Crowd (`.zip` de la release v0.1.4), renseigner la **clé TMDB** dans la config du plugin.
  3. Vérifier que la page « Jelly Crowd » apparaît et liste films/séries (browse + recherche).
  4. Si la page n'apparaît pas : lire le log `PluginPageRegistrationService` et **ajuster `PageUrl`** dans `Services/PluginPageRegistrationService.cs`.

## M2 — Requêtes (file d'attente admin)  ◐ (code fait, reste la vérif live)

Objectif : créer des requêtes et les gérer côté admin.

- ☑ `IRequestStore` + `JsonRequestStore` — **store JSON** (fichier atomique dans le data path du plugin), choisi plutôt que SQLite pour éviter une dépendance native (volume de requêtes faible). Champs : id, userId, tmdbId, type, titre, poster, statut, dates, décideur.
- ☑ `RequestsController` : `Create`/`Mine` (user, `DefaultAuthorization`), `All`/`Approve`/`Deny` (admin, `RequiresElevation`). Doublon → 409, invalide → 400, introuvable → 404. + `ICurrentUserAccessor` (sur `IAuthorizationContext`). Tests nominal + erreurs.
- ☑ Bouton « Demander » sur les cartes du catalogue + page user « Mes requêtes » (`requests.html`/`requests.js`), logique pure (`statusLabelKey`) testée dans `tests/js`.
- ☑ File d'approbation dans la page de config admin (liste des `Pending` + Approuver/Refuser), i18n.
- ☐ **Vérif (instance live)** : un user crée une requête depuis le catalogue, l'admin la voit dans la config et l'approuve/refuse, le statut se met à jour dans « Mes requêtes ».

## M3 — Disponibilité bibliothèque  ◐ (code fait, reste la vérif live)

Objectif : savoir ce qui existe déjà et résoudre automatiquement les requêtes satisfaites.

- ☑ `ILibraryMatcher`/`LibraryMatcher` : recherche d'un item par `HasAnyProviderId[Tmdb]` via `ILibraryManager.GetItemList` (movie→`Movie`, tv→`Series`). Tests (Moq).
- ☑ Flag `Available` renseigné sur les résultats du catalogue (`CatalogController` enrichit Trending/Search/Details). Le badge + le modal l'affichent déjà.
- ☑ `ReconcileTask` (`IScheduledTask`, intervalle 6 h, auto-découverte) : passe les requêtes `Approved` en `Available` quand le média est en biblio. Tests.
- ☐ **Vérif (instance live)** : un titre déjà présent apparaît « Disponible » dans le catalogue ; après ajout d'un média demandé en biblio, la tâche planifiée « Jelly Crowd: reconcile requests » le bascule en `Available`.

## M4 — Quotas disque par utilisateur  ◐ (code fait, reste la vérif live)

Objectif : limiter l'occupation disque par user et bloquer au-delà.

- ☑ `LibraryMatcher.GetSizeBytes` : taille fichier d'un film, ou somme des tailles d'épisodes d'une série.
- ☑ `QuotaService` : usage = Σ tailles des requêtes `Available` du user ; `CanRequestAsync` = usage réel + estimations des requêtes en cours + estimation de la nouvelle ≤ quota ; override user sinon défaut ; 0 = illimité. Tests.
- ☑ Config : `QuotaOverrides` (par user) + défaut + estimations ; édités via la page admin (liste des users + Gio).
- ☑ `QuotaController` : `GET /JellyCrowd/Quota/Me` (usage du user courant).
- ☑ Enforcement à la création (`RequestsController.Create` → **403** si dépassement). Test.
- ☑ Affichage usage/quota côté user (barre sur « Mes requêtes ») + feedback « Quota dépassé » sur le bouton Demander. Helpers `formatBytes`/`quotaPercent` testés.
- ☐ **Vérif (instance live)** : régler un quota bas pour un user, vérifier la barre d'usage, et qu'une requête au-delà du quota est refusée (403 → « Quota dépassé »).

## M5 — Finition & distribution  ◐

- ☑ Accès direct : page de config dans la nav du dashboard admin (`EnableInMainMenu`).
- ☑ Onglets dans la config : Réglages / Quotas utilisateurs / Demandes.
- ☑ **Manifest de dépôt plugin** (`manifest.json` à la racine, peuplé à chaque release par `release.yml`) + doc d'install dans le README.
- ☑ i18n FR/EN sur les chaînes (onglets inclus).
- ☐ Notifications (requête approuvée / disponible / quota atteint).
- ☐ Thème & UX : passe d'affinage.
- ☐ **Vérif (instance live)** : ajouter le dépôt `https://raw.githubusercontent.com/Klakh/jelly-crowd/main/manifest.json`, installer/mettre à jour depuis le catalogue, vérifier la nav directe + les onglets.

---

## Hors périmètre v1 (idées futures)

- Intégration **Radarr/Sonarr** pour satisfaire les requêtes automatiquement.
- Quotas par type de média / par durée de rétention.
- Recommandations personnalisées, watchlists.
