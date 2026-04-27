using AstralPartyMod.AstralPartyCardCode.Modifiers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class StarEngineModifierModelDbPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(StarEngineModifier)))
            ModelDb.Inject(typeof(StarEngineModifier));
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
public static class StarEngineGoodModifiersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
    {
        if (__result.Any(modifier => modifier is StarEngineModifier))
            return;

        var modifiers = __result.ToList();
        modifiers.Add(ModelDb.Modifier<StarEngineModifier>());
        __result = modifiers;
    }
}
