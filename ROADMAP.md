# Roadmap — Jelly Crowd

Plugin Jellyfin (Overseerr-like) : catalogue TMDB + requêtes en file admin + quotas disque par utilisateur.
Cible : **Jellyfin 10.11.x / .NET 9**. Implémentation milestone par milestone.

Légende : ☐ à faire · ☑ fait · ◐ en cours

---

## M0 — Scaffolding & build  ◐

Objectif : un plugin vide qui **compile et se charge** dans Jellyfin 10.11, avec une page de config admin.

- ☑ Structure du dépôt + fichiers racine (`CLAUDE.md`, `ROADMAP.md`, `README.md`, `LICENSE`, `.gitignore`).
- ☐ Projet .NET : `.sln`, `.csproj` (net9.0), `Directory.Build.props`, `.editorconfig`, `jellyfin.ruleset`.
- ☐ `build.yaml` (manifest plugin : guid, `targetAbi 10.11.x`, framework `net9.0`, artefact dll).
- ☐ `Plugin.cs` (`BasePlugin<PluginConfiguration>`, `IHasWebPages`) + `PluginConfiguration.cs` + `configPage.html`.
- ☐ CI GitHub : build `Release` + package `.zip` du plugin.
- ☐ **Vérif** : `dotnet build -c Release` OK ; dll chargée dans *Dashboard → Plugins*, page config s'ouvre.

## M1 — Catalogue TMDB  ◐

Objectif : parcourir et chercher le catalogue TMDB depuis une page user.

- ☑ `TmdbClient`/`ITmdbClient` (HttpClient, clé API depuis la config) + `TmdbResponseParser` (testé).
- ☑ `CatalogController` : `Trending`, `Search`, `Details/{type}/{id}` (auth, langue, 400/404/503) + tests.
- ☑ Champ clé API TMDB dans la page de config admin.
- ☑ Assets page user `catalog` (HTML/JS/CSS) + i18n en/fr, servis par `WebController` (testé).
- ☑ **Enregistrement Plugin Pages** câblé (`PluginPageRegistrationService` via `IPluginPagesManager`, tolérant à l'absence) + tests JS (`node:test`).
- ☐ **Vérif (instance live)** : installer Plugin Pages + File Transformation, vérifier que la page « Jelly Crowd » apparaît et liste les résultats TMDB (browse + recherche). Ajuster `PageUrl` si nécessaire.

## M2 — Requêtes (file d'attente admin)  ☐

Objectif : créer des requêtes et les gérer côté admin.

- ☐ `RequestStore` (SQLite) : table `requests` (id, userId, tmdbId, type, titre, statut, dates...).
- ☐ `RequestsController` : `create` (user), `list` (user/admin), `approve`/`deny` (admin).
- ☐ Bouton « Demander » sur la fiche + page user « Mes requêtes ».
- ☐ File d'approbation dans la page de config admin (liste + approuver/refuser).
- ☐ **Vérif** : un user crée une requête, l'admin la voit et l'approuve/refuse, statut mis à jour.

## M3 — Disponibilité bibliothèque  ☐

Objectif : savoir ce qui existe déjà et résoudre automatiquement les requêtes satisfaites.

- ☐ `LibraryMatcher` : recherche d'un item par `ProviderId` Tmdb via `ILibraryManager`.
- ☐ Flag `available` ajouté aux résultats du catalogue.
- ☐ `ReconcileTask` (`IScheduledTask`) : lie les requêtes approuvées aux nouveaux items, passe en `available`.
- ☐ **Vérif** : un titre déjà en biblio est marqué « disponible » ; une requête se résout quand l'item arrive.

## M4 — Quotas disque par utilisateur  ☐

Objectif : limiter l'occupation disque par user et bloquer au-delà.

- ☐ `QuotaService` : usage_user = Σ tailles fichiers des items liés aux requêtes satisfaites du user.
- ☐ Config : quota global par défaut + overrides par user + tailles d'estimation (film/épisode).
- ☐ `QuotaController` : usage par user ; get/set quotas (admin).
- ☐ Enforcement à la création de requête : refus si `usage + estimation > quota`.
- ☐ Affichage usage/quota côté user (barre de progression).
- ☐ **Vérif** : un user au-delà de son quota ne peut plus créer de requête ; l'usage reflète la biblio.

## M5 — Finition & distribution  ☐

- ☐ Notifications (requête approuvée / disponible / quota atteint).
- ☐ i18n FR/EN.
- ☐ Thème & UX alignés sur Jellyfin (via Plugin Pages).
- ☐ Manifest de dépôt plugin installable + doc d'installation.
- ☐ **Vérif** : install propre depuis un dépôt plugin sur une instance neuve.

---

## Hors périmètre v1 (idées futures)

- Intégration **Radarr/Sonarr** pour satisfaire les requêtes automatiquement.
- Quotas par type de média / par durée de rétention.
- Recommandations personnalisées, watchlists.
