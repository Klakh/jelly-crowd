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
```

Racine : `CLAUDE.md`, `ROADMAP.md`, `README.md`, `LICENSE`, `build.yaml` (manifest plugin),
`Directory.Build.props`, `.editorconfig`, `jellyfin.ruleset`, `.sln`.

## Build / test

```powershell
dotnet build -c Release          # nécessite le .NET 9 SDK installé
```

Le `.dll` produit (`Jellyfin.Plugin.JellyCrowd/bin/Release/net9.0/`) se copie dans le data path Jellyfin :
`<jellyfin-data>/plugins/JellyCrowd_<version>/`. Redémarrer Jellyfin → le plugin apparaît dans
*Dashboard → Plugins* et expose sa page de config.

Pré-requis runtime côté Jellyfin pour les pages user : installer **Plugin Pages** + **File Transformation**
(dépôt `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`).

## Conventions

- Namespace racine : `Jellyfin.Plugin.JellyCrowd`.
- Style imposé par `.editorconfig` + `jellyfin.ruleset` (StyleCop) ; `TreatWarningsAsErrors=true`.
- En-tête de licence GPL-3.0 si requis ; documentation XML sur les membres publics.
- DTOs dans `Models/` ; pas de logique métier dans les contrôleurs (déléguer aux `Services/`).
- GUID du plugin : `a1994160-4ea2-4d81-bd3c-ffe825700d98` (ne pas changer).

## Contraintes clés (à ne pas oublier)

- **Attribution des quotas** : Jellyfin ne sait pas « qui a demandé quoi ». C'est **Jelly Crowd** qui possède
  ce mapping (requête → item). Usage_user = Σ tailles fichiers des items liés à ses requêtes satisfaites.
  La taille réelle n'est connue qu'**après** satisfaction → enforcement à la création basé sur usage actuel +
  estimation configurable.
- **Compat** : ne pas casser 10.11 / net9. Les pages user passent **uniquement** par Plugin Pages.
- Implémentation **milestone par milestone** (voir `ROADMAP.md`) ; M0 = scaffold qui se charge dans Jellyfin.

## Décisions d'architecture validées

| Sujet | Choix |
|-------|-------|
| Fulfillment | File d'attente admin (pas de Radarr/Sonarr en v1) |
| Catalogue | TMDB (découverte) + croisement biblio Jellyfin |
| UI | Plugin Pages (pages user-facing thémées) |
| Version | Jellyfin 10.11.x / .NET 9 |
