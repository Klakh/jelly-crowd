<div align="center">

# 🎬 Jelly Crowd

**Un catalogue de découverte et un système de requêtes — façon Overseerr — directement intégré dans Jellyfin, avec quotas disque par utilisateur.**

[![License: GPL-3.0](https://img.shields.io/badge/License-GPLv3-blue.svg?style=for-the-badge)](https://www.gnu.org/licenses/gpl-3.0)
![Jellyfin 10.11](https://img.shields.io/badge/Jellyfin-10.11.x-00A4DC?style=for-the-badge&logo=jellyfin)
![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)

</div>

---

**Jelly Crowd** est un **plugin Jellyfin natif**. Contrairement aux services externes type Overseerr/Jellyseerr
qui tournent à côté du serveur, Jelly Crowd vit **dans** Jellyfin et réutilise ses utilisateurs, son
authentification et son thème.

## ✨ Fonctionnalités

- 🍿 **Catalogue de découverte (TMDB)** — parcours et recherche de films/séries, y compris ce qui n'est pas
  encore dans la bibliothèque, avec un marqueur « déjà disponible ».
- 📝 **Requêtes utilisateur** — les utilisateurs demandent un média ; les requêtes arrivent dans une
  **file d'attente admin** (approbation/refus). *(Intégration Radarr/Sonarr prévue ultérieurement.)*
- 💾 **Quotas disque par utilisateur** — quota configurable (en octets) par utilisateur ; les requêtes
  satisfaites consomment le quota, et les nouvelles requêtes sont bloquées au-delà.
- 🎨 **Pages utilisateur intégrées** — UI thématisée via le plugin [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages).

> Pour le détail des phases de développement, voir [`ROADMAP.md`](./ROADMAP.md).

## 📦 Pré-requis

- **Jellyfin 10.11.x**
- Plugins [**Plugin Pages**](https://github.com/IAmParadox27/jellyfin-plugin-pages) et
  [**File Transformation**](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)
  (dépôt : `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`)
- Une **clé API TMDB** (gratuite) pour le catalogue.

## 🚀 Installation (en développement)

```powershell
dotnet build -c Release
```

Copier le `.dll` produit dans `<jellyfin-data>/plugins/JellyCrowd/`, puis redémarrer Jellyfin.
Le plugin apparaît dans **Dashboard → Plugins**. Renseigner la clé TMDB et les quotas dans sa page de config.

## 🛠️ Développement

Voir [`CLAUDE.md`](./CLAUDE.md) pour l'architecture, les conventions et les commandes.

## 📄 Licence

[GPL-3.0](./LICENSE) — aligné sur l'écosystème des plugins Jellyfin.
