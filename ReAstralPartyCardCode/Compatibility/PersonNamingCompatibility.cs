// Transitional compatibility aliases for the Persona -> Person internal rename.
// Keep removable old-name shims centralized here when they do not need to stay
// attached to serialization models. Delete this file once old call sites are gone.
global using PersonaSkillCardPool = ReAstralPartyMod.ReAstralPartyCardCode.CardPools.PersonSkillCardPool;
global using PersonaRelicBase = ReAstralPartyMod.ReAstralPartyCardCode.Relics.PersonRelicBase;
global using CooldownPersonaRelicBase = ReAstralPartyMod.ReAstralPartyCardCode.Relics.CooldownPersonRelicBase;
global using LegacyCooldownPersonaRelicBase = ReAstralPartyMod.ReAstralPartyCardCode.Relics.LegacyCooldownPersonRelicBase;
global using PersonaMultiplayerEffectHelper = ReAstralPartyMod.ReAstralPartyCardCode.Utils.PersonMultiplayerEffectHelper;
global using PersonaRelicHelper = ReAstralPartyMod.ReAstralPartyCardCode.Utils.PersonRelicHelper;
global using PersonaRelicRegistry = ReAstralPartyMod.ReAstralPartyCardCode.Utils.PersonRelicRegistry;
global using PersonaSkillCardFilter = ReAstralPartyMod.ReAstralPartyCardCode.Utils.PersonSkillCardFilter;
global using StartingPersonaMode = ReAstralPartyMod.ReAstralPartyCardCode.Settings.StartingPersonMode;
global using StartingPersonaDisplayMode = ReAstralPartyMod.ReAstralPartyCardCode.Settings.StartingPersonDisplayMode;
global using StartingPersonaAssignmentMode = ReAstralPartyMod.ReAstralPartyCardCode.Settings.StartingPersonAssignmentMode;
