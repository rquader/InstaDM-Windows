'use strict';

/*
 * Containment-guard tests. Fully local: no network, no browser, no
 * Instagram account. Run with `node --test tests/InstaDM.WebHarness.Tests/`.
 *
 * The guard template is loaded from src/InstaDM.App/Web/containment-guard.js
 * and the policy is spliced exactly the way the app does it at runtime
 * (PolicyScriptBuilder.InjectIntoScript), using the checked-in fixture that
 * PolicyFixtureDriftTests pins to the C# builder's output.
 */

const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const Module = require('node:module');

const { createFakeWindow, makeLink } = require('../Fixtures/local-spa-harness/fake-dom.js');

const repoRoot = path.join(__dirname, '..', '..');
const guardTemplate = fs.readFileSync(
  path.join(repoRoot, 'src', 'InstaDM.App', 'Web', 'containment-guard.js'), 'utf8');
const policyJson = fs.readFileSync(
  path.join(repoRoot, 'tests', 'Fixtures', 'local-spa-harness', 'policy.default.json'), 'utf8').trim();

const PLACEHOLDER = '__INSTADM_POLICY__';
assert.ok(guardTemplate.includes(PLACEHOLDER), 'guard template must contain the policy placeholder');

function loadGuardModule(json) {
  const source = guardTemplate.replace(PLACEHOLDER, json);
  const m = new Module('containment-guard', null);
  m._compile(source, path.join(repoRoot, 'src', 'InstaDM.App', 'Web', 'containment-guard.js'));
  return m.exports;
}

const guardModule = loadGuardModule(policyJson);
const policy = JSON.parse(policyJson);

const INBOX = 'https://www.instagram.com/direct/inbox/';
const THREAD = 'https://www.instagram.com/direct/t/123456/';
const LOGIN = 'https://www.instagram.com/accounts/login/';

function installedGuard(href) {
  const win = createFakeWindow(href);
  const guard = guardModule.createGuard(win, JSON.parse(policyJson));
  assert.ok(guard, 'guard must activate with a valid policy');
  guard.install();
  return { win, guard };
}

/* ------------------------------------------------------------------ */
/* pure judgment                                                       */
/* ------------------------------------------------------------------ */

test('embedded policy fixture matches the C# wire shape', () => {
  assert.equal(policy.version, 1);
  assert.deepEqual(policy.dmPrefixes, ['/direct/inbox', '/direct/t', '/direct/new']);
  assert.ok(policy.authPrefixes.includes('/auth_platform'));
});

test('canonicalize mirrors the C# canonicalizer on adversarial inputs', () => {
  const { guard } = installedGuard(INBOX);
  // rejects
  for (const raw of [
    'http://www.instagram.com/direct/inbox/',
    'javascript:alert(1)',
    'https://user@www.instagram.com/',
    'https://www.instagram.com@evil.com/direct/inbox/',
    'https://www.instagram.com:8443/x',
    'https://www.\u0456nstagram.com/',
    'https://xn--nstagram-e1a.com/',
    'https://www.instagram.com/direct%2Finbox/',
  ]) {
    assert.equal(guard.canonicalize(raw), null, raw);
  }
  // accepts + normalizes
  assert.deepEqual(guard.canonicalize('https://WWW.INSTAGRAM.COM./direct/inbox/?x=1#f'),
    { host: 'www.instagram.com', path: '/direct/inbox/' });
  // dot segments resolve before judgment, like .NET Uri
  assert.deepEqual(guard.canonicalize('https://www.instagram.com/direct/../'),
    { host: 'www.instagram.com', path: '/' });
});

test('directory-boundary matching mirrors PathMatcher', () => {
  const { guard } = installedGuard(INBOX);
  assert.equal(guard.matchesPrefix('/p/abc', '/p'), true);
  assert.equal(guard.matchesPrefix('/profile', '/p'), false);
  assert.equal(guard.matchesPrefix('/privacy', '/p'), false);
  assert.equal(guard.matchesPrefix('/directory', '/direct'), false);
  assert.equal(guard.matchesPrefix('/direct/inboxx', '/direct/inbox'), false);
});

test('isAllowed: DM and auth yes; feed/profile/post/bare-direct no', () => {
  const { guard } = installedGuard(INBOX);
  const allowed = [INBOX, THREAD, 'https://www.instagram.com/direct/new/', LOGIN,
    'https://www.instagram.com/challenge/1/', 'https://www.instagram.com/auth_platform/x/',
    'https://accounts.instagram.com/whatever/'];
  const blocked = ['https://www.instagram.com/', 'https://www.instagram.com/direct/',
    'https://www.instagram.com/someuser/', 'https://www.instagram.com/p/C1/',
    'https://www.instagram.com/explore/', 'https://evil.com/direct/inbox/'];
  for (const u of allowed) { assert.equal(guard.isAllowed(guard.canonicalize(u)), true, u); }
  for (const u of blocked) { assert.equal(guard.isAllowed(guard.canonicalize(u)), false, u); }
});

/* ------------------------------------------------------------------ */
/* click interception                                                  */
/* ------------------------------------------------------------------ */

test('blocked click: guard prevents default and stops SPA handler', () => {
  const { win } = installedGuard(THREAD);
  let spaSawClick = false;
  win.addEventListener('click', () => { spaSawClick = true; }, false); // Instagram's router

  const { target } = makeLink('/someuser/', 2); // nested span inside profile link
  const event = win.dispatch('click', target);

  assert.equal(event.defaultPrevented, true);
  assert.equal(spaSawClick, false, 'SPA handler must never see the blocked click');
  assert.deepEqual(win.postedMessages, [
    { v: 1, source: 'instadm-guard', kind: 'blockedClick', surface: 'other' },
  ]);
});

test('blocked click reports coarse surface categories, never URLs', () => {
  const cases = [
    ['/', 'feed'],
    ['/explore/', 'explore'],
    ['/reel/C1/', 'reels'],
    ['/stories/u/1/', 'stories'],
    ['/p/C1/', 'post'],
    ['/direct/', 'directShell'],
    ['https://evil.com/x', 'offPlatform'],
  ];
  for (const [href, surface] of cases) {
    const { win } = installedGuard(THREAD);
    win.dispatch('click', makeLink(href, 0).target);
    assert.equal(win.postedMessages.length, 1, href);
    assert.equal(win.postedMessages[0].surface, surface, href);
    const serialized = JSON.stringify(win.postedMessages[0]);
    assert.ok(!serialized.includes('evil.com') && !serialized.includes('/p/C1'),
      'report must not contain URLs');
  }
});

test('allowed click passes through untouched', () => {
  const { win } = installedGuard(INBOX);
  let spaSawClick = false;
  win.addEventListener('click', () => { spaSawClick = true; }, false);

  const event = win.dispatch('click', makeLink('/direct/t/42/', 1).target);

  assert.equal(event.defaultPrevented, false);
  assert.equal(spaSawClick, true);
  assert.deepEqual(win.postedMessages, []);
});

test('relative hrefs resolve against the current page', () => {
  const { win } = installedGuard('https://www.instagram.com/direct/t/42/');
  // ../../ from /direct/t/42/ lands on /direct/<x>/ (the blocked shell);
  // ../../../ escapes to a profile at the root. Both must be judged on the
  // RESOLVED path, not the raw href.
  const shell = win.dispatch('click', makeLink('../../someuser/', 0).target);
  assert.equal(shell.defaultPrevented, true);
  assert.equal(win.postedMessages[0].surface, 'directShell');

  const profile = win.dispatch('click', makeLink('../../../someuser/', 0).target);
  assert.equal(profile.defaultPrevented, true);
  assert.equal(win.postedMessages[1].surface, 'other');
});

test('non-link clicks and fragment links are ignored', () => {
  const { win } = installedGuard(INBOX);
  const plain = { tagName: 'DIV', parentNode: null, getAttribute: () => null };
  assert.equal(win.dispatch('click', plain).defaultPrevented, false);
  assert.equal(win.dispatch('click', makeLink('#section', 0).target).defaultPrevented, false);
  assert.deepEqual(win.postedMessages, []);
});

test('auxclick (middle-click) is intercepted like click', () => {
  const { win } = installedGuard(INBOX);
  const event = win.dispatch('auxclick', makeLink('https://www.instagram.com/explore/', 0).target);
  assert.equal(event.defaultPrevented, true);
  assert.equal(win.postedMessages[0].surface, 'explore');
});

test('javascript: href in a link is blocked (fail closed)', () => {
  const { win } = installedGuard(INBOX);
  const event = win.dispatch('click', makeLink('javascript:void(0)', 0).target);
  assert.equal(event.defaultPrevented, true);
  assert.equal(win.postedMessages[0].surface, 'malformed');
});

test('dynamically added links are covered (listener is on window)', () => {
  const { win } = installedGuard(INBOX);
  // "Added after load" simply means dispatched later — the capture listener
  // on window sees every bubbling click regardless of when the DOM grew.
  const late = makeLink('/reels/', 3);
  const event = win.dispatch('click', late.target);
  assert.equal(event.defaultPrevented, true);
  assert.equal(win.postedMessages[0].surface, 'reels');
});

/* ------------------------------------------------------------------ */
/* History API interception                                            */
/* ------------------------------------------------------------------ */

test('pushState to a blocked surface is swallowed', () => {
  const { win } = installedGuard(THREAD);
  win.history.pushState({}, '', '/someuser/');
  assert.deepEqual(win.historyCalls, [], 'original pushState must not run');
  assert.equal(win.location.href, THREAD, 'location must not move');
  assert.deepEqual(win.postedMessages, [
    { v: 1, source: 'instadm-guard', kind: 'blockedHistory', surface: 'other' },
  ]);
});

test('pushState to a DM surface proceeds', () => {
  const { win } = installedGuard(INBOX);
  win.history.pushState({}, '', '/direct/t/77/');
  assert.equal(win.historyCalls.length, 1);
  assert.equal(win.location.href, 'https://www.instagram.com/direct/t/77/');
  assert.deepEqual(win.postedMessages, []);
});

test('replaceState is wrapped with the same rules', () => {
  const { win } = installedGuard(INBOX);
  win.history.replaceState({}, '', '/explore/');
  assert.deepEqual(win.historyCalls, []);
  assert.equal(win.postedMessages[0].kind, 'blockedHistory');
  win.history.replaceState({}, '', '/direct/inbox/');
  assert.equal(win.historyCalls.length, 1);
});

test('same-document state updates (no url) always pass', () => {
  const { win } = installedGuard(THREAD);
  win.history.pushState({ scroll: 123 }, '');
  win.history.replaceState({ scroll: 456 }, '', '');
  assert.equal(win.historyCalls.length, 2);
  assert.deepEqual(win.postedMessages, []);
});

test('bare /direct pushState is swallowed (minimized-messenger shell)', () => {
  const { win } = installedGuard(THREAD);
  win.history.pushState({}, '', '/direct/');
  assert.deepEqual(win.historyCalls, []);
  assert.equal(win.postedMessages[0].surface, 'directShell');
});

/* ------------------------------------------------------------------ */
/* auth stand-down                                                     */
/* ------------------------------------------------------------------ */

test('on a login page the guard stands down completely', () => {
  const { win } = installedGuard(LOGIN);
  // Auth flows hop through arbitrary routes — even a normally-blocked
  // destination passes while the CURRENT page is an auth surface.
  const event = win.dispatch('click', makeLink('/', 0).target);
  assert.equal(event.defaultPrevented, false);
  win.history.pushState({}, '', '/');
  assert.equal(win.historyCalls.length, 1);
  assert.deepEqual(win.postedMessages, []);
});

test('stand-down covers challenge, auth_platform, and the auth host', () => {
  for (const href of [
    'https://www.instagram.com/challenge/123/',
    'https://www.instagram.com/auth_platform/codeentry/',
    'https://accounts.instagram.com/login/',
  ]) {
    const { win } = installedGuard(href);
    const event = win.dispatch('click', makeLink('/anything/at/all/', 0).target);
    assert.equal(event.defaultPrevented, false, href);
  }
});

test('stand-down ends when the page is a DM surface again', () => {
  const { win } = installedGuard(LOGIN);
  win.location.href = INBOX; // login completed, watcher routed to inbox
  const event = win.dispatch('click', makeLink('/', 0).target);
  assert.equal(event.defaultPrevented, true);
});

test('follow-requests path is NOT an auth stand-down surface', () => {
  const { win } = installedGuard('https://www.instagram.com/accounts/activity/');
  const event = win.dispatch('click', makeLink('/', 0).target);
  assert.equal(event.defaultPrevented, true,
    '/accounts/activity must not grant stand-down');
});

/* ------------------------------------------------------------------ */
/* optional prefixes and payload validation                            */
/* ------------------------------------------------------------------ */

test('optionalPrefixes grant exactly the listed surfaces', () => {
  const withFollowRequests = JSON.parse(policyJson);
  withFollowRequests.optionalPrefixes = ['/accounts/activity'];
  const win = createFakeWindow(INBOX);
  const guard = guardModule.createGuard(win, withFollowRequests);
  guard.install();
  assert.equal(guard.isAllowed(guard.canonicalize('https://www.instagram.com/accounts/activity/')), true);
  assert.equal(guard.isAllowed(guard.canonicalize('https://www.instagram.com/accounts/edit/')), false);
});

test('shared posts stay blocked in the guard even when natively enabled', () => {
  // The C# builder never exports post/reel prefixes (source-gating is
  // native-only); prove the guard blocks them with the default payload.
  const { win } = installedGuard(THREAD);
  const event = win.dispatch('click', makeLink('/p/C1/', 1).target);
  assert.equal(event.defaultPrevented, true);
  assert.equal(win.postedMessages[0].surface, 'post');
});

test('malformed policy payload deactivates the guard instead of misjudging', () => {
  for (const bad of [null, {}, { version: 2 }, { version: 1, allowedHosts: 'nope' }]) {
    const win = createFakeWindow(INBOX);
    assert.equal(guardModule.createGuard(win, bad), null);
    assert.equal(win.postedMessages[0].kind, 'guardInactive');
  }
});

test('guard reports carry only fixed schema keys', () => {
  const { win } = installedGuard(THREAD);
  win.dispatch('click', makeLink('/explore/', 0).target);
  win.history.pushState({}, '', '/someuser/');
  for (const msg of win.postedMessages) {
    assert.deepEqual(Object.keys(msg).sort(), ['kind', 'source', 'surface', 'v']);
    assert.equal(typeof msg.surface, 'string');
  }
});
