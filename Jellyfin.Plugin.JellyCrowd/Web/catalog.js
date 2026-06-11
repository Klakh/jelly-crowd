/*
 * Jelly Crowd — catalog page.
 * Browses the TMDB discovery catalog (trending + search), opens a details modal on click,
 * and lets users request titles. All user-visible strings come from Web/strings/<lang>.json
 * so the UI follows the active Jellyfin/browser language (fallback: en).
 */
(function () {
  'use strict';

  var SUPPORTED_LANGS = ['en', 'fr'];
  var POSTER_BASE = 'https://image.tmdb.org/t/p/w342';
  var BACKDROP_BASE = 'https://image.tmdb.org/t/p/w780';
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

  // Authenticated JSON POST against our API.
  function apiPost(path, body) {
    if (window.ApiClient && typeof window.ApiClient.ajax === 'function') {
      return window.ApiClient.ajax({
        type: 'POST',
        url: pluginUrl(path),
        data: JSON.stringify(body),
        contentType: 'application/json',
        dataType: 'json'
      });
    }

    return fetch(pluginUrl(path), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    }).then(function (r) {
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
    card.className = 'jellycrowd-card jellycrowd-card-clickable';
    card.addEventListener('click', function () { openModal(item); });

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

    if (!item.Available) {
      var button = document.createElement('button');
      button.className = 'jellycrowd-request';
      button.type = 'button';
      button.textContent = t('request_button');
      button.addEventListener('click', function (e) {
        e.stopPropagation();
        requestItem(item, button);
      });
      card.appendChild(button);
    }

    return card;
  }

  function openModal(item) {
    var overlay = document.createElement('div');
    overlay.className = 'jellycrowd-modal-overlay';

    var modal = document.createElement('div');
    modal.className = 'jellycrowd-modal';
    if (item.BackdropPath) {
      modal.style.backgroundImage = 'url("' + BACKDROP_BASE + item.BackdropPath + '")';
    }

    var close = document.createElement('button');
    close.type = 'button';
    close.className = 'jellycrowd-modal-close';
    close.setAttribute('aria-label', t('close'));
    close.textContent = '✕';

    var body = document.createElement('div');
    body.className = 'jellycrowd-modal-body';

    var title = document.createElement('h2');
    title.className = 'jellycrowd-modal-title';
    title.textContent = lib.formatTitle(item);
    body.appendChild(title);

    var meta = document.createElement('div');
    meta.className = 'jellycrowd-modal-meta';
    var rating = lib.formatRating(item.VoteAverage);
    if (rating) {
      var ratingSpan = document.createElement('span');
      ratingSpan.textContent = '★ ' + rating;
      meta.appendChild(ratingSpan);
    }

    if (item.Available) {
      var availSpan = document.createElement('span');
      availSpan.textContent = t('available_badge');
      meta.appendChild(availSpan);
    }

    body.appendChild(meta);

    var overview = document.createElement('p');
    overview.className = 'jellycrowd-modal-overview';
    overview.textContent = item.Overview || t('no_overview');
    body.appendChild(overview);

    if (!item.Available) {
      var requestButton = document.createElement('button');
      requestButton.className = 'jellycrowd-request';
      requestButton.type = 'button';
      requestButton.textContent = t('request_button');
      requestButton.addEventListener('click', function () { requestItem(item, requestButton); });
      body.appendChild(requestButton);
    }

    modal.appendChild(close);
    modal.appendChild(body);
    overlay.appendChild(modal);
    document.body.appendChild(overlay);

    function dismiss() {
      overlay.remove();
      document.removeEventListener('keydown', onKey);
    }

    function onKey(e) {
      if (e.key === 'Escape') {
        dismiss();
      }
    }

    close.addEventListener('click', dismiss);
    overlay.addEventListener('click', function (e) {
      if (e.target === overlay) {
        dismiss();
      }
    });
    document.addEventListener('keydown', onKey);
  }

  function requestItem(item, button) {
    button.disabled = true;
    button.textContent = t('requesting');
    apiPost('JellyCrowd/Requests', {
      TmdbId: item.TmdbId,
      MediaType: item.MediaType,
      Title: item.Title,
      PosterPath: item.PosterPath
    }).then(function () {
      button.textContent = t('requested');
    }).catch(function (error) {
      if (error && error.status === 409) {
        button.textContent = t('already_requested');
      } else {
        button.disabled = false;
        button.textContent = t('request_button');
      }
    });
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
