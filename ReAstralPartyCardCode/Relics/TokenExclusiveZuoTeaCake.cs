using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveZuoTeaCake : AstralPartyRelicModel
{
    [SavedProperty] public bool AstralParty_TokenExclusiveZuoTeaCakeActive { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<ZuoTeaCakePower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMagicAcademySeriesId)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenExclusiveZuoTeaCakeActive = true;
    }

    public override async Task BeforeCombatStart()
    {
        if (!AstralParty_TokenExclusiveZuoTeaCakeActive || Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1m, Owner.Creature, null, true);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!AstralParty_TokenExclusiveZuoTeaCakeActive || Owner?.Creature == null)
            return;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Attack)
            return;
        if (result.TotalDamage <= 0m)
            return;

        var roomType = Owner.Creature.CombatState?.Encounter?.RoomType;
        if (roomType is RoomType.Elite or RoomType.Boss)
            return;

        Flash();
        await PowerCmd.Apply<ZuoTeaCakePower>(Owner.Creature, 1m, Owner.Creature, cardSource, false);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.RoomType == RoomType.Boss)
            AstralParty_TokenExclusiveZuoTeaCakeActive = false;

        return Task.CompletedTask;
    }
}