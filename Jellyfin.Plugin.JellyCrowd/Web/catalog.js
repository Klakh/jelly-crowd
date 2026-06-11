/*
 * Jelly Crowd — catalog page.
 * Browse/discover the TMDB catalog with genre/year/rating/sort filters, search, a hover preview,
 * a details modal (genres, runtime, TMDB/IMDb links) and per-title requests. All user-visible
 * strings come from Web/strings/<lang>.json so the UI follows the active Jellyfin/browser language.
 */
(function () {
  'use strict';

  var SUPPORTED_LANGS = ['en', 'fr'];
  var POSTER_BASE = 'https://image.tmdb.org/t/p/w342';
  var BACKDROP_BASE = 'https://image.tmdb.org/t/p/w780';
  var lib = window.JellyCrowdLib;
  var strings = {};
  var quotaExceeded = false;

  var MIN_YEAR = 1900;
  var MAX_YEAR = new Date().getFullYear();

  var filters = {
    mediaType: 'movie',
    genres: [],
    minYear: MIN_YEAR,
    maxYear: MAX_YEAR,
    minRating: 0,
    maxRating: 10,
    sortBy: 'popularity'
  };

  function fullLocale() {
    return navigator.language || 'en-US';
  }

  function shortLang() {
    return lib.pickLang(fullLocale(), SUPPORTED_LANGS);
  }

  function t(key) {
    return Object.prototype.hasOwnProperty.call(strings, key) ? strings[key] : key;
  }

  function pluginUrl(path) {
    if (window.ApiClient && typeof window.ApiClient.getUrl === 'function') {
      return window.ApiClient.getUrl(path);
    }
    return '/' + path;
  }

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

  // ---------- cards ----------

  function navigateToItem(itemId) {
    var hash = '#/details?id=' + itemId;
    if (window.ApiClient && typeof window.ApiClient.serverId === 'function') {
      hash += '&serverId=' + window.ApiClient.serverId();
    }
    window.location.hash = hash;
  }

  // A disabled "request" button carrying the quota-exceeded explanation.
  function blockedRequestButton() {
    var button = document.createElement('button');
    button.className = 'jellycrowd-request jellycrowd-request-blocked';
    button.type = 'button';
    button.disabled = true;
    button.title = t('quota_full_hint');
    button.textContent = t('request_button');
    button.addEventListener('click', function (e) { e.stopPropagation(); });
    return button;
  }

  function renderCard(item) {
    var card = document.createElement('div');
    card.className = 'jellycrowd-card jellycrowd-card-clickable';
    card.addEventListener('click', function () {
      if (item.Available && item.JellyfinItemId) {
        navigateToItem(item.JellyfinItemId);
      } else {
        openModal(item);
      }
    });

    var posterWrap = document.createElement('div');
    posterWrap.className = 'jellycrowd-poster-wrap';

    if (item.PosterPath) {
      var img = document.createElement('img');
      img.className = 'jellycrowd-poster';
      img.loading = 'lazy';
      img.alt = item.Title || '';
      img.src = POSTER_BASE + item.PosterPath;
      posterWrap.appendChild(img);
    } else {
      var placeholder = document.createElement('div');
      placeholder.className = 'jellycrowd-poster jellycrowd-poster-empty';
      posterWrap.appendChild(placeholder);
    }

    if (item.Available) {
      var badge = document.createElement('span');
      badge.className = 'jellycrowd-badge';
      badge.textContent = t('available_badge');
      posterWrap.appendChild(badge);
    }

    var hover = document.createElement('div');
    hover.className = 'jellycrowd-hover';
    var rating = lib.formatRating(item.VoteAverage);
    var year = lib.yearOf(item);
    hover.textContent = [rating ? '★ ' + rating : '', year].filter(Boolean).join('  ·  ');
    posterWrap.appendChild(hover);

    card.appendChild(posterWrap);

    var title = document.createElement('div');
    title.className = 'jellycrowd-card-title';
    title.textContent = lib.formatTitle(item);
    card.appendChild(title);

    if (!item.Available) {
      if (quotaExceeded) {
        card.appendChild(blockedRequestButton());
      } else {
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
    }

    return card;
  }

  function requestItem(item, button) {
    button.disabled = true;
    button.textContent = t('requesting');
    apiPost('JellyCrowd/Requests', {
      TmdbId: item.TmdbId,
      MediaType: item.MediaType,
      Title: item.Title,
      PosterPath: item.PosterPath,
      ReleaseDate: item.ReleaseDate
    }).then(function () {
      button.textContent = t('requested');
    }).catch(function (error) {
      if (error && error.status === 409) {
        button.textContent = t('already_requested');
      } else if (error && error.status === 403) {
        button.textContent = t('quota_exceeded');
      } else {
        button.disabled = false;
        button.textContent = t('request_button');
      }
    });
  }

  // ---------- modal ----------

  function externalLink(href, text) {
    var a = document.createElement('a');
    a.href = href;
    a.target = '_blank';
    a.rel = 'noopener noreferrer';
    a.textContent = text;
    return a;
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

    if (item.PosterPath) {
      var poster = document.createElement('img');
      poster.className = 'jellycrowd-modal-poster';
      poster.loading = 'lazy';
      poster.alt = item.Title || '';
      poster.src = POSTER_BASE + item.PosterPath;
      body.appendChild(poster);
    }

    var content = document.createElement('div');
    content.className = 'jellycrowd-modal-content';

    var title = document.createElement('h2');
    title.className = 'jellycrowd-modal-title';
    title.textContent = lib.formatTitle(item);
    content.appendChild(title);

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
    content.appendChild(meta);

    var genresEl = document.createElement('div');
    genresEl.className = 'jellycrowd-modal-genres';
    content.appendChild(genresEl);

    var overview = document.createElement('p');
    overview.className = 'jellycrowd-modal-overview';
    overview.textContent = item.Overview || t('no_overview');
    content.appendChild(overview);

    var links = document.createElement('div');
    links.className = 'jellycrowd-modal-links';
    links.appendChild(externalLink('https://www.themoviedb.org/' + item.MediaType + '/' + item.TmdbId, t('view_tmdb')));
    content.appendChild(links);

    if (!item.Available) {
      if (quotaExceeded) {
        content.appendChild(blockedRequestButton());
      } else {
        var requestButton = document.createElement('button');
        requestButton.className = 'jellycrowd-request';
        requestButton.type = 'button';
        requestButton.textContent = t('request_button');
        requestButton.addEventListener('click', function () { requestItem(item, requestButton); });
        content.appendChild(requestButton);
      }
    }

    body.appendChild(content);

    // Enrich with full details (genres, runtime, IMDb link).
    apiGet('JellyCrowd/Catalog/Details/' + item.MediaType + '/' + item.TmdbId + '?language=' + encodeURIComponent(fullLocale()))
      .then(function (details) {
        if (!details) {
          return;
        }
        (details.Genres || []).forEach(function (name) {
          var chip = document.createElement('span');
          chip.className = 'jellycrowd-chip';
          chip.textContent = name;
          genresEl.appendChild(chip);
        });
        if (details.Runtime) {
          var rt = document.createElement('span');
          rt.textContent = details.Runtime + ' ' + t('runtime_min');
          meta.appendChild(rt);
        }
        if (details.ImdbId) {
          links.appendChild(externalLink('https://www.imdb.com/title/' + details.ImdbId, t('view_imdb')));
        }
      })
      .catch(function () { /* details are best-effort */ });

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

  // ---------- results ----------

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

  function discoverPath() {
    var p = 'JellyCrowd/Catalog/Discover?mediaType=' + filters.mediaType
      + '&language=' + encodeURIComponent(fullLocale())
      + '&sortBy=' + encodeURIComponent(filters.sortBy);
    if (filters.genres.length) {
      p += '&genres=' + encodeURIComponent(filters.genres.join(','));
    }
    if (filters.minYear > MIN_YEAR) {
      p += '&minYear=' + filters.minYear;
    }
    if (filters.maxYear < MAX_YEAR) {
      p += '&maxYear=' + filters.maxYear;
    }
    if (filters.minRating > 0) {
      p += '&minRating=' + filters.minRating;
    }
    if (filters.maxRating < 10) {
      p += '&maxRating=' + filters.maxRating;
    }
    return p;
  }

  function loadDiscover() {
    document.getElementById('jcSectionTitle').textContent = t('browse_title');
    setMessage(t('loading'));
    apiGet(discoverPath()).then(renderItems).catch(showError);
  }

  function search(query) {
    if (!query) {
      loadDiscover();
      return;
    }
    document.getElementById('jcSectionTitle').textContent = t('results_title');
    setMessage(t('loading'));
    apiGet('JellyCrowd/Catalog/Search?query=' + encodeURIComponent(query) + '&language=' + encodeURIComponent(fullLocale()))
      .then(renderItems)
      .catch(showError);
  }

  // ---------- filters UI ----------

  function dualSlider(container, opts) {
    container.innerHTML = '';
    var track = document.createElement('div');
    track.className = 'jellycrowd-dual-track';
    var fill = document.createElement('div');
    fill.className = 'jellycrowd-dual-fill';

    function makeInput(value) {
      var input = document.createElement('input');
      input.type = 'range';
      input.min = String(opts.min);
      input.max = String(opts.max);
      input.step = String(opts.step);
      input.value = String(value);
      return input;
    }

    var low = makeInput(opts.low);
    var high = makeInput(opts.high);

    var values = document.createElement('div');
    values.className = 'jellycrowd-dual-values';
    var lowLabel = document.createElement('span');
    var highLabel = document.createElement('span');
    values.appendChild(lowLabel);
    values.appendChild(highLabel);

    container.appendChild(track);
    container.appendChild(fill);
    container.appendChild(low);
    container.appendChild(high);
    container.appendChild(values);

    function pct(v) {
      return ((v - opts.min) / (opts.max - opts.min)) * 100;
    }
    function refresh(fire) {
      var pair = lib.orderPair(Number(low.value), Number(high.value));
      fill.style.left = pct(pair[0]) + '%';
      fill.style.width = (pct(pair[1]) - pct(pair[0])) + '%';
      lowLabel.textContent = opts.format(pair[0]);
      highLabel.textContent = opts.format(pair[1]);
      if (fire) {
        opts.onChange(pair[0], pair[1]);
      }
    }
    low.addEventListener('input', function () { refresh(false); });
    high.addEventListener('input', function () { refresh(false); });
    low.addEventListener('change', function () { refresh(true); });
    high.addEventListener('change', function () { refresh(true); });
    refresh(false);
  }

  function setupYearSlider() {
    dualSlider(document.getElementById('jcYear'), {
      min: MIN_YEAR, max: MAX_YEAR, step: 1, low: filters.minYear, high: filters.maxYear,
      format: function (v) { return String(Math.round(v)); },
      onChange: function (lo, hi) { filters.minYear = Math.round(lo); filters.maxYear = Math.round(hi); loadDiscover(); }
    });
  }

  function setupRatingSlider() {
    dualSlider(document.getElementById('jcRating'), {
      min: 0, max: 10, step: 0.5, low: filters.minRating, high: filters.maxRating,
      format: function (v) { return Number(v).toFixed(1); },
      onChange: function (lo, hi) { filters.minRating = lo; filters.maxRating = hi; loadDiscover(); }
    });
  }

  function buildSort() {
    var select = document.getElementById('jcSort');
    select.innerHTML = '';
    [['popularity', 'sort_popularity'], ['rating', 'sort_rating'], ['release', 'sort_release']].forEach(function (pair) {
      var opt = document.createElement('option');
      opt.value = pair[0];
      opt.textContent = t(pair[1]);
      select.appendChild(opt);
    });
    select.value = filters.sortBy;
    select.addEventListener('change', function () { filters.sortBy = select.value; loadDiscover(); });
  }

  function renderGenres(genres) {
    var container = document.getElementById('jcGenres');
    container.innerHTML = '';
    (genres || []).forEach(function (genre) {
      var chip = document.createElement('button');
      chip.type = 'button';
      chip.className = 'jellycrowd-chip';
      chip.textContent = genre.Name;
      chip.addEventListener('click', function () {
        var id = String(genre.Id);
        var idx = filters.genres.indexOf(id);
        if (idx >= 0) {
          filters.genres.splice(idx, 1);
          chip.classList.remove('jellycrowd-chip-active');
        } else {
          filters.genres.push(id);
          chip.classList.add('jellycrowd-chip-active');
        }
        loadDiscover();
      });
      container.appendChild(chip);
    });
  }

  function loadGenres() {
    apiGet('JellyCrowd/Catalog/Genres/' + filters.mediaType + '?language=' + encodeURIComponent(fullLocale()))
      .then(renderGenres)
      .catch(function () { renderGenres([]); });
  }

  function setMediaType(type) {
    if (filters.mediaType === type) {
      return;
    }
    filters.mediaType = type;
    filters.genres = [];
    document.getElementById('jcTypeMovie').classList.toggle('jellycrowd-chip-active', type === 'movie');
    document.getElementById('jcTypeTv').classList.toggle('jellycrowd-chip-active', type === 'tv');
    loadGenres();
    loadDiscover();
  }

  function resetFilters() {
    filters.genres = [];
    filters.minYear = MIN_YEAR;
    filters.maxYear = MAX_YEAR;
    filters.minRating = 0;
    filters.maxRating = 10;
    filters.sortBy = 'popularity';
    document.getElementById('jcSort').value = 'popularity';
    document.getElementById('jcSearchInput').value = '';
    setupYearSlider();
    setupRatingSlider();
    loadGenres();
    loadDiscover();
  }

  function applyStaticText() {
    document.getElementById('jcTitle').textContent = t('app_title');
    document.getElementById('jcSearchInput').placeholder = t('search_placeholder');
    document.getElementById('jcSearchButton').textContent = t('search_button');
    document.getElementById('jcLabelType').textContent = t('filters_type');
    document.getElementById('jcTypeMovie').textContent = t('type_movies');
    document.getElementById('jcTypeTv').textContent = t('type_shows');
    document.getElementById('jcLabelGenres').textContent = t('filters_genres');
    document.getElementById('jcLabelYear').textContent = t('filters_year');
    document.getElementById('jcLabelRating').textContent = t('filters_rating');
    document.getElementById('jcLabelSort').textContent = t('filters_sort');
    document.getElementById('jcReset').textContent = t('filters_reset');
  }

  function init() {
    loadStrings().then(function () {
      applyStaticText();
      buildSort();
      setupYearSlider();
      setupRatingSlider();

      document.getElementById('jcTypeMovie').addEventListener('click', function () { setMediaType('movie'); });
      document.getElementById('jcTypeTv').addEventListener('click', function () { setMediaType('tv'); });
      document.getElementById('jcReset').addEventListener('click', resetFilters);

      document.getElementById('jcSearchForm').addEventListener('submit', function (e) {
        e.preventDefault();
        search(document.getElementById('jcSearchInput').value.trim());
      });

      apiGet('JellyCrowd/Quota/Me')
        .then(function (q) {
          quotaExceeded = !!(q && !q.Unlimited && q.QuotaBytes > 0 && q.UsedBytes >= q.QuotaBytes);
        })
        .catch(function () { /* quota check is best-effort */ })
        .then(function () {
          loadGenres();
          loadDiscover();
        });
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
