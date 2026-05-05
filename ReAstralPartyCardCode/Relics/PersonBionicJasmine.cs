using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonBionicJasmine : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonBionicJasmineSteps { get; set; }

    [SavedProperty] public int AstralParty_PersonBionicJasmineStrength { get; set; }

    [SavedProperty] public int AstralParty_PersonBionicJasmineDexterity { get; set; }

    [SavedProperty] public bool AstralParty_PersonBionicJasminePendingTurnStartSetup { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonBionicJasmineSteps;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [AstralKeywords.CreateHoverTip(AstralKeywords.AstralStepsId)];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonBionicJasmineSteps = 0;
        AstralParty_PersonBionicJasminePendingTurnStartSetup = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null || !AstralParty_PersonBionicJasminePendingTurnStartSetup)
            return;

        AstralParty_PersonBionicJasminePendingTurnStartSetup = false;
        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, -1m, Owner.Creature, null);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, -1m, Owner.Creature, null);

        AstralParty_PersonBionicJasmineStrength = 0;
        AstralParty_PersonBionicJasmineDexterity = 0;

        var bonusCount = AstralParty_PersonBionicJasmineSteps
                         / ExtraBatteryRelicHelper.GetAdjustedBionicJasmineStepThreshold(Owner, 13);
        if (bonusCount <= 0)
            return;

        Flash();

        for (var i = 0; i < bonusCount; i++)
            if (i % 2 == 0)
            {
                await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1m, Owner.Creature, null);
                AstralParty_PersonBionicJasmineStrength++;
            }
            else
            {
                await PowerCmd.Apply<DexterityPower>(Owner.Creature, 1m, Owner.Creature, null);
                AstralParty_PersonBionicJasmineDexterity++;
            }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature == null)
            return;

        if (AstralParty_PersonBionicJasmineStrength > 0)
            await PowerCmd.Apply<StrengthPower>(
                Owner.Creature,
                -AstralParty_PersonBionicJasmineStrength,
                Owner.Creature,
                null
            );

        if (AstralParty_PersonBionicJasmineDexterity > 0)
            await PowerCmd.Apply<DexterityPower>(
                Owner.Creature,
                -AstralParty_PersonBionicJasmineDexterity,
                Owner.Creature,
                null
            );

        AstralParty_PersonBionicJasmineStrength = 0;
        AstralParty_PersonBionicJasmineDexterity = 0;
        AstralParty_PersonBionicJasminePendingTurnStartSetup = true;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        var stepsToAdd = room.RoomType switch
        {
            // 未分配/默认房间类型：增加 2 步
            RoomType.Unassigned => 2,
            // 休息站点：增加 2 步
            RoomType.RestSite => 2,
            // 商店：增加 2 步
            RoomType.Shop => 2,
            // 宝藏房：增加 2 步
            RoomType.Treasure => 2,
            // 普通怪物房：增加 5 步
            RoomType.Monster => 5,
            // 事件房：增加 3 步
            RoomType.Event => 3,
            // 精英怪房：增加 8 步
            RoomType.Elite => 8,
            // Boss 房：增加 12 步
            RoomType.Boss => 12,
            // 地图节点：增加 10 步
            RoomType.Map => 10,
            _ => 2
        };

        AstralParty_PersonBionicJasmineSteps += stepsToAdd;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public void AddSteps(int stepsToAdd)
    {
        if (stepsToAdd <= 0)
            return;

        // Keep this helper available for future step-granting effects outside room entry.
        AstralParty_PersonBionicJasmineSteps += stepsToAdd;
        Flash();
        InvokeDisplayAmountChanged();
    }
}
