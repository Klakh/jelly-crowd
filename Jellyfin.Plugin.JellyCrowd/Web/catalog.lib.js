/*
 * Jelly Crowd — pure, framework-free helpers shared by catalog.js.
 * UMD wrapper so the same file works as a browser global (JellyCrowdLib)
 * and as a CommonJS module under `node --test` (see tests/js/).
 */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.JellyCrowdLib = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';

  // Pick the 2-letter catalog language from a full locale, falling back to 'en'.
  function pickLang(locale, supported) {
    var code = String(locale || 'en').slice(0, 2).toLowerCase();
    return supported.indexOf(code) >= 0 ? code : 'en';
  }

  // Extract the 4-digit year from a TMDB date string, or '' when absent.
  function yearOf(item) {
    return item && item.ReleaseDate ? String(item.ReleaseDate).slice(0, 4) : '';
  }

  // "Title (Year)" when a year is known, otherwise just the title.
  function formatTitle(item) {
    var title = (item && item.Title) || '';
    var year = yearOf(item);
    return year ? title + ' (' + year + ')' : title;
  }

  // Map an HTTP error status to the i18n key used for the message.
  function errorKey(status) {
    return status === 503 ? 'error_not_configured' : 'error_generic';
  }

  // Map a request status (numeric enum or string, as serialized by the API) to its i18n key.
  function statusLabelKey(status) {
    var map = {
      '0': 'status_pending', 'pending': 'status_pending',
      '1': 'status_approved', 'approved': 'status_approved',
      '2': 'status_denied', 'denied': 'status_denied',
      '3': 'status_available', 'available': 'status_available'
    };
    return map[String(status).toLowerCase()] || 'status_pending';
  }

  return {
    pickLang: pickLang,
    yearOf: yearOf,
    formatTitle: formatTitle,
    errorKey: errorKey,
    statusLabelKey: statusLabelKey
  };
});
