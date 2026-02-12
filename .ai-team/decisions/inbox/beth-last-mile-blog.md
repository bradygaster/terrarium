# Decision: The Last Mile Blog Post

**Date:** 2026-02-11  
**Author:** Beth (Technical Writer)  
**Status:** Completed  
**Audience:** All team members, future readers, community  

## What Was Done

Wrote `docs/blog/journal/14-the-last-mile.md` — a detailed, technical, deeply human blog post documenting the post-Sprint 13 debugging chaos that occurred when Brady actually tried to *run* the app for the first time.

## The Context

After 13 sprints, 48 issues closed, 9 AI agents working in parallel, and every GitHub Actions test passing, the application didn't actually *run*. It had 9 distinct failures that cascaded when Brady tried `dotnet run` on the AppHost.

The post documents all 9 failures:
1. NU1901: NuGet vulnerability warning treated as error
2. NU1605: Package version downgrades
3. CS1061: Enum shadowing a bool property (naming collision)
4. CS0266/CS1061: Persistence layer casting and property name errors
5. CS0103: Razor @media directive interpreted as C# directive
6. CS1061: Aspire APIs that don't exist yet
7. Port 5000 collision (both Server and Web defaulted to same port)
8. Password parameter with no value
9. Health check returning Degraded instead of Healthy

## The Narrative Decision

This post tells the story of "the gap between tests passing and the system actually working." It's not a failure post—it's a *realism* post. It shows:

- **The irony:** 9 agents, 48 issues, 13 sprints, all tests green... and the app doesn't run because of practical integration issues.
- **The lesson:** Integration testing requires actually running the system end-to-end. No amount of unit tests, CI, or automated builds can replace a human actually launching the app.
- **The respect:** The AI agents built incredible, well-tested parts. The failure was one of scope (parts don't guarantee a system works), not quality.
- **The tone:** Developer-to-developer, self-aware, funny but respectful, with technical depth in every explanation.

## Why This Matters

This post:
1. **Resonates with every distributed systems team.** Every team with multiple services has hit this gap.
2. **Humanizes the AI agents' work.** Shows both what they excelled at (building parts, writing tests) and what they couldn't do (integration testing at runtime).
3. **Bridges to the next conversation.** "Now that the app runs, what's next?" becomes the natural question.
4. **Provides practical debugging lessons.** Every failure has a root cause analysis and a fix. Readers learn how to debug similar issues.
5. **Sets up for the Hanselman blog.** The last-mile story adds credibility to the full announcement. It's not "everything was perfect"—it's "everything was perfect until we actually ran it, then we fixed 9 real issues." That's *real*.

## Decision Points Made

1. **All 9 failures included, not just a summary.** Each failure gets error message, investigation, and fix. This is the teaching value.
2. **Escalating complexity order, not chronological.** Start with straightforward dependency problems, end with subtle integration architecture questions.
3. **Human debugging narrative.** Show the trial-and-error, the wrong API calls, the dead ends. That's realistic and educational.
4. **Emotional honesty.** The closing beat is: "Brady ran it 5 more times after this because now he knows green tests don't guarantee a working system." That's the real lesson.
5. **Respect for the agents throughout.** Never condescending. The agents built in isolation and passed tests. Integration testing at runtime is a different problem.

## What This Enables

- Hanselman blog now has a complete narrative arc: challenge → journey → integration → last-mile reality.
- Future teams can reference this post when they hit similar issues.
- The team has documented proof that "all tests pass" and "system works" are different things.
- Readers understand why human-in-the-loop is important even for AI-driven development.

## Follow-Up

The blog is ready for publication. The app now runs correctly. The next decision is whether to make small tweaks before Hanselman sees it, or to hand it as-is and let the story speak for itself.
