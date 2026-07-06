/* =========================================================
   Changelog page — fetches every GitHub release for the repo
   named in <main class="changelog" data-repo="owner/name"> and
   renders a card per release: notes (a small Markdown subset),
   any embedded screenshots, and download links (APK + source
   zip) plus a link to the release on GitHub.

   Unauthenticated GitHub API is limited to 60 requests/hour per
   IP, so results are cached in sessionStorage for a few minutes.
   ========================================================= */
(() => {
  "use strict";

  const main = document.querySelector("main.changelog");
  if (!main) return;
  const repo = main.dataset.repo;
  const list = main.querySelector("#cl-list");
  if (!repo || !list) return;

  const API = "https://api.github.com/repos/" + repo + "/releases?per_page=100";
  const RELEASES_PAGE = "https://github.com/" + repo + "/releases";
  const CACHE_KEY = "cz_releases_" + repo;
  const CACHE_TTL = 10 * 60 * 1000; // 10 minutes

  const DL_ICON = '<svg viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><path d="M7 10l5 5 5-5"/><path d="M12 15V3"/></svg>';
  const GH_ICON = '<svg viewBox="0 0 24 24" width="17" height="17" fill="currentColor" aria-hidden="true"><path d="M12 .5A11.5 11.5 0 0 0 .5 12a11.5 11.5 0 0 0 7.86 10.92c.58.1.79-.25.79-.56v-2c-3.2.7-3.88-1.37-3.88-1.37-.53-1.33-1.28-1.69-1.28-1.69-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.7 1.26 3.36.96.1-.75.4-1.26.73-1.55-2.56-.29-5.25-1.28-5.25-5.7 0-1.26.45-2.29 1.19-3.1-.12-.29-.52-1.46.11-3.05 0 0 .97-.31 3.18 1.18a11 11 0 0 1 5.8 0c2.2-1.49 3.17-1.18 3.17-1.18.63 1.59.23 2.76.11 3.05.74.81 1.19 1.84 1.19 3.1 0 4.43-2.7 5.4-5.27 5.69.41.36.78 1.06.78 2.14v3.17c0 .31.21.67.8.56A11.5 11.5 0 0 0 23.5 12 11.5 11.5 0 0 0 12 .5z"/></svg>';

  const esc = (s) =>
    String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");

  const fmtDate = (iso) => {
    const d = new Date(iso);
    return isNaN(d) ? "" : d.toLocaleDateString("en-US", { year: "numeric", month: "long", day: "numeric" });
  };

  const fmtSize = (bytes) => {
    if (!bytes && bytes !== 0) return "";
    const mb = bytes / (1024 * 1024);
    return mb >= 1 ? mb.toFixed(1) + " MB" : Math.max(1, Math.round(bytes / 1024)) + " KB";
  };

  // ---- tiny Markdown subset → HTML (release notes) -----------------------

  // inline formatting: escape first, then re-introduce a safe HTML subset
  const inline = (raw) => {
    let s = esc(raw);
    s = s.replace(/`([^`]+)`/g, (m, c) => "<code>" + c + "</code>");
    // markdown links [text](url) — url already escaped, so &quot; can't break out
    s = s.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g,
      (m, t, u) => '<a href="' + u + '" target="_blank" rel="noopener">' + t + "</a>");
    s = s.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
    // bare URLs that aren't already inside an attribute (…="url") or tag (>url)
    s = s.replace(/(^|[^"=>/])(https?:\/\/[^\s<)]+)/g,
      (m, pre, u) => pre + '<a href="' + u + '" target="_blank" rel="noopener">' + u + "</a>");
    return s;
  };

  // normalise a width attribute: "70%" stays, a bare "300" becomes "300px",
  // anything unrecognised is dropped so it can't break out of the style attr
  const normWidth = (w) => {
    if (!w) return "";
    w = String(w).trim();
    if (/^\d+$/.test(w)) return w + "px";
    if (/^[\d.]+(px|%)$/.test(w)) return w;
    return "";
  };

  // pull image descriptors (src + optional width + alt) out of a line,
  // for both markdown ![alt](url) and raw <img …> tags
  const collectImages = (line) => {
    const imgs = [];
    line.replace(/!\[([^\]]*)\]\(([^)\s]+)[^)]*\)/g,
      (m, alt, u) => (imgs.push({ src: u, alt: alt, width: "" }), m));
    line.replace(/<img\b[^>]*>/gi, (tag) => {
      const src = (tag.match(/\bsrc\s*=\s*["']([^"']+)["']/i) || [])[1];
      if (src) imgs.push({
        src: src,
        alt: (tag.match(/\balt\s*=\s*["']([^"']*)["']/i) || [])[1] || "",
        width: (tag.match(/\bwidth\s*=\s*["']?([^"'\s>]+)/i) || [])[1] || "",
      });
      return tag;
    });
    return imgs;
  };

  // true when a line is only image markup (so we can lift it into a media strip)
  const isImageOnly = (line) => {
    const rest = line
      .replace(/<\/?p[^>]*>/gi, "")
      .replace(/<img[^>]*>/gi, "")
      .replace(/!\[[^\]]*\]\([^)]*\)/g, "")
      .trim();
    return rest === "" && collectImages(line).length > 0;
  };

  // when every image in the group carries an explicit width, keep the given
  // proportions on one row that scales with the container (GitHub-style);
  // otherwise fall back to a wrapping, height-capped strip
  const mediaFigure = (imgs) => {
    const sized = imgs.length > 0 && imgs.every((im) => normWidth(im.width));
    const tags = imgs.map((im) => {
      const w = normWidth(im.width);
      const style = sized && w ? ' style="width:' + w + '"' : "";
      return '<img loading="lazy" src="' + esc(im.src) + '" alt="' + esc(im.alt) + '"' + style + " />";
    }).join("");
    return '<div class="cl-media' + (sized ? " cl-media-sized" : "") + '">' + tags + "</div>";
  };

  // blockquote inner lines: a leading ###/**…** becomes bold, rest is inline
  const renderQuote = (lines) =>
    lines
      .map((l) => {
        const h = l.match(/^#{1,6}\s+(.*)$/);
        return h ? "<strong>" + inline(h[1]) + "</strong>" : inline(l);
      })
      .filter((x) => x !== "")
      .join("<br>");

  const renderNotes = (md) => {
    if (!md || !md.trim()) return '<p class="cl-empty">No release notes.</p>';
    const lines = md.replace(/\r\n?/g, "\n").split("\n");
    const out = [];
    let i = 0;
    while (i < lines.length) {
      const line = lines[i];

      if (!line.trim()) { i++; continue; }

      if (isImageOnly(line)) { out.push(mediaFigure(collectImages(line))); i++; continue; }

      const h = line.match(/^(#{1,6})\s+(.*)$/);
      if (h) {
        const lvl = Math.min(h[1].length + 1, 5); // ## → h3, ### → h4 …
        out.push("<h" + lvl + ' class="cl-h">' + inline(h[2]) + "</h" + lvl + ">");
        i++; continue;
      }

      if (/^\s*>/.test(line)) {
        const buf = [];
        while (i < lines.length && /^\s*>/.test(lines[i])) {
          buf.push(lines[i].replace(/^\s*>\s?/, ""));
          i++;
        }
        out.push("<blockquote>" + renderQuote(buf) + "</blockquote>");
        continue;
      }

      if (/^\s*[-*]\s+/.test(line)) {
        const items = [];
        while (i < lines.length && (/^\s*[-*]\s+/.test(lines[i]) || (items.length && /^\s{2,}\S/.test(lines[i])))) {
          if (/^\s*[-*]\s+/.test(lines[i])) items.push(lines[i].replace(/^\s*[-*]\s+/, ""));
          else items[items.length - 1] += " " + lines[i].trim(); // soft-wrapped continuation
          i++;
        }
        out.push("<ul>" + items.map((t) => "<li>" + inline(t) + "</li>").join("") + "</ul>");
        continue;
      }

      const para = [line];
      i++;
      while (i < lines.length && lines[i].trim() &&
             !/^\s*([-*]\s+|>|#{1,6}\s)/.test(lines[i]) && !isImageOnly(lines[i])) {
        para.push(lines[i]); i++;
      }
      out.push("<p>" + inline(para.join(" ")) + "</p>");
    }
    return out.join("\n");
  };

  // ---- release card ------------------------------------------------------

  const releaseCard = (rel, isLatest) => {
    const title = rel.name || rel.tag_name;
    const tag = rel.tag_name;
    const date = fmtDate(rel.published_at || rel.created_at);

    const tags = ['<button type="button" class="cl-tag-ver" data-hash="' + esc(tag) +
      '" title="Copy link to this release">' + esc(tag) + "</button>"];
    if (isLatest) tags.push('<span class="cl-latest">Latest</span>');
    if (rel.prerelease) tags.push('<span class="cl-pre">Pre-release</span>');

    let head =
      '<header class="cl-head"><div><h2 class="cl-title">' + esc(title) + "</h2>" +
      '<div class="cl-tags">' + tags.join("") + "</div></div>";
    if (date) head += '<time class="cl-date" datetime="' + esc(rel.published_at || "") + '">' + esc(date) + "</time>";
    head += "</header>";

    const notes = '<div class="cl-notes">' + renderNotes(rel.body || "") + "</div>";

    // downloads: APK asset(s) + source zip + link to the release on GitHub
    const btns = [];
    (rel.assets || [])
      .filter((a) => /\.apk$/i.test(a.name))
      .forEach((a) => {
        const size = fmtSize(a.size);
        btns.push(
          '<a class="btn btn-primary" href="' + esc(a.browser_download_url) + '" download>' +
          DL_ICON + "<span>APK" + (size ? ' <span class="cl-size">' + size + "</span>" : "") + "</span></a>"
        );
      });
    const zip = "https://github.com/" + repo + "/archive/refs/tags/" + encodeURIComponent(tag) + ".zip";
    btns.push('<a class="btn btn-ghost" href="' + esc(zip) + '">' + DL_ICON + "<span>Source .zip</span></a>");
    btns.push('<a class="cl-ghlink" href="' + esc(rel.html_url) + '" target="_blank" rel="noopener">' +
      GH_ICON + "<span>View on GitHub</span></a>");

    const actions = '<div class="cl-actions">' + btns.join("") + "</div>";

    // id = tag name, so releases are directly linkable (changelog.html#v2.1.0)
    return '<article class="cl-release reveal" id="' + esc(tag) + '">' + head + notes + actions + "</article>";
  };

  const renderAll = (releases) => {
    if (!Array.isArray(releases) || !releases.length) {
      list.innerHTML = '<p class="cl-error">No releases published yet — check <a href="' +
        RELEASES_PAGE + '" target="_blank" rel="noopener">GitHub</a>.</p>';
      return;
    }
    // "Latest" = newest non-prerelease (GitHub returns newest first)
    const latestTag = (releases.find((r) => !r.prerelease && !r.draft) || releases[0]).tag_name;
    list.innerHTML = releases
      .filter((r) => !r.draft)
      .map((r) => releaseCard(r, r.tag_name === latestTag))
      .join("");
    revealCards();
    scrollToHash();
  };

  // cards render after the fetch resolves, so the browser's initial jump to
  // #tag finds nothing — re-run it here (and on later hash changes) to slide there
  const scrollToHash = () => {
    const id = decodeURIComponent(location.hash.slice(1));
    if (!id) return;
    const el = document.getElementById(id);
    if (el) requestAnimationFrame(() => el.scrollIntoView());
  };
  window.addEventListener("hashchange", scrollToHash);

  // navigator.clipboard needs a secure context (https/localhost); over a plain
  // LAN IP it's undefined, so fall back to a hidden-textarea execCommand copy.
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
  const copyText = (text) => {
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).catch(() => execCopy(text));
    else execCopy(text);
  };
  // click the version pill → copy that release's deep link, flash "Copied"
  // (delegated on #cl-list so it survives re-renders; attached before any render)
  list.addEventListener("click", (e) => {
    const pill = e.target.closest(".cl-tag-ver");
    if (!pill) return;
    copyText(location.origin + location.pathname + "#" + pill.dataset.hash);
    pill.classList.add("copied");
    clearTimeout(pill._t);
    pill._t = setTimeout(() => pill.classList.remove("copied"), 1200);
  });

  const showError = () => {
    list.innerHTML =
      '<p class="cl-error">Couldn\'t load releases right now (GitHub may be rate-limiting). ' +
      'See them directly on <a href="' + RELEASES_PAGE + '" target="_blank" rel="noopener">GitHub</a>.</p>';
  };

  // fade cards in as they scroll into view (this page has no global observer)
  const revealCards = () => {
    const cards = list.querySelectorAll(".reveal");
    if (!("IntersectionObserver" in window) ||
        matchMedia("(prefers-reduced-motion: reduce)").matches) {
      cards.forEach((c) => c.classList.add("in"));
      return;
    }
    const io = new IntersectionObserver((entries) => {
      entries.forEach((en) => {
        if (en.isIntersecting) { en.target.classList.add("in"); io.unobserve(en.target); }
      });
    }, { rootMargin: "0px 0px -8% 0px" });
    cards.forEach((c) => io.observe(c));
  };

  // ---- cache + fetch -----------------------------------------------------

  const readCache = () => {
    try {
      const raw = sessionStorage.getItem(CACHE_KEY);
      if (!raw) return null;
      const obj = JSON.parse(raw);
      return obj && Array.isArray(obj.data) ? obj : null;
    } catch (_) { return null; }
  };
  const writeCache = (data) => {
    try { sessionStorage.setItem(CACHE_KEY, JSON.stringify({ t: Date.now(), data })); } catch (_) {}
  };

  const cached = readCache();
  if (cached && Date.now() - cached.t < CACHE_TTL) {
    renderAll(cached.data);
    return; // fresh enough — skip the network
  }

  fetch(API, { headers: { Accept: "application/vnd.github+json" }, cache: "no-cache" })
    .then((r) => {
      if (!r.ok) throw new Error(r.status);
      return r.json();
    })
    .then((data) => {
      writeCache(data);
      renderAll(data);
    })
    .catch(() => {
      if (cached) renderAll(cached.data); // stale is better than nothing
      else showError();
    });
})();
