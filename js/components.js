/* =========================================================
   Reusable site components (no build step, no fetch — works
   over file:// and https alike). Defines custom elements:
     <site-header>       — shared top nav (used on every page)
     <site-footer>       — full footer (landing page)
     <site-footer-slim>  — slim footer (legal pages)
   ========================================================= */
(() => {
  "use strict";

  const YEAR = new Date().getFullYear() === 2026 ? new Date().getFullYear() : "2026-" + new Date().getFullYear() % 100;
  const PLAY = "https://play.google.com/store/apps/details?id=com.DanyT.ChillZone";
  const REPO = "https://github.com/DanyilT/ChillZone";
  const GITHUB = "https://github.com/DanyilT";
  const COFFEE = "https://buymeacoffee.com/danyt";
  const UA = "https://savelife.in.ua/en";
  const V1_APP = "https://github.com/DanyilT/ChillZone/releases/tag/v1.2.1"; // latest v1 release (APK)
  const V1_PAGE = "https://danyilt-chillzone.pages.dev"; // v1 landing page

  // Link to an in-page section if it exists here, otherwise jump to it on the home page.
  const to = (id) => (document.getElementById(id) ? `#${id}` : `index.html#${id}`);

  class SiteHeader extends HTMLElement {
    connectedCallback() {
      this.innerHTML = `
        <header class="nav">
          <div class="container nav-inner">
            <a class="brand" href="${to("top")}"><img src="favicon.svg" alt=""> ChillZone</a>
            <nav class="nav-links">
              <a href="${to("features")}">Features</a>
              <a href="${to("video")}">Video</a>
              <a href="${to("how")}">How to play</a>
              <a href="${to("shots")}">Screenshots</a>
              <a class="btn btn-primary nav-cta" href="${PLAY}" target="_blank" rel="noopener">Get the app</a>
            </nav>
          </div>
        </header>`;
    }
  }

  class SiteFooter extends HTMLElement {
    connectedCallback() {
      this.innerHTML = `
        <footer class="footer">
          <div class="container">
            <div class="footer-grid">
              <div class="footer-about">
                <a class="brand" href="${to("top")}"><img src="favicon.svg" alt=""> ChillZone</a>
                <p>A free augmented-reality throwing game for Android. Made with Unity by an indie developer.</p>
                <a href="${UA}" target="_blank" rel="noopener">
                  <span class="ua">
                    <span class="flag"><i class="b"></i><i class="y"></i></span>
                    <span><b>Stand with Ukraine</b></span>
                  </span>
                </a>
              </div>
              <div>
                <h4>Play</h4>
                <ul>
                  <li><a href="${PLAY}" target="_blank" rel="noopener">Google Play</a></li>
                  <li><a href="${to("how")}">How to play</a></li>
                  <li><a href="${to("video")}">Watch the demo</a></li>
                </ul>
              </div>
              <div>
                <h4>Project</h4>
                <ul>
                  <li><a href="${REPO}" target="_blank" rel="noopener">Source code</a></li>
                  <li><a href="${GITHUB}" target="_blank" rel="noopener">Developer</a></li>
                  <li><a href="${COFFEE}" target="_blank" rel="noopener">Buy me a coffee</a></li>
                </ul>
              </div>
              <div>
                <h4>Legacy</h4>
                <ul>
                  <li><a href="${V1_APP}" target="_blank" rel="noopener">ChillZone v1 (apk)</a></li>
                  <li><a href="${V1_PAGE}" target="_blank" rel="noopener">Web page v1</a></li>
                </ul>
              </div>
            </div>
            <div class="footer-bottom">
              <span>&copy; ${YEAR} <a href="https://github.com/DanyilT" target="_blank" rel="noopener">DanyT</a> · <a href="https://unity.com" target="_blank" rel="noopener">Built with Unity</a></span>
              <span>
                <a href="privacy.html">Privacy Policy</a> &nbsp;·&nbsp;
                <a href="terms.html">Terms &amp; Conditions</a>
              </span>
            </div>
          </div>
        </footer>`;
    }
  }

  class SiteFooterSlim extends HTMLElement {
    connectedCallback() {
      this.innerHTML = `
        <footer class="footer">
          <div class="container">
            <div class="footer-bottom" style="border-top:none;padding-top:0;">
              <span>&copy; ${YEAR} <a href="https://github.com/DanyilT" target="_blank" rel="noopener">DanyT</a></span>
              <span>
                <a href="index.html">Home</a> &nbsp;·&nbsp;
                <a href="privacy.html">Privacy Policy</a> &nbsp;·&nbsp;
                <a href="terms.html">Terms &amp; Conditions</a>
              </span>
            </div>
          </div>
        </footer>`;
    }
  }

  customElements.define("site-header", SiteHeader);
  customElements.define("site-footer", SiteFooter);
  customElements.define("site-footer-slim", SiteFooterSlim);
})();
