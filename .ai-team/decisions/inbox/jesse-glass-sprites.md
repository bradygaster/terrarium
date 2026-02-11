### 2025-07-16: Glass CSS expanded with full component library; all 76 original assets cataloged
**By:** Jesse (Client Dev)
**Status:** Implemented (PR #123)
**Issues:** #23, #25

**What:**
1. Glass CSS expanded from base tokens to full component library. New components: sidebar panels with frosted glass (`backdrop-filter: blur()`), text inputs, select dropdowns, toolbar with image buttons, stat grid, minimap, modal dialogs, ticker bar, tabs, icon buttons, themed scrollbars. ~60 new `--glass-*` CSS custom properties added.
2. All 76 original image assets extracted from legacy codebase to `src/Terrarium.Web/wwwroot/assets/` organized by type (sprites, terrain, ui, cursors, icons, screenshots). Full manifest.json catalog created with original paths and purpose descriptions.

**Why:**
1. The web app needs complete Glass-themed components to recreate the original Terrarium look. These CSS classes give any UI developer the building blocks to assemble the game interface without touching raw color values.
2. Brady's #1 directive: "PRESERVE ALL original imagery. When people who know what .NET Terrarium was, they should recognize it immediately." Every image asset in the legacy codebase is now available in the web project.
