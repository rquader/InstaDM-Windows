'use strict';

/*
 * Minimal fake window for exercising containment-guard.js in Node without a
 * browser, network, or Instagram account. Models only what the guard
 * touches: capture/bubble click dispatch with stopImmediatePropagation
 * semantics, a History API that commits to location, anchor-like nodes with
 * parent chains, and the chrome.webview message bridge.
 */

function createFakeWindow(initialHref) {
  var captureListeners = { click: [], auxclick: [] };
  var bubbleListeners = { click: [], auxclick: [] };
  var postedMessages = [];
  var historyCalls = [];

  var win = {
    location: { href: initialHref },
    chrome: {
      webview: {
        postMessage: function (msg) { postedMessages.push(msg); }
      }
    },
    history: {
      pushState: function (state, title, url) {
        historyCalls.push({ method: 'pushState', url: url });
        if (url !== undefined && url !== null && url !== '') {
          win.location.href = new URL(String(url), win.location.href).href;
        }
      },
      replaceState: function (state, title, url) {
        historyCalls.push({ method: 'replaceState', url: url });
        if (url !== undefined && url !== null && url !== '') {
          win.location.href = new URL(String(url), win.location.href).href;
        }
      }
    },
    addEventListener: function (type, fn, capture) {
      var bucket = capture ? captureListeners : bubbleListeners;
      if (!bucket[type]) { bucket[type] = []; }
      bucket[type].push(fn);
    },

    /* ------------- harness-only helpers ------------- */

    postedMessages: postedMessages,
    historyCalls: historyCalls,

    // Dispatches like a real target-phase-less simplified DOM: window
    // capture listeners first (the guard), then window bubble listeners
    // (standing in for Instagram's SPA router). Returns the event.
    dispatch: function (type, target) {
      var stoppedImmediate = false;
      var stopped = false;
      var event = {
        type: type,
        target: target,
        defaultPrevented: false,
        preventDefault: function () { event.defaultPrevented = true; },
        stopPropagation: function () { stopped = true; },
        stopImmediatePropagation: function () { stoppedImmediate = true; stopped = true; }
      };

      var phase = [captureListeners[type] || [], bubbleListeners[type] || []];
      for (var p = 0; p < phase.length; p++) {
        if (stopped) { break; }
        for (var i = 0; i < phase[p].length; i++) {
          phase[p][i](event);
          if (stoppedImmediate) { return event; }
        }
      }
      return event;
    }
  };

  return win;
}

// Builds an <a href> node, optionally wrapped so the click target is a
// nested child (Instagram wraps thumbnails/spans inside links).
function makeLink(href, nestedDepth) {
  var anchor = {
    tagName: 'A',
    parentNode: null,
    getAttribute: function (name) { return name === 'href' ? href : null; }
  };
  var node = anchor;
  for (var i = 0; i < (nestedDepth || 0); i++) {
    node = { tagName: 'SPAN', parentNode: node, getAttribute: function () { return null; } };
  }
  // Note: fake children point UP via parentNode, which is all findLinkHref walks.
  return { anchor: anchor, target: node };
}

module.exports = { createFakeWindow: createFakeWindow, makeLink: makeLink };
