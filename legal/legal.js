/* =========================================================
   Renders a legal page from its JSON (privacy.json / terms.json).
   The <main class="legal" data-doc="privacy"> element declares which
   document to load, so the copy is edited/versioned in one JSON file.
   Each section heading is a deep-linkable anchor with a copy-link button.
   ========================================================= */
(() => {
  "use strict";

  const main = document.querySelector("main.legal");
  if (!main) return;
  const doc = main.dataset.doc;
  if (!doc) return;

  // Resolve the JSON + the home link relative to THIS script's folder, so they work whether
  // the pages sit at the site root or inside the legal/ folder (the fetch is otherwise resolved
  // against the page URL, which breaks once the JSON lives in a subfolder).
  const SRC = (document.currentScript && document.currentScript.src) || location.href;
  const HOME = new URL("../index.html", SRC).href;

  // example: "5. Augmented-reality safety" -> "augmented-reality-safety" (leading count dropped)
  const slugify = (s) =>
    s.replace(/^\s*\d+[.)]?\s+/, "")
      .toLowerCase()
      .replace(/['’]/g, "")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");

  const fmtDate = (iso) => {
    const d = new Date(iso);
    return isNaN(d) ? iso : d.toLocaleDateString("en-US", { year: "numeric", month: "long", day: "numeric" });
  };

  const escAttr = (s) => s.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");

  const LINK_ICON = '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 13a5 5 0 0 0 7.07 0l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.07 0l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>';

  fetch(new URL(doc + ".json", SRC).href, { cache: "no-cache" })
    .then((r) => {
      if (!r.ok) throw new Error(r.status);
      return r.json();
    })
    .then((data) => render(data))
    .catch(() => {
      main.innerHTML =
        '<div class="container legal-wrap"><a class="back" href="' + HOME + '">← Back to home</a>' +
        "<p>Sorry — this document could not be loaded. Please try again later.</p></div>";
    });

  function render(data) {
    const meta = [];
    if (data.version) meta.push("Version " + data.version);
    if (data.effectiveDate) meta.push("Effective " + fmtDate(data.effectiveDate));

    let html = '<div class="container legal-wrap">';
    html += '<a class="back" href="' + HOME + '">← Back to home</a>';
    html += "<h1>" + data.title + "</h1>";
    if (meta.length) html += '<p class="updated">' + meta.join("&nbsp;·&nbsp;") + "</p>";
    if (data.intro) html += '<div class="legal-intro">' + data.intro + "</div>";

    (data.sections || []).forEach((sec) => {
      const id = slugify(sec.heading);
      const body = (sec.content || []).join("\n");
      html +=
        '<section class="legal-section" id="' + id + '">' +
        '<h2 class="legal-heading">' +
        '<a class="legal-anchor" href="#' + id + '">' + sec.heading + "</a>" +
        '<button type="button" class="copy-link" data-hash="' + id +
        '" aria-label="Copy link to “' + escAttr(sec.heading) + '”" title="Copy link">' +
        LINK_ICON + "</button>" +
        "</h2>" +
        '<div class="legal-body">' + body + "</div>" +
        "</section>";
    });
    if (data.outro) html += '<div class="legal-outro">' + data.outro + "</div>";
    html += "</div>";
    main.innerHTML = html;

    // update the document title with the loaded doc title
    if (data.title) document.title = data.title + " — ChillZone";

    // navigator.clipboard needs a secure context (https/localhost); over a plain-HTTP LAN
    // IP it's undefined, so fall back to a hidden-textarea execCommand copy.
    const execCopy = (text) => {
      const ta = document.createElement("textarea");
      ta.value = text;
      ta.setAttribute("readonly", "");
      ta.style.cssText = "position:fixed;top:-9999px;opacity:0;";
      document.body.appendChild(ta);
      ta.select();
      try { document.execCommand("copy"); } catch (_) {}
      document.body.removeChild(ta);
    };

    // highlight (and optionally scroll to) the section named by the current URL hash
    const focusFromHash = (scroll) => {
      main.querySelectorAll(".legal-section.is-focused").forEach((s) => s.classList.remove("is-focused"));
      const id = decodeURIComponent(location.hash.slice(1));
      if (!id) return;
      const el = document.getElementById(id);
      if (!el) return;
      el.classList.add("is-focused");
      if (scroll) requestAnimationFrame(() => el.scrollIntoView());
    };

    main.addEventListener("click", (e) => {
      // copy-link button → copy the section URL silently + flash a green check
      const btn = e.target.closest(".copy-link");
      if (btn) {
        const url = location.origin + location.pathname + "#" + btn.dataset.hash;
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(url).catch(() => execCopy(url));
        } else {
          execCopy(url);
        }
        btn.classList.add("copied");
        clearTimeout(btn._t);
        btn._t = setTimeout(() => btn.classList.remove("copied"), 1200);
        return;
      }
      // heading → toggle the reference: clicking the already-referenced section clears it
      const a = e.target.closest(".legal-anchor");
      if (a) {
        e.preventDefault();
        const id = a.getAttribute("href").slice(1);
        if (decodeURIComponent(location.hash.slice(1)) === id) {
          history.pushState("", document.title, location.pathname + location.search);
          focusFromHash(false);
        } else {
          location.hash = id;
        }
      }
    });

    // focus on first load AND on hash change, so opening a reference URL focuses it too
    window.addEventListener("hashchange", () => focusFromHash(false));
    if (location.hash.length > 1) focusFromHash(true);
  }
})();
