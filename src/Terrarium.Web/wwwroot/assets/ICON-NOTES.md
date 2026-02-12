# PWA Icon Generation Notes

## Current Status
Placeholder PNG files created. Need to convert existing Terrarium icons to proper PWA sizes.

## Source Icons
- `assets/icons/tericon32_app.ico` — Main app icon (32x32)
- `assets/icons/tericon32.ico` — Alternative icon
- `assets/ui/splashscreen.png` — Could be used for screenshots

## Required Icons
1. **icon-192.png** (192x192) — Standard PWA icon
2. **icon-512.png** (512x512) — High-res PWA icon
3. **screenshot-desktop.png** (1280x720) — Desktop screenshot
4. **screenshot-mobile.png** (750x1334) — Mobile screenshot

## Generation Steps
Use ImageMagick or similar to upscale and convert:

```bash
# Convert .ico to PNG and upscale
magick assets/icons/tericon32_app.ico -resize 192x192 -background "#000060" -gravity center -extent 192x192 assets/icon-192.png
magick assets/icons/tericon32_app.ico -resize 512x512 -background "#000060" -gravity center -extent 512x512 assets/icon-512.png
```

For now, the placeholders will work for testing. The manifest.json references these paths.
