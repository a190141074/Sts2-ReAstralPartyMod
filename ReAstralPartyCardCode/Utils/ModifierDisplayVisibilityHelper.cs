using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ModifierDisplayVisibilityHelper
{
    public static bool IsHiddenModifier(ModifierModel? modifier)
    {
        return modifier is LucidDreamMaliceModifier
            or AstralRelicStoreFirstEventModifier;
    }

    public static bool IsHiddenModifier(SerializableModifier? modifier)
    {
        if (modifier?.Id == null)
            return false;

        return modifier.Id == ModelDb.Modifier<LucidDreamMaliceModifier>().Id
            || modifier.Id == ModelDb.Modifier<AstralRelicStoreFirstEventModifier>().Id;
    }

    public static IEnumerable<ModifierModel> FilterVisibleModifiers(IEnumerable<ModifierModel> modifiers)
    {
        return modifiers.Where(static modifier => !IsHiddenModifier(modifier));
    }

    public static IEnumerable<SerializableModifier> FilterVisibleModifiers(IEnumerable<SerializableModifier> modifiers)
    {
        return modifiers.Where(static modifier => !IsHiddenModifier(modifier));
    }
}
