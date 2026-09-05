# Spire Codex

Use Spire Codex only to look up original-game content: cards, relics, monsters, events,
structure, values, and source wording. It is not authority for C# signatures, Harmony targets,
RitsuLib APIs, loaded DLL/PCK identity, or runtime behavior.

- Site: `https://spire-codex.com`
- `eng` is the safer wording reference; treat `zhs` as an auxiliary translation.
- When the question becomes “which hook/overload/parameter/runtime assembly?”, stop here and
  inspect the current `sts2.xml`, `STS2-RitsuLib.xml`, reflection, decompilation, or logs.

Recommended sequence: use the API/site to identify the original content, then return to the
current runtime and repository for implementation and verification.
