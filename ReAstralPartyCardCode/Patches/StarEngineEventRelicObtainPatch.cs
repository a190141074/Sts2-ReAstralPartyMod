namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

// Intentionally left unregistered. Event and combat relic replacement now happens at reward
// population sites; patching RelicCmd.Obtain as a sink can rewrite deterministic internal
// obtains and remote reward replay messages in multiplayer.
public static class StarEngineEventRelicObtainPatch
{
}
