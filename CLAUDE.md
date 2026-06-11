# CLAUDE.md — Jelly Crowd

Configuration agent pour ce dépôt. Lis ce fichier avant toute intervention.

## Overview

**Jelly Crowd** est un **plugin natif Jellyfin** (assembly .NET) qui apporte, intégré directement dans
Jellyfin, l'équivalent d'Overseerr :

1. **Catalogue de découverte TMDB** — parcourir/chercher films & séries, y compris ce qui n'est pas encore
   dans la bibliothèque, avec un marqueur « déjà disponible ».
2. **Requêtes utilisateur** — un user demande un média ; la requête part dans une **file d'attente admin**
   (l'admin approuve/refuse/satisfait). Pas d'intégration Radarr/Sonarr en v1.
3. **Quotas disque par utilisateur** — chaque user a un quota (en octets) configurable ; ses requêtes
   satisfaites consomment son quota. Au-delà, nouvelles requêtes bloquées.

Ce n'est **pas** `jelly-quotas` (app externe React/Node à côté de Jellyfin) — c'est un plugin **dans** Jellyfin.

## Stack & versions

- **.NET 9** (`net9.0`) — Jellyfin **10.11.x**.
- Références host : `Jellyfin.Controller`, `Jellyfin.Model` (`ExcludeAssets=runtime`, fournis par le host).
- UI user-facing via le plugin **Plugin Pages** (`Jellyfin.Plugin.PluginPages`), qui dépend de
  **File Transformation** (IAmParadox27). Versions compatibles 10.11 (Plugin Pages ≥ 2.4.x).
- Persistance : **SQLite** (`Microsoft.Data.Sqlite`) dans le data path du plugin.
- Catalogue : **API TMDB** (clé API requise, stockée en config plugin).
- Licence : **GPL-3.0**.

## Layout

```
Jellyfin.Plugin.JellyCrowd/
  Plugin.cs                  # BasePlugin<PluginConfiguration>, IHasWebPages (page config admin)
  PluginServiceRegistrator.cs# DI : enregistrement services + pages user (Plugin Pages)
  Configuration/
    PluginConfiguration.cs   # clé TMDB, quota défaut, overrides par user, options d'estimation
    configPage.html          # page de config admin (ressource embarquée)
  Api/                       # contrôleurs ASP.NET ControllerBase (REST)
    CatalogController.cs     # proxy TMDB + flag availability
    RequestsController.cs    # create/list/approve/deny/fulfill
    QuotaController.cs       # usage par user, get/set quotas
  Services/
    TmdbClient.cs            # HttpClient TMDB
    LibraryMatcher.cs        # TMDB id <-> items biblio (ILibraryManager)
    RequestStore.cs          # persistance SQLite
    QuotaService.cs          # calcul usage + enforcement
  Tasks/ReconcileTask.cs     # IScheduledTask : recalcul usage + résolution requêtes
  Models/                    # DTOs (RequestRecord, QuotaInfo, CatalogItem...)
  Web/                       # pages user-facing embarquées (HTML/JS/CSS)

Jellyfin.Plugin.JellyCrowd.Tests/  # projet xUnit ; tests exécutés en CI (dotnet test)
```

Racine : `CLAUDE.md`, `ROADMAP.md`, `README.md`, `LICENSE`, `build.yaml` (manifest plugin),
`Directory.Build.props`, `.editorconfig`, `jellyfin.ruleset`, `.sln`.

## Build / test

```powershell
dotnet build -c Release          # nécessite le .NET 9 SDK installé
dotnet test  -c Release          # exécute la suite xUnit (même commande qu'en CI)
```

Le `.dll` produit (`Jellyfin.Plugin.JellyCrowd/bin/Release/net9.0/`) se copie dans le data path Jellyfin :
`<jellyfin-data>/plugins/JellyCrowd_<version>/`. Redémarrer Jellyfin → le plugin apparaît dans
*Dashboard → Plugins* et expose sa page de config.

Pré-requis runtime côté Jellyfin pour les pages user : installer **Plugin Pages** + **File Transformation**
(dépôt `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`).

## Conventions

- **Tout le code et les commentaires en anglais** (identifiants, commentaires, docs XML, messages de log,
  noms de tests). Le français est réservé à la doc projet (`CLAUDE.md`, `ROADMAP.md`, `README.md`) et aux
  chaînes traduites destinées aux utilisateurs (cf. i18n).
- **Indentation : 2 espaces**, pour tous les fichiers (C#, HTML, JS, YAML, XML/csproj). Imposé par `.editorconfig`
  (`indent_size = 2`).
- Namespace racine : `Jellyfin.Plugin.JellyCrowd`.
- Style imposé par `.editorconfig` + `jellyfin.ruleset` (StyleCop) ; `TreatWarningsAsErrors=true`.
- En-tête de licence GPL-3.0 si requis ; documentation XML sur les membres publics.
- DTOs dans `Models/` ; pas de logique métier dans les contrôleurs (déléguer aux `Services/`).
- GUID du plugin : `a1994160-4ea2-4d81-bd3c-ffe825700d98` (ne pas changer).

## Localisation (i18n) — suivre la langue de Jellyfin

**Le plugin doit afficher sa langue en fonction de la langue de Jellyfin**, pas une langue figée.

- Les chaînes destinées à l'utilisateur ne sont **jamais en dur** dans le code/HTML : elles vivent dans des
  catalogues de traduction par langue (`Web/strings/<lang>.json`, ex. `en.json`, `fr.json`).
- **Côté pages user (Plugin Pages)** : détecter la langue active de l'utilisateur Jellyfin (préférence utilisateur
  / locale du client web, fallback `navigator.language` puis `en`) et charger le catalogue correspondant ;
  fallback sur `en` pour toute clé manquante.
- **Côté serveur** (messages d'API/erreurs visibles par l'utilisateur) : prévoir aussi des chaînes localisables ;
  `en` par défaut.
- Langues de base : **en** (défaut/fallback) et **fr**. Ajouter une langue = déposer un nouveau fichier de
  catalogue, sans toucher au code.
- Toute nouvelle chaîne visible par l'utilisateur doit être ajoutée au moins à `en.json` (et idéalement `fr.json`)
  dans la même PR.

## Règle de tests (NON NÉGOCIABLE)

**Toute fonctionnalité ajoutée doit livrer ses tests dans la même PR, et la CI doit les exécuter.**
Concrètement, on n'ajoute rien sans couverture :

- **Chaque route/endpoint** (`Api/*Controller`) → test(s) couvrant le cas nominal + au moins un cas d'erreur
  (non autorisé, entrée invalide, quota dépassé…).
- **Chaque service** (`Services/*`) → tests unitaires de la logique (calcul d'usage, enforcement quota,
  matching biblio, parsing TMDB…).
- **Chaque bouton / interaction UI** → la logique métier déclenchée doit être testable et testée côté backend ;
  pour le comportement front (Plugin Pages), extraire le JS dans des fonctions pures testables et/ou ajouter un
  test e2e si pertinent. Pas de logique non testée cachée dans le HTML.
- Un PR qui ajoute du code sans test associé est considéré **incomplet**.
- La CI (`.github/workflows/build.yml`) lance `dotnet test` ; un test rouge **bloque** le merge.

Le projet de tests .NET vit dans `Jellyfin.Plugin.JellyCrowd.Tests/` (xUnit). Il référence les assemblies
Jellyfin **sans** `ExcludeAssets` pour disposer du runtime à l'exécution.

**Tests JS** : la logique front pure est isolée dans `Web/*.lib.js` (wrapper UMD : global navigateur +
module CommonJS) et testée via `node --test tests/js/*.test.js` (sans dépendance). La CI lance ces deux
suites (.NET + JS). Tout nouveau bouton/interaction expose sa logique dans un `*.lib.js` testé.

## Versionning automatique & CI/CD

- **CI** (`build.yml`) : sur push `main` et chaque PR → restore + build Release + `dotnet test` + package `.zip`.
- **Release** (`release.yml`) : sur push `main`, versionning sémantique auto piloté par un **mot-clé du message
  de commit** :
  - `[major]` ou `#major` → bump MAJEUR
  - `[minor]` ou `#minor` → bump MINEUR
  - sinon → bump PATCH
  - `[skip release]` → pas de release
  Le workflow estampille la version dans `Directory.Build.props` + `build.yaml`, commit `chore(release): vX.Y.Z [skip ci]`,
  pose le tag `vX.Y.Z`, et publie une **Release GitHub** avec le `.zip` du plugin + son `.md5`.
- Pas de CD vers un dépôt plugin installable pour l'instant (prévu M5 si besoin).
- ⚠️ La Release pousse un commit sur `main` : si une **protection de branche** est activée, autoriser
  `github-actions[bot]` à pousser (ou utiliser un PAT dédié).

## Contraintes clés (à ne pas oublier)

- **Attribution des quotas** : Jellyfin ne sait pas « qui a demandé quoi ». C'est **Jelly Crowd** qui possède
  ce mapping (requête → item). Usage_user = Σ tailles fichiers des items liés à ses requêtes satisfaites.
  La taille réelle n'est connue qu'**après** satisfaction → enforcement à la création basé sur usage actuel +
  estimation configurable.
- **Compat** : ne pas casser 10.11 / net9. Les pages user passent **uniquement** par Plugin Pages.
- **Auth des contrôleurs (10.11)** : il n'existe PAS de policy nommée `DefaultAuthorization`. Pour un endpoint
  utilisateur authentifié → `[Authorize]` (policy par défaut). Pour un endpoint admin → `[Authorize(Policy = "RequiresElevation")]`.
  Assets statiques publics → `[AllowAnonymous]`.
- Implémentation **milestone par milestone** (voir `ROADMAP.md`) ; M0 = scaffold qui se charge dans Jellyfin.

## Décisions d'architecture validées

| Sujet | Choix |
|-------|-------|
| Fulfillment | File d'attente admin (pas de Radarr/Sonarr en v1) |
| Catalogue | TMDB (découverte) + croisement biblio Jellyfin |
| UI | Plugin Pages (pages user-facing thémées) |
| Version | Jellyfin 10.11.x / .NET 9 |
