### 2025-07-16: SDK Samples Structure and Conventions
**By:** Hank (Tester/QA)
**Status:** Implemented (PR #122)

**What:**
1. SDK samples live in `src/Terrarium.Samples/{SampleName}/` — each sample is a standalone `.csproj` referencing `Terrarium.OrganismBase`.
2. Three samples ported: SimpleHerbivore, SimpleCarnivore, SimplePlant. Together they cover all key creature APIs.
3. Sample README at `src/Terrarium.Samples/README.md` documents attributes, APIs, build instructions, and how to create new creatures.
4. Samples follow the modern API conventions: nullable annotations, pattern matching, modern event handler syntax.

**Why:**
- These are how developers learn to write Terrarium creatures. They must compile, be correct, and demonstrate the full API surface.
- Each sample is a standalone project so developers can copy one as a starting point without pulling in the full solution.
- The README serves as the primary developer-facing documentation for creature authoring.
