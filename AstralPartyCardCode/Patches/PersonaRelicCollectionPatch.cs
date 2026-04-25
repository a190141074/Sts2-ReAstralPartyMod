using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using AstralPartyMod.AstralPartyCardCode.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Unlocks;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
public static class PersonaRelicCollectionPatch
{
    private const string LocTable = "relic_collection";
    private const string SubcategoryKey = "ASTRALPARTYMOD-PERSONA_RELIC_COLLECTION_SUBCATEGORY";
    private const string PersonaChestIconPath = "res://AstralPartyMod/images/potion/person_chest_choose.png";

    private const string StarterHeaderZh = "初始：";
    private const string StarterHeaderZhBody = "角色们开始游戏时自身携带的遗物。";
    private const string PersonaHeaderZh = "人格遗物：";
    private const string PersonaHeaderZhBody = "来自星引擎世界的人格投影。";

    private const string StarterHeaderEn = "Starter:";
    private const string StarterHeaderEnBody = "Relics that characters start the game with.";
    private const string PersonaHeaderEn = "Persona Relics:";
    private const string PersonaHeaderEnBody = "Persona projections from the Astral Party world.";

    private static readonly FieldInfo HeaderLabelField =
        AccessTools.Field(typeof(NRelicCollectionCategory), "_headerLabel")
        ?? throw new MissingFieldException(typeof(NRelicCollectionCategory).FullName, "_headerLabel");

    private static readonly FieldInfo SubCategoriesField =
        AccessTools.Field(typeof(NRelicCollectionCategory), "_subCategories")
        ?? throw new MissingFieldException(typeof(NRelicCollectionCategory).FullName, "_subCategories");

    private static readonly MethodInfo CreateForSubcategoryMethod =
        AccessTools.Method(typeof(NRelicCollectionCategory), "CreateForSubcategory")
        ?? throw new MissingMethodException(typeof(NRelicCollectionCategory).FullName, "CreateForSubcategory");

    private static readonly MethodInfo LoadSubcategoryMethod =
        AccessTools.Method(
            typeof(NRelicCollectionCategory),
            "LoadSubcategory",
            [typeof(NRelicCollection), typeof(LocString), typeof(IEnumerable<RelicModel>), typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)]
        )
        ?? throw new MissingMethodException(typeof(NRelicCollectionCategory).FullName, "LoadSubcategory");

    private static readonly MethodInfo LoadIconMethod =
        AccessTools.Method(typeof(NRelicCollectionCategory), "LoadIcon", [typeof(Texture2D)])
        ?? throw new MissingMethodException(typeof(NRelicCollectionCategory).FullName, "LoadIcon");

    private static string? _starterHeaderTemplate;
    private static Texture2D? _personaChestIcon;

    public static void Postfix(
        NRelicCollectionCategory __instance,
        RelicRarity relicRarity,
        NRelicCollection collection,
        LocString header,
        HashSet<RelicModel> seenRelics,
        UnlockState unlockState,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Starter)
            return;

        _starterHeaderTemplate ??= header.GetRawText();
        AddPersonaSubcategory(__instance, collection, seenRelics, allUnlockedRelics);
    }

    private static void AddPersonaSubcategory(
        NRelicCollectionCategory category,
        NRelicCollection collection,
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (collection.Relics.Any(PersonaRelicRegistry.IsPersonaRelic))
            return;

        var personaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics();
        if (personaRelics.Count == 0)
            return;

        var seenWithPersona = seenRelics.Concat(personaRelics).ToHashSet();
        var unlockedWithPersona = allUnlockedRelics.Concat(personaRelics).ToHashSet();

        var subCategories = GetSubCategories(category);
        var subCategory = (NRelicCollectionCategory)CreateForSubcategoryMethod.Invoke(category, null)!;
        var insertIndex = ((Control)HeaderLabelField.GetValue(category)!).GetIndex() + subCategories.Count + 1;

        subCategories.Add(subCategory);
        category.AddChild(subCategory);
        category.MoveChild(subCategory, insertIndex);

        LoadSubcategoryMethod.Invoke(
            subCategory,
            [collection, new LocString(LocTable, SubcategoryKey), personaRelics, seenWithPersona, unlockedWithPersona]
        );

        ApplyCustomHeaderText(subCategory);
        ApplyCustomHeaderIcon(subCategory);
    }

    private static void ApplyCustomHeaderText(NRelicCollectionCategory subCategory)
    {
        if (HeaderLabelField.GetValue(subCategory) is not MegaRichTextLabel headerLabel)
            return;

        var fallback = new LocString(LocTable, SubcategoryKey).GetRawText();
        headerLabel.SetTextAutoSize(FormatLikeStarterHeader(_starterHeaderTemplate, fallback));
    }

    private static string FormatLikeStarterHeader(string? starterTemplate, string fallback)
    {
        if (string.IsNullOrWhiteSpace(starterTemplate))
            return fallback;

        var formatted = starterTemplate
            .Replace(StarterHeaderZh, PersonaHeaderZh)
            .Replace(StarterHeaderZhBody, PersonaHeaderZhBody)
            .Replace(StarterHeaderEn, PersonaHeaderEn)
            .Replace(StarterHeaderEnBody, PersonaHeaderEnBody);

        return formatted == starterTemplate ? fallback : formatted;
    }

    private static void ApplyCustomHeaderIcon(NRelicCollectionCategory subCategory)
    {
        _personaChestIcon ??= GD.Load<Texture2D>(PersonaChestIconPath);
        if (_personaChestIcon == null)
            return;

        LoadIconMethod.Invoke(subCategory, [_personaChestIcon]);
    }

    private static List<NRelicCollectionCategory> GetSubCategories(NRelicCollectionCategory category)
    {
        return (List<NRelicCollectionCategory>)SubCategoriesField.GetValue(category)!;
    }
}
