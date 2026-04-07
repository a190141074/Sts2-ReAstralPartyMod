using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class PersonWeirdEgg : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonWeirdEggCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonWeirdEggOpenedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    // 【修改】动态计算显示值，始终显示 1-3 的循环进度
    public override int DisplayAmount => (AstralParty_PersonWeirdEggCounter - 1) % 3 + 1;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonWeirdEggCounter = 1;
        AstralParty_PersonWeirdEggOpenedThisCombat = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner) return;
        if (Owner?.Creature?.CombatState == null) return;

        // 【修改】使用取模运算判断是否是周期的第1个回合 (1, 4, 7...)
        // 这样即使跨战斗，只要计数器连续，就能正确触发
        if ((AstralParty_PersonWeirdEggCounter - 1) % 3 == 0)
        {
            if (AstralParty_PersonWeirdEggOpenedThisCombat) return;

            Flash();
            var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTroubleMaker>(), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
            AstralParty_PersonWeirdEggOpenedThisCombat = true;
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null) return;
        if (side != Owner.Creature.Side) return;

        // 【修改】只递增，不再判断 >=3 后重置
        AstralParty_PersonWeirdEggCounter++;

        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // 【修改】不再重置计数器，保留当前数值到下一场战斗
        // 只重置“本回合已触发”标记，防止状态污染
        AstralParty_PersonWeirdEggOpenedThisCombat = false;
        AstralParty_PersonWeirdEggCounter++;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }
}