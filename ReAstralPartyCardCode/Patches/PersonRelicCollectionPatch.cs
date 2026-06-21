using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Unlocks;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
public static class PersonRelicCollectionPatch
{
    private const string PersonaChestIconPath = "res://ReAstralPartyMod/images/potion/person_chest_choose.png";
    private const string VariantPersonaIconPath = "res://ReAstralPartyMod/images/ui/variant_persona_relic_pool.png";

    private const string StarterHeaderZh = "初始：";
    private const string StarterHeaderZhBody = "角色们开始游戏时自身携带的遗物。";
    private const string PersonaHeaderZh = "人格遗物：";
    private const string PersonaHeaderZhBody = "来自星引擎世界的人格投影。";
    private const string VariantPersonaHeaderZh = "异格遗物：";
    private const string VariantPersonaHeaderZhBody = "来自更多世界的人格拓展包";

    private const string StarterHeaderEn = "Starter:";
    private const string StarterHeaderEnBody = "Relics that characters start the game with.";
    private const string PersonaHeaderEn = "Persona Relics:";
    private const string PersonaHeaderEnBody = "Persona projections from the Astral Party world.";
    private const string VariantPersonaHeaderEn = "Variant Persona Relics:";
    private const string VariantPersonaHeaderEnBody = "Variant persona projections from the Astral Party world.";

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
            [
                typeof(NRelicCollection), typeof(LocString), typeof(IEnumerable<RelicModel>),
                typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)
            ]
        )
        ?? throw new MissingMethodException(typeof(NRelicCollectionCategory).FullName, "LoadSubcategory");

    private static readonly MethodInfo LoadIconMethod =
        AccessTools.Method(typeof(NRelicCollectionCategory), "LoadIcon", [typeof(Texture2D)])
        ?? throw new MissingMethodException(typeof(NRelicCollectionCategory).FullName, "LoadIcon");

    private static string? _starterHeaderTemplate;
    private static Texture2D? _personaChestIcon;
    private static Texture2D? _variantPersonaIcon;

    public static void Postfix(
        NRelicCollectionCategory __instance,
        RelicRarity relicRarity,
        NRelicCollection collection,
        LocString header,
        HashSet<RelicModel> seenRelics,
        UnlockState unlockState,
        HashSet<RelicModel> allUnlockedRelics)
    {
        DiceSeriesHelper.ExpandSeenAndUnlockedDiceRelics(seenRelics, allUnlockedRelics);
        ExclusiveRelicUnlockHelper.MarkRelicsSeenAndUnlockedForCollections(seenRelics, allUnlockedRelics);

        if (relicRarity != RelicRarity.Starter)
            return;

        _starterHeaderTemplate ??= header.GetRawText();
        AddPersonaSubcategory(__instance, collection, header, seenRelics, allUnlockedRelics);
        AddVariantPersonaSubcategory(__instance, collection, header, seenRelics, allUnlockedRelics);
    }

    private static void AddPersonaSubcategory(
        NRelicCollectionCategory category,
        NRelicCollection collection,
        LocString fallbackHeader,
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (collection.Relics.Any(PersonRelicRegistry.IsPersonaRelic))
            return;

        var personaRelics = PersonRelicRegistry.GetCanonicalPersonaRelics();
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
            [collection, fallbackHeader, personaRelics, seenWithPersona, unlockedWithPersona]
        );

        ApplyCustomHeaderText(subCategory);
        ApplyCustomHeaderIcon(subCategory);
    }

    private static void AddVariantPersonaSubcategory(
        NRelicCollectionCategory category,
        NRelicCollection collection,
        LocString fallbackHeader,
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (collection.Relics.Any(PersonRelicRegistry.IsVariantPersonaRelic))
            return;

        var variantPersonaRelics = PersonRelicRegistry.GetCanonicalVariantPersonaRelics();
        if (variantPersonaRelics.Count == 0)
            return;

        var seenWithVariantPersona = seenRelics.Concat(variantPersonaRelics).ToHashSet();
        var unlockedWithVariantPersona = allUnlockedRelics.Concat(variantPersonaRelics).ToHashSet();

        var subCategories = GetSubCategories(category);
        var subCategory = (NRelicCollectionCategory)CreateForSubcategoryMethod.Invoke(category, null)!;
        var insertIndex = ((Control)HeaderLabelField.GetValue(category)!).GetIndex() + subCategories.Count + 1;

        subCategories.Add(subCategory);
        category.AddChild(subCategory);
        category.MoveChild(subCategory, insertIndex);

        LoadSubcategoryMethod.Invoke(
            subCategory,
            [collection, fallbackHeader, variantPersonaRelics, seenWithVariantPersona, unlockedWithVariantPersona]
        );

        ApplyVariantCustomHeaderText(subCategory);
        ApplyVariantCustomHeaderIcon(subCategory);
    }

    private static void ApplyCustomHeaderText(NRelicCollectionCategory subCategory)
    {
        if (HeaderLabelField.GetValue(subCategory) is not MegaRichTextLabel headerLabel)
            return;

        headerLabel.SetTextAutoSize(FormatLikeStarterHeader(_starterHeaderTemplate));
    }

    private static string FormatLikeStarterHeader(string? starterTemplate)
    {
        if (string.IsNullOrWhiteSpace(starterTemplate))
            return GetPersonaFallbackHeader();

        var formatted = starterTemplate
            .Replace(StarterHeaderZh, PersonaHeaderZh)
            .Replace(StarterHeaderZhBody, PersonaHeaderZhBody)
            .Replace(StarterHeaderEn, PersonaHeaderEn)
            .Replace(StarterHeaderEnBody, PersonaHeaderEnBody);

        return formatted == starterTemplate ? GetPersonaFallbackHeader() : formatted;
    }

    private static string GetPersonaFallbackHeader()
    {
        return TranslationServer.GetLocale().StartsWith("zh")
            ? "[gold]人格遗物：[/gold] 来自星引擎世界的人格投影。"
            : "[gold]Persona Relics:[/gold] Persona projections from the Astral Party world.";
    }

    private static void ApplyCustomHeaderIcon(NRelicCollectionCategory subCategory)
    {
        _personaChestIcon ??= GD.Load<Texture2D>(PersonaChestIconPath);
        if (_personaChestIcon == null)
            return;

        LoadIconMethod.Invoke(subCategory, [_personaChestIcon]);
    }

    private static void ApplyVariantCustomHeaderText(NRelicCollectionCategory subCategory)
    {
        if (HeaderLabelField.GetValue(subCategory) is not MegaRichTextLabel headerLabel)
            return;

        headerLabel.SetTextAutoSize(FormatLikeVariantPersonaHeader(_starterHeaderTemplate));
    }

    private static string FormatLikeVariantPersonaHeader(string? starterTemplate)
    {
        if (string.IsNullOrWhiteSpace(starterTemplate))
            return GetVariantPersonaFallbackHeader();

        var formatted = starterTemplate
            .Replace(StarterHeaderZh, VariantPersonaHeaderZh)
            .Replace(StarterHeaderZhBody, VariantPersonaHeaderZhBody)
            .Replace(StarterHeaderEn, VariantPersonaHeaderEn)
            .Replace(StarterHeaderEnBody, VariantPersonaHeaderEnBody);

        return formatted == starterTemplate ? GetVariantPersonaFallbackHeader() : formatted;
    }

    private static string GetVariantPersonaFallbackHeader()
    {
        return TranslationServer.GetLocale().StartsWith("zh")
            ? "[gold]异格人格遗物：[/gold] 来自星引擎世界的异格人格投影。"
            : "[gold]Variant Persona Relics:[/gold] Variant persona projections from the Astral Party world.";
    }

    private static void ApplyVariantCustomHeaderIcon(NRelicCollectionCategory subCategory)
    {
        _variantPersonaIcon ??= GD.Load<Texture2D>(VariantPersonaIconPath);
        if (_variantPersonaIcon == null)
            return;

        LoadIconMethod.Invoke(subCategory, [_variantPersonaIcon]);
    }

    private static List<NRelicCollectionCategory> GetSubCategories(NRelicCollectionCategory category)
    {
        return (List<NRelicCollectionCategory>)SubCategoriesField.GetValue(category)!;
    }
}
