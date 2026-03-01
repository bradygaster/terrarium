# JSON Casing Fix Session — 2026-02-12

**Requested by:** bradygaster

**Who worked:** Gus (Server Dev)

**What:** Fixed JSON property casing mismatch between C# `GameRenderState`/`CreatureRenderData` and JavaScript `terrarium-renderer.js`. Added `[JsonPropertyName]` attributes to all properties for camelCase serialization.

**Result:** Build passes. Creatures and plants now render correctly on canvas after System.Text.Json serialization produces camelCase property names.

**Decision logged to decisions.md:** 2025-07-17 JsonPropertyName attributes entry.
