using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace AstralPartyMod.AstralPartyCardCode.cards;

public abstract partial class AstralPartyCardModel : CustomCardModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitBasePath => $"res://AstralPartyMod/images/card_portraits/{CardId}";
    protected virtual string FrameBasePath => $"res://AstralPartyMod/images/card_portraits/{CardId}";

    public override string PortraitPath => $"{PortraitBasePath}.png";

    protected AstralPartyCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true, bool autoAdd = true)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}