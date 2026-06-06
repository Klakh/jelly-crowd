# Roadmap — Jelly Crowd

Plugin Jellyfin (Overseerr-like) : catalogue TMDB + requêtes en file admin + quotas disque par utilisateur.
Cible : **Jellyfin 10.11.x / .NET 9**. Implémentation milestone par milestone.

Légende : ☐ à faire · ☑ fait · ◐ en cours

---

## 📍 État actuel (point de reprise) — au 2026-06-06

- **Version publiée** : `v0.1.4` (releases auto sur GitHub `Klakh/jelly-crowd`). Branche `main`, CI **verte**.
- **Fait** : M0 (scaffold + plugin qui charge) et **M1 côté code** (catalogue TMDB browse/recherche + page user + i18n en/fr + enregistrement Plugin Pages).
- **En cours / prochaine action** : **M2 — Requêtes** (pas encore commencé).
- **Bloqué côté agent (à faire par l'utilisateur)** : **vérifier l'UID M1 sur une instance Jellyfin live** (voir M1, dernière case). C'est le seul élément non validé.

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

## M2 — Requêtes (file d'attente admin)  ☐ ← PROCHAINE ÉTAPE

Objectif : créer des requêtes et les gérer côté admin.

- ☐ `RequestStore` (SQLite via `Microsoft.Data.Sqlite`, dans le data path du plugin) : table `requests` (id, userId, tmdbId, type, titre, statut, dates...).
- ☐ `RequestsController` : `create` (user), `list` (user/admin), `approve`/`deny` (admin) + tests (nominal + erreurs + autorisations).
- ☐ Bouton « Demander » sur la fiche + page user « Mes requêtes » (logique pure dans un `*.lib.js` testé).
- ☐ File d'approbation dans la page de config admin (liste + approuver/refuser).
- ☐ **Vérif** : un user crée une requête, l'admin la voit et l'approuve/refuse, statut mis à jour.

## M3 — Disponibilité bibliothèque  ☐

Objectif : savoir ce qui existe déjà et résoudre automatiquement les requêtes satisfaites.

- ☐ `LibraryMatcher` : recherche d'un item par `ProviderId` Tmdb via `ILibraryManager`.
- ☐ Flag `available` ajouté aux résultats du catalogue (le champ `CatalogItem.Available` existe déjà).
- ☐ `ReconcileTask` (`IScheduledTask`) : lie les requêtes approuvées aux nouveaux items, passe en `available`.
- ☐ **Vérif** : un titre déjà en biblio est marqué « disponible » ; une requête se résout quand l'item arrive.

## M4 — Quotas disque par utilisateur  ☐

Objectif : limiter l'occupation disque par user et bloquer au-delà.

- ☐ `QuotaService` : usage_user = Σ tailles fichiers des items liés aux requêtes satisfaites du user.
- ☐ Config : quota global par défaut + overrides par user + tailles d'estimation (film/épisode). *(Les champs `DefaultUserQuotaBytes`, `EstimatedMovieSizeBytes`, `EstimatedEpisodeSizeBytes` existent déjà dans `PluginConfiguration`.)*
- ☐ `QuotaController` : usage par user ; get/set quotas (admin).
- ☐ Enforcement à la création de requête : refus si `usage + estimation > quota`.
- ☐ Affichage usage/quota côté user (barre de progression).
- ☐ **Vérif** : un user au-delà de son quota ne peut plus créer de requête ; l'usage reflète la biblio.

## M5 — Finition & distribution  ☐

- ☐ Notifications (requête approuvée / disponible / quota atteint).
- ☐ i18n FR/EN complète sur toutes les nouvelles chaînes.
- ☐ Thème & UX alignés sur Jellyfin (via Plugin Pages).
- ☐ Manifest de dépôt plugin installable + doc d'installation.
- ☐ **Vérif** : install propre depuis un dépôt plugin sur une instance neuve.

---

## Hors périmètre v1 (idées futures)

- Intégration **Radarr/Sonarr** pour satisfaire les requêtes automatiquement.
- Quotas par type de média / par durée de rétention.
- Recommandations personnalisées, watchlists.
