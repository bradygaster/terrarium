# Decision: Keep ArrayList Scan() for Now

**Author:** Mike (Networking / Engine Dev)
**Date:** 2026-02-10
**Status:** Proposed

## Context

The legacy `IAnimalWorldBoundary.Scan()` method returns `System.Collections.ArrayList`. This is a non-generic collection from .NET 1.0 days.

## Decision

I kept `ArrayList` as the return type for `Scan()` because:
1. The Game project (which implements this interface) hasn't been ported yet
2. Changing it to `List<OrganismState>` now would create a compile-time dependency on the Game project's port
3. It preserves source-level compatibility with existing creature code that casts from ArrayList

## Action Required

When the Game project is ported (whoever picks that up), `IAnimalWorldBoundary.Scan()` should be changed to return `List<OrganismState>` and the `using System.Collections` import can be removed from the interface file.
