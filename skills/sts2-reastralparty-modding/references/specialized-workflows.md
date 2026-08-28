# Specialized Workflows

Read only the section matching the task. These are repo-specific constraints, not default steps for ordinary card, relic, power, or localization work.

## History Recovery

- For `backup first, notify me, then rollback`, create and report an independent backup ref and the exact rollback range, then stop for explicit confirmation.
- Preserve later formal-version bug-fix commits unless the user explicitly includes them. Do not infer a rollback boundary from an adjacent compatibility change.
- Before hard reset, revert, cherry-pick, or bulk recovery, record status and retain a recoverable ref. After recovery, prove the intended HEAD and worktree before building or editing gameplay.

## Startup, Personas, and Settings

- Treat `StartingPerson*`, `StartingPersona*`, Neow ready-page, room settings, and persona registration as protected chains. Change them only with explicit authorization and current-log evidence.
- Normal `ModifierModel`s affect Neow generation. Prefer `RunSavedData` plus a narrow hook for run-scoped state; never install hidden modifiers before `Neow.GenerateInitialOptions`.
- Gameplay settings span local persistence, lobby snapshot, run snapshot, and the runtime getter. A visible panel alone is not proof that a setting works or synchronizes.

## Store and Moon Systems

- Distinguish normal moon-relic inventory from the fixed extra moon-relic slots. Audit each source before changing availability.
- Ban sets are source filters, not global prohibitions. Use canonical runtime IDs and verify persistence, snapshot propagation, and each relevant candidate source.
- Preserve normal shop slot counts by replacing disallowed entries rather than deleting them.

## Cross-Player Deck Transactions

- Do not mutate run decks during combat. Snapshot deterministic pending work, then use formal post-combat `LoadCard`, add, and remove command chains.
- A separate play creates a separate pending batch. Persist only stable identifiers and complete a batch only after target creation, source removal, and any replacement succeed.
- Use `RunState.CloneCard` for run-deck cards, not combat-only `CreateClone()`.

## Specialized Card and VFX Work

- Read the current game API before altering `CardCmd.AutoPlay`, `OnPlayWrapper`, card-pile ownership, VFX hover, rewards, or combat-end behavior.
- Reusing a visual model does not authorize inheriting its gameplay model. Use an independent card when any inherited play, pile, hover, forge, or dynamic-variable behavior is wrong for the feature.
- For temporary cards, VFX, or right-click behavior, verify both the command path and player-visible card face, hover, pile movement, cleanup, and failure fallback.

## Final Arena and Fixed Packages

- Read this section only for Final Arena work. Preserve its existing deterministic synchronization helpers, per-player completion state, and canonical IDs; do not replace them with global completion flags or guessed minimum counts.
- Fixed replacement decks, reward-bar personas, and cross-battle counters need one authoritative helper/state source and idempotent recovery. Do not duplicate their definitions at call sites.

## Packaging Evidence

- For a full build/export, verify game-directory DLL/PCK artifacts. For versioned bundles, verify the loader, manifest, selected variant, JSON, and PCK together.
- A textual PCK scan can find stale packed documentation or localization, but it is diagnostic evidence only. Pair it with source/resource searches and actual runtime-load evidence before claiming a deleted feature is absent.
