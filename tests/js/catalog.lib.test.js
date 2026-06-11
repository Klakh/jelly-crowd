'use strict';

const test = require('node:test');
const assert = require('node:assert');
const path = require('node:path');

const lib = require(path.join(
  __dirname,
  '..',
  '..',
  'Jellyfin.Plugin.JellyCrowd',
  'Web',
  'catalog.lib.js'));

const SUPPORTED = ['en', 'fr'];

test('pickLang returns the matching 2-letter code', () => {
  assert.strictEqual(lib.pickLang('fr-FR', SUPPORTED), 'fr');
  assert.strictEqual(lib.pickLang('en-US', SUPPORTED), 'en');
});

test('pickLang falls back to en for unsupported or empty locales', () => {
  assert.strictEqual(lib.pickLang('de-DE', SUPPORTED), 'en');
  assert.strictEqual(lib.pickLang('', SUPPORTED), 'en');
  assert.strictEqual(lib.pickLang(null, SUPPORTED), 'en');
});

test('yearOf extracts the year or returns empty', () => {
  assert.strictEqual(lib.yearOf({ ReleaseDate: '2021-02-02' }), '2021');
  assert.strictEqual(lib.yearOf({}), '');
  assert.strictEqual(lib.yearOf(null), '');
});

test('formatTitle appends the year when known', () => {
  assert.strictEqual(lib.formatTitle({ Title: 'Dune', ReleaseDate: '2021-10-22' }), 'Dune (2021)');
  assert.strictEqual(lib.formatTitle({ Title: 'No Date' }), 'No Date');
});

test('errorKey maps 503 to the not-configured message', () => {
  assert.strictEqual(lib.errorKey(503), 'error_not_configured');
  assert.strictEqual(lib.errorKey(500), 'error_generic');
  assert.strictEqual(lib.errorKey(undefined), 'error_generic');
});

test('formatRating returns one decimal or empty', () => {
  assert.strictEqual(lib.formatRating(7.5), '7.5');
  assert.strictEqual(lib.formatRating(8), '8.0');
  assert.strictEqual(lib.formatRating(0), '');
  assert.strictEqual(lib.formatRating(undefined), '');
});

test('statusLabelKey handles numeric and string statuses', () => {
  assert.strictEqual(lib.statusLabelKey(0), 'status_pending');
  assert.strictEqual(lib.statusLabelKey(1), 'status_approved');
  assert.strictEqual(lib.statusLabelKey(2), 'status_denied');
  assert.strictEqual(lib.statusLabelKey(3), 'status_available');
  assert.strictEqual(lib.statusLabelKey('Approved'), 'status_approved');
  assert.strictEqual(lib.statusLabelKey('weird'), 'status_pending');
});

test('formatBytes renders binary units', () => {
  assert.strictEqual(lib.formatBytes(0), '0 B');
  assert.strictEqual(lib.formatBytes(1024), '1.0 KiB');
  assert.strictEqual(lib.formatBytes(5 * 1024 * 1024 * 1024), '5.0 GiB');
});

test('quotaPercent clamps and treats <=0 quota as unlimited', () => {
  assert.strictEqual(lib.quotaPercent(5, 10), 50);
  assert.strictEqual(lib.quotaPercent(15, 10), 100);
  assert.strictEqual(lib.quotaPercent(1, 0), 0);
});
