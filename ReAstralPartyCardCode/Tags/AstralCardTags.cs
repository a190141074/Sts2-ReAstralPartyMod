using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Tags;

[RegisterOwnedCardTag(CollectorsStem)]
[RegisterOwnedCardTag(PersonSkillStem)]
[RegisterOwnedCardTag(BaseAbilityStem)]
public static class AstralCardTags
{
    private static bool _registered;

    public const string CollectorsStem = "ASTRAL_COLLECTION";
    public const string PersonSkillStem = "ASTRAL_PERSON_SKILL";
    public const string BaseAbilityStem = "ASTRAL_BASE_ABILITY";

    public static string CollectorsId => GetId(CollectorsStem);
    public static string PersonSkillId => GetId(PersonSkillStem);
    public static string BaseAbilityId => GetId(BaseAbilityStem);

    public static CardTag Collectors => CollectorsId.GetModCardTag();
    public static CardTag PersonSkill => PersonSkillId.GetModCardTag();
    public static CardTag BaseAbility => BaseAbilityId.GetModCardTag();

    public static void RegisterAll()
    {
        if (_registered)
            return;

        // Auto-registration handles the actual tag minting before content discovery.
        _registered = true;
    }

    public static string GetId(string stem)
    {
        return ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, stem);
    }

    public static bool HasTag(CardModel? card, CardTag tag)
    {
        return card != null && card.Tags.Contains(tag);
    }

    public static bool HasTag(CardModel? card, string qualifiedTagId)
    {
        return card != null
               && ModCardTagRegistry.TryGetCardTag(qualifiedTagId, out var tag)
               && card.Tags.Contains(tag);
    }

    public static bool HasCollectorsTag(CardModel? card)
    {
        return HasTag(card, CollectorsId);
    }

    public static bool HasPersonSkillTag(CardModel? card)
    {
        return HasTag(card, PersonSkillId);
    }

    public static bool HasBaseAbilityTag(CardModel? card)
    {
        return HasTag(card, BaseAbilityId);
    }
}
