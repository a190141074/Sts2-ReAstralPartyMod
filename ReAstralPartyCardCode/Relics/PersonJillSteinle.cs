using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonJillSteinle : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonJillSteinleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonJillSteinlePendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonJillSteinleCounter;
        set => AstralParty_PersonJillSteinleCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonJillSteinlePendingCombatStartCard;
        set => AstralParty_PersonJillSteinlePendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillMixedCocktails>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMixedId),
        HoverTipFactory.FromPower<MixedCocktailsPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillMixedCocktails>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }
}
