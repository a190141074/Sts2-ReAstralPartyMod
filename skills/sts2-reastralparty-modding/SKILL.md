---
name: sts2-reastralparty-modding
description: Work on ReAstralParty under B:\Documents\re-astral-party-mod, including its RitsuLib content, localization, combat, sync, startup, and export paths.
---

# STS2 ReAstralParty Modding

Use this overlay for work in `B:\Documents\re-astral-party-mod`. It supplements `sts2-ritsulib-modding`; read that skill first.

## Load Order

1. `sts2-ritsulib-modding`
2. `doc/AGENT.zh.md`
3. `doc/AGENT.md`
4. This file
5. A matching reference below, only when its trigger applies

## Maintenance

- `C:\Users\LuoYe\.codex\skills\sts2-reastralparty-modding` is the primary source.
- `B:\Documents\re-astral-party-mod\skills\sts2-reastralparty-modding` is its required mirror.
- Any change to this overlay or `sts2-ritsulib-modding` must update both copies in the same task. Validate both and compare their corresponding files before completion.

## Core Workflow

- Start from the named artifact: code, localization key, save, log, game-directory bundle, or UI screenshot. Treat a screenshot as evidence of observed behavior, not additional design requirements.
- Find a working repository analogue before introducing new content or abstractions. Prefer repository base models and RitsuLib helpers over handwritten framework or Harmony code.
- For API, lifecycle, or patch-signature uncertainty, inspect the current `STS2-RitsuLib.xml`, `sts2.xml`, or current decompilation before writing code. Do not guess from old branches or method names.
- Preserve user-defined gameplay semantics and player-facing wording. A parenthetical implementation note, internal id, or timing explanation is not display text unless the user explicitly says it is.
- Keep changes surgical. Do not modify protected startup/persona chains, run settings, or history unless the request authorizes that scope.

## Localization and Display

- Audit the whole player-visible surface for each changed model: title, description, smart description, card/relic/power hover, images, and any static documentation that is actually shipped.
- Preserve the user-specified sentence order, punctuation, tags, and line breaks. Do not add implementation details to player text.
- Keep three-language localization keys and tag structure aligned. Validate UTF-8 JSON after edits.
- In card localization, do not manually color any numbers: upgrade/dynamic values are colored blue by the game UI. Color gameplay keywords and named effects uniformly with `[gold]`, including Strength/力量 and Dexterity/敏捷. Leave dynamic `{...}` expressions and energy-icon helpers unchanged.
- Canonical models may be viewed with no owner or combat state. Title, description, hover, and registration paths must be canonical-safe.
- When an old mechanic is removed, search code, all localizations, static docs/indexes, resources, and generated content descriptions. Do not stop at the initiating call site.

## Hover and Content Families

- Inspect the full recursive hover graph. `HoverTipFactory.FromRelic<T>()` may cause `T`'s own `ExtraHoverTips` to become visible; verify the current runtime behavior or implementation before treating that as the source of a UI symptom.
- For a card/relic/power family, audit registration, canonical id, assets, three localizations, generation/transform paths, hover graph, and any shared observers or helpers that trigger it.
- Do not remove an old power from every context by name alone. Identify the owning gameplay chain and retain independent legacy chains the request explicitly keeps.

## Runtime and Sync

- Inspect the newest timestamped log and its first hard error before changing gameplay or sync. Separate the root error from later async, checksum, or UI fallout.
- For combat or multiplayer changes, use stable ordering, official command chains, and a single authoritative persisted/synchronized state. Snapshot collections before commands that can mutate them.
- A compiled model is not a working feature. Report separately: static checks, build/export, runtime callback evidence, and player-visible verification. Do not call an untested behavior fixed.

## Build and Export

- `dotnet build ReAstralPartyMod.csproj` is the normal full verification and deployment path. Confirm the game-directory DLL/PCK changed; compile-only output is insufficient for a requested export.
- Use `git diff --check`, localization validation, and a targeted source/localization search after edits.
- A PCK textual scan is auxiliary diagnostic evidence only. It can reveal packed stale indexes, but cannot prove runtime behavior or feature absence by itself.

## References

- Read [workflows.md](references/workflows.md) for task-specific repository routes.
- Read [timing-map.md](references/timing-map.md) for hook/lifecycle selection.
- Read [bilingual-glossary-template.md](references/bilingual-glossary-template.md) for established player terms.
- Read [specialized-workflows.md](references/specialized-workflows.md) only for rollback, startup/persona/settings, shop/moon, cross-player deck transactions, VFX/temporary cards, Final Arena, or versioned bundle work.

## Completion

State exactly which level was verified: source/static, build/export, runtime logs, or in-game player-visible behavior. When in-game verification is still needed, name the minimal scenario rather than implying it has passed.
