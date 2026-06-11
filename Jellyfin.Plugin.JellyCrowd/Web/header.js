/*
 * Jelly Crowd — header injection.
 * Loaded into the Jellyfin web client (via File Transformation) to add Catalog / My Requests links
 * and a compact quota bar into the top header. The header DOM is not a public contract, so the
 * selectors below may need tweaking per Jellyfin version.
 */
(function () {
  'use strict';

  var SUPPORTED = ['en', 'fr'];
  var strings = {};

  function getUrl(p) {
    return (window.ApiClient && window.ApiClient.getUrl) ? window.ApiClient.getUrl(p) : '/' + p;
  }

  function lang() {
    var code = (navigator.language || 'en').slice(0, 2).toLowerCase();
    return SUPPORTED.indexOf(code) >= 0 ? code : 'en';
  }

  function t(key) {
    return Object.prototype.hasOwnProperty.call(strings, key) ? strings[key] : key;
  }

  function loadStrings() {
    return fetch(getUrl('JellyCrowd/Web/strings/' + lang() + '.json'))
      .then(function (r) { return r.ok ? r.json() : {}; })
      .then(function (d) { strings = d || {}; })
      .catch(function () { strings = {}; });
  }

  function pageHash(file) {
    return '#/userpluginsettings.html?pageUrl=' + encodeURIComponent('/JellyCrowd/Web/' + file);
  }

  function navButton(label, file) {
    var a = document.createElement('a');
    a.textContent = label;
    a.href = pageHash(file);
    a.style.cssText = 'margin:0 .5em;color:inherit;text-decoration:none;font-size:.9em;cursor:pointer;align-self:center;white-space:nowrap;';
    return a;
  }

  function bytes(n) {
    n = Number(n) || 0;
    var u = ['B', 'KiB', 'MiB', 'GiB', 'TiB'];
    var i = 0;
    while (n >= 1024 && i < u.length - 1) { n /= 1024; i++; }
    return (i === 0 ? n : n.toFixed(1)) + ' ' + u[i];
  }

  function buildQuota() {
    var box = document.createElement('span');
    box.style.cssText = 'display:inline-flex;flex-direction:column;justify-content:center;min-width:8em;margin:0 .6em;font-size:.7em;';
    var label = document.createElement('span');
    var track = document.createElement('span');
    track.style.cssText = 'height:.35em;border-radius:.2em;background:rgba(255,255,255,.2);overflow:hidden;display:block;margin-top:.2em;';
    var fill = document.createElement('span');
    fill.style.cssText = 'display:block;height:100%;background:#00a4dc;width:0%;';
    track.appendChild(fill);
    box.appendChild(label);
    box.appendChild(track);

    if (window.ApiClient && window.ApiClient.ajax) {
      window.ApiClient.ajax({ type: 'GET', url: getUrl('JellyCrowd/Quota/Me'), dataType: 'json' })
        .then(function (q) {
          if (!q) { return; }
          if (q.Unlimited || q.QuotaBytes <= 0) {
            label.textContent = t('quota_storage') + ': ' + bytes(q.UsedBytes) + ' / ' + t('quota_unlimited');
            track.style.display = 'none';
          } else {
            label.textContent = bytes(q.UsedBytes) + ' / ' + bytes(q.QuotaBytes);
            var p = q.QuotaBytes > 0 ? Math.min(100, q.UsedBytes / q.QuotaBytes * 100) : 0;
            fill.style.width = p + '%';
          }
        })
        .catch(function () { /* ignore */ });
    }

    return box;
  }

  function insert(host) {
    if (!host || document.querySelector('.jcHeaderBtns')) {
      return;
    }
    var wrap = document.createElement('span');
    wrap.className = 'jcHeaderBtns';
    wrap.style.cssText = 'display:inline-flex;align-items:center;';
    wrap.appendChild(navButton(t('nav_catalog'), 'catalog.html'));
    wrap.appendChild(navButton(t('nav_requests'), 'requests.html'));
    wrap.appendChild(buildQuota());
    host.insertBefore(wrap, host.firstChild);
  }

  function tryInsert() {
    insert(document.querySelector('.headerRight'));
  }

  function start() {
    var observer = new MutationObserver(function () { tryInsert(); });
    observer.observe(document.body, { childList: true, subtree: true });
    tryInsert();
  }

  loadStrings().then(start);
})();
