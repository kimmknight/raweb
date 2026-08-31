# Generating button SVGs in `public/buttons/`

These are static SVGs, hand-authored to match GitHub's Primer button styles, served at `https://install.raweb.app/buttons/<name>.svg` and embedded via `<picture>`/`<img>` in PR descriptions and docs.

## Critical: how to measure text width

**Do not trust ImageMagick's own SVG text rendering for sizing.** It silently substitutes a generic fallback font for `-apple-system, BlinkMacSystemFont, 'Segoe UI', ...` and produces text widths that do not match real browser rendering (confirmed: differences large enough to visibly break padding). GitHub itself renders these with real Segoe UI (Windows) or the OS equivalent, so measurements must come from an actual Chromium render, not ImageMagick.

Recipe (Windows box with Edge installed):

```bash
EDGE="C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
OUT="<scratchpad>"

# 1. render the candidate SVG as an <img> in a minimal page
cat > "$OUT/page.html" <<HTML
<!DOCTYPE html><html><head><style>body{margin:0;background:#0d1117}img{display:block}</style></head>
<body><img src="file:///<path-to-svg>"></body></html>
HTML

# 2. screenshot it (unique --user-data-dir per call, or concurrent runs fail silently)
"$EDGE" --headless --disable-gpu --force-device-scale-factor=1 --window-size=500,60 \
  --user-data-dir="$OUT/profile_x" --screenshot="$OUT/out.png" "file:///$OUT/page.html"

# 3. measure real ink extent via ImageMagick (pixel-counting on an already-rendered
#    image is fine - only ImageMagick's own text layout is untrustworthy)
magick "$OUT/out.png" -crop <canvas_w>x32+0+0 +repage -grayscale Rec709Luma -threshold 40% -negate "$OUT/t.png"
magick "$OUT/t.png" -trim -format "%wx%h%O\n" info:
```

The trim result's width is the real ink width of the label text at `font-size:14; font-weight:500`.

## Sizing formula

- Padding is **exactly 12px** on the left and right (measured edge-to-ink, i.e. from the button's outer edge to where the glyphs actually start/end) — this applies uniformly whether the button has a border or not.
- `total width = measured_text_width + 24`
- `height = 32`, `border-radius = 6` (`rx="6" ry="6"`)
- `text x = total_width / 2`, `text y="16"`, `text-anchor="middle"`, `dominant-baseline="central"`
- No drop-shadow filter — current Primer buttons don't use one.
- Bordered variants: `rect x="0.5" y="0.5" width="{total-1}" height="31" stroke-width="1"`. Borderless (disabled) variants: `rect x="0" y="0" width="{total}" height="32"`, no `stroke`.
- **Verify after writing the file**: re-render with the recipe above and confirm left/right margins are equal (12px each) before considering it done. This project has shipped mismatched rect/canvas widths before (rect width stale after a resize) — always check the *actual* rendered pixels, not just the numbers in the file.

## Colors (Primer, verified against the live GitHub lookbook CSS)

| Variant | Background | Border | Text |
|---|---|---|---|
| Default, light | `#f6f8fa` | `#d0d7de` | `#1f2328` |
| Default, dark | `#21262d` | `rgba(240,246,252,0.15)` | `#c9d1d9` |
| Primary, light | `#1f883d` | `#1f232826` | `#ffffff` |
| Primary, dark | `#238636` | `#ffffff26` | `#ffffff` |
| Disabled, light | `#e6eaef` | none (`border:0`) | `#59636e` |
| Disabled, dark | `#262c36` | none (`border:0`) | `#9198a1` |
| Danger, light | `#f6f8fa` (same as default) | `#d0d7de` (same as default) | `#d1242f` |
| Danger, dark | `#21262d` (same as default) | `rgba(240,246,252,0.15)` (same as default) | `#fa5e55` |

Font: `font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif" font-size="14" font-weight="500"`.

## Naming convention

`{verb}-{subject}[-primary|-disabled|-danger]-{dark|light}.svg`, e.g. `install-this-developer-build.svg`, `install-this-developer-build-primary-dark.svg`, `view-preview-in-browser-disabled-light.svg`. Each variant needs both a `-light` and `-dark` file.
