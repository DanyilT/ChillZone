# 🗑️ ChillZone — landing page

The source of the ChillZone marketing site, hosted with GitHub Pages. This is an **orphan `gh-pages`
branch** — it intentionally shares no history with `main` (which holds the Unity game project).

**Live site:** https://danyilt.github.io/ChillZone/  
**GitHub repo:** https://github.com/DanyilT/ChillZone/

> No build step — it's plain HTML/CSS/JS, served as-is.

## 🗂️ Structure

| Path                                     | Purpose                                                                                                              |
|------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `index.html`                             | Landing page — hero (swipeable device stack), video, features, how‑to‑play, screenshots, "how it was made", download |
| `privacy.html` / `terms.html`            | Legal pages — thin shells that render from JSON                                                                      |
| `css/style.css`                          | All styles                                                                                                           |
| `js/components.js`                       | Shared `<site-header>` / `<site-footer>` / `<site-footer-slim>` web components (no build, no `fetch`)                |
| `legal/legal.js`                         | Renders a legal page from its JSON (deep‑linkable headings + copy‑link buttons)                                      |
| `legal/privacy.json`, `legal/terms.json` | Legal copy + `version` / `effectiveDate` — **edit these, not the HTML**                                              |
| `assets/screenshots/`                    | 18:9 phone screenshots + throw‑sequence frames (used for the hero animation)                                         |
| `assets/screenrecordings/`               | Gameplay clips (`compressed/` holds the web‑optimised hit/miss videos)                                               |
| `assets/screenshot-editor.png`           | Unity‑editor shot for the "how it was made" section                                                                  |
| `assets/og-image.jpg`                    | Social‑share image (1200×630)                                                                                        |
| `assets/play-qr.png`                     | Scan‑to‑install QR of the Google Play link (shown on hover, non‑Android desktop)                                     |
| `.nojekyll`                              | Tells GitHub Pages to serve files as‑is (no Jekyll build)                                                            |

## ✍️ Editing the legal pages

The Privacy Policy and Terms are **rendered from JSON** — edit `legal/privacy.json` / `legal/terms.json`,
never the HTML. Each file has `title`, `version`, `effectiveDate`, `intro`, a `sections[]` array
(`heading` + `content[]` of HTML strings), and an optional `outro`. Section headings automatically become
deep‑linkable anchors (`#kebab-cased-heading`, any leading number dropped) with a copy‑link button. Bump
`version` / `effectiveDate` when you change the wording.

## 🖨️ Regenerating the generated assets

```bash
# QR code — regenerate if the Play Store link changes
npx qrcode "https://play.google.com/store/apps/details?id=com.DanyT.ChillZone" -o assets/play-qr.png -w 300
```
```bash
# OG image — three screenshots composited on the brand background (needs ImageMagick)
magick -size 1200x630 xc:'#0a0e0c' \
  \( assets/screenshots/screenshot-tennisball-spawn.png -resize x520 \) -gravity center -geometry -300+0 -composite \
  \( assets/screenshots/screenshot-ball-picker.png      -resize x520 \) -gravity center -geometry +0+0   -composite \
  \( assets/screenshots/screenshot-basketball-spawn.png -resize x520 \) -gravity center -geometry +300+0 -composite \
  assets/og-image.jpg
```

## 👀 Local preview

Open `index.html` directly in a browser, or run a static server from this folder:

```bash
python3 -m http.server 8000  # then visit http://localhost:8000
```
```bash
# Or, to allow access from other devices on the same network, bind to all interfaces:
python3 -m http.server 8001 --bind 0.0.0.0  # then visit http://<your-computer-ip>:8001 from another device
```

> [!NOTE]  
> `http.server` sends no cache headers, so browsers hang onto old CSS/JS — hard‑refresh after edits.

## 📄 License

Licensed under the **MIT License** — see [LICENSE](LICENSE).
