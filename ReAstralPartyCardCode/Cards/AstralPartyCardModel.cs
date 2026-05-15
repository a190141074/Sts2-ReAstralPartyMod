using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Content;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

public abstract partial class AstralPartyCardModel : ModCardTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual bool ShouldAutoApplyCooldownEnchantment => false;
    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string PortraitBasePath => $"res://ReAstralPartyMod/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"res://ReAstralPartyMod/images/card_portraits/{CardId}";
    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath = PortraitPath
    };

    public override string PortraitPath => $"{PortraitBasePath}.png";

    public override bool HasTurnEndInHandEffect =>
        base.HasTurnEndInHandEffect || Keywords.Contains(AstralKeywords.AstralTemporary);

    protected AstralPartyCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card == this && Pile?.Type == PileType.Discard && Keywords.Contains(AstralKeywords.AstralTemporary))
            RemoveKeyword(AstralKeywords.AstralTemporary);

        await base.AfterCardChangedPiles(card, oldPileType, source);

        if (card == this && Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
            await EnsureCooldownEnchantment();

        if (card == this && Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
            await ExhaustIfDuplicateUniqueInHand();
    }

    internal Task EnsureCooldownEnchantment()
    {
        if (!ShouldAutoApplyCooldownEnchantment)
            return Task.CompletedTask;
        if (Keywords.Contains(CardKeyword.Retain) || Keywords.Contains(CardKeyword.Exhaust))
            return Task.CompletedTask;

        CardCmd.Enchant<AstralCooldownEnchantment>(this, 1m);
        return Task.CompletedTask;
    }

    private async Task ExhaustIfDuplicateUniqueInHand()
    {
        if (!HasUniqueConstraintKeyword(this) || Owner == null)
            return;

        var handCards = PileType.Hand.GetPile(Owner).Cards;
        var hasEarlierDuplicate = handCards.Any(other =>
            !ReferenceEquals(other, this)
            && HasUniqueConstraintKeyword(other)
            && string.Equals(other.Title, Title, StringComparison.Ordinal));

        if (!hasEarlierDuplicate)
            return;

        await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), this, false, false);
    }

    private static bool HasUniqueConstraintKeyword(CardModel card)
    {
        return card.Keywords.Contains(AstralKeywords.AstralUnique);
    }

    internal static bool ShouldAutoApplyCooldown(CardModel card)
    {
        return card is AstralPartyCardModel astralCard && astralCard.ShouldAutoApplyCooldownEnchantment;
    }

    protected void RecordTelemetryOnPlay()
    {
        if (ShouldAutoApplyCooldownEnchantment)
            AstralTelemetry.RecordPersonaSkillUseIfTracked(this);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
