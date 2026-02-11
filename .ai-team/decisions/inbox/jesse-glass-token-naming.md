# Decision: CSS Token Naming Convention for Glass Theme

**By:** Jesse (Client Dev)
**Date:** 2025-07-15
**Status:** Implemented (PR #102)

## What

All CSS design tokens for the Terrarium Glass theme follow the naming convention:

```
--glass-{category}-{element}-{modifier}
```

**Categories:** `color`, `gradient`, `border`, `shadow`, `font`, `spacing`, `size`, `radius`
**Elements:** `panel`, `button`, `titlebar`, `chrome`, `led`, `label`, `dialog`, `statusbar`
**Modifiers:** `top`, `bottom`, `normal`, `hover`, `pressed`, `disabled`, `highlight`, `idle`, `waiting`, `failed`

Component CSS classes use BEM-style naming: `.glass-panel`, `.glass-panel--sunk`, `.glass-titlebar__controls`.

## Why

Anyone building Blazor components or additional CSS on top of the Glass theme needs a predictable, discoverable token naming system. This convention maps directly to the legacy C# code structure (e.g., `GlassStyle.ButtonHover.Top` → `--glass-gradient-button-hover-top`) making it easy to cross-reference with the original source.

## Impact

All agents building UI components should use these tokens rather than hard-coding color values. The tokens in `glass-theme.css` are the single source of truth for Terrarium's visual identity.
