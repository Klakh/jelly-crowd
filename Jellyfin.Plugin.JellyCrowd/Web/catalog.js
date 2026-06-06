/*
 * Jelly Crowd — catalog page.
 * Browses the TMDB discovery catalog (trending + search). Requests come in M2.
 * All user-visible strings come from Web/strings/<lang>.json so the UI follows
 * the active Jellyfin/browser language (fallback: en).
 */
(function () {
  'use strict';

  var SUPPORTED_LANGS = ['en', 'fr'];
  var POSTER_BASE = 'https://image.tmdb.org/t/p/w342';
  var lib = window.JellyCrowdLib;
  var strings = {};

  // Full locale (e.g. "fr-FR") sent to TMDB; the 2-letter code picks the string catalog.
  function fullLocale() {
    return (navigator.language || 'en-US');
  }

  function shortLang() {
    return lib.pickLang(fullLocale(), SUPPORTED_LANGS);
  }

  function t(key) {
    return Object.prototype.hasOwnProperty.call(strings, key) ? strings[key] : key;
  }

  // Same-origin URL for one of our endpoints/assets, honoring the Jellyfin base path.
  function pluginUrl(path) {
    if (window.ApiClient && typeof window.ApiClient.getUrl === 'function') {
      return window.ApiClient.getUrl(path);
    }

    return '/' + path;
  }

  // Authenticated JSON GET against our API (ApiClient injects the access token).
  function apiGet(path) {
    if (window.ApiClient && typeof window.ApiClient.ajax === 'function') {
      return window.ApiClient.ajax({ type: 'GET', url: pluginUrl(path), dataType: 'json' });
    }

    return fetch(pluginUrl(path)).then(function (r) {
      if (!r.ok) {
        var err = new Error('HTTP ' + r.status);
        err.status = r.status;
        throw err;
      }

      return r.json();
    });
  }

  function loadStrings() {
    return fetch(pluginUrl('JellyCrowd/Web/strings/' + shortLang() + '.json'))
      .then(function (r) { return r.ok ? r.json() : {}; })
      .catch(function () { return {}; })
      .then(function (loaded) { strings = loaded || {}; });
  }

  function renderCard(item) {
    var card = document.createElement('div');
    card.className = 'jellycrowd-card';

    if (item.PosterPath) {
      var img = document.createElement('img');
      img.className = 'jellycrowd-poster';
      img.loading = 'lazy';
      img.alt = item.Title || '';
      img.src = POSTER_BASE + item.PosterPath;
      card.appendChild(img);
    } else {
      var placeholder = document.createElement('div');
      placeholder.className = 'jellycrowd-poster jellycrowd-poster-empty';
      card.appendChild(placeholder);
    }

    if (item.Available) {
      var badge = document.createElement('span');
      badge.className = 'jellycrowd-badge';
      badge.textContent = t('available_badge');
      card.appendChild(badge);
    }

    var title = document.createElement('div');
    title.className = 'jellycrowd-card-title';
    title.textContent = lib.formatTitle(item);
    card.appendChild(title);

    return card;
  }

  function setMessage(text) {
    var el = document.getElementById('jcMessage');
    if (text) {
      el.textContent = text;
      el.hidden = false;
    } else {
      el.hidden = true;
    }
  }

  function renderItems(items) {
    var grid = document.getElementById('jcGrid');
    grid.innerHTML = '';

    if (!items || items.length === 0) {
      setMessage(t('no_results'));
      return;
    }

    setMessage('');
    items.forEach(function (item) { grid.appendChild(renderCard(item)); });
  }

  function showError(error) {
    document.getElementById('jcGrid').innerHTML = '';
    setMessage(t(lib.errorKey(error && error.status)));
  }

  function load(path, sectionTitleKey) {
    document.getElementById('jcSectionTitle').textContent = t(sectionTitleKey);
    setMessage(t('loading'));
    apiGet(path + (path.indexOf('?') >= 0 ? '&' : '?') + 'language=' + encodeURIComponent(fullLocale()))
      .then(renderItems)
      .catch(showError);
  }

  function loadTrending() {
    load('JellyCrowd/Catalog/Trending', 'trending_title');
  }

  function search(query) {
    if (!query) {
      loadTrending();
      return;
    }

    load('JellyCrowd/Catalog/Search?query=' + encodeURIComponent(query), 'results_title');
  }

  function applyStaticText() {
    document.getElementById('jcTitle').textContent = t('app_title');
    document.getElementById('jcSearchInput').placeholder = t('search_placeholder');
    document.getElementById('jcSearchButton').textContent = t('search_button');
  }

  function init() {
    loadStrings().then(function () {
      applyStaticText();

      document.getElementById('jcSearchForm').addEventListener('submit', function (e) {
        e.preventDefault();
        search(document.getElementById('jcSearchInput').value.trim());
      });

      loadTrending();
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
