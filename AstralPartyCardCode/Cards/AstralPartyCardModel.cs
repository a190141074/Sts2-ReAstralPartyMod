using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.cards;

public abstract partial class AstralPartyCardModel : CustomCardModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://AstralPartyMod/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"res://AstralPartyMod/images/card_portraits/{CardId}";

    public override string PortraitPath => $"{PortraitBasePath}.png";

    // Temporary cards should follow the normal end-of-turn hand cleanup path.
    public override bool HasTurnEndInHandEffect =>
        base.HasTurnEndInHandEffect || Keywords.Contains(AstralKeywords.AstralTemporary);

    protected AstralPartyCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (
                card == this
                && Pile?.Type == PileType.Discard
                && Keywords.Contains(AstralKeywords.AstralTemporary)
            )
            // Turn-end temporary cleanup routes cards through the play pile first, so remove the
            // keyword whenever the card actually lands in discard instead of only Hand -> Discard.
            RemoveKeyword(AstralKeywords.AstralTemporary);

        return base.AfterCardChangedPiles(card, oldPileType, source);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}