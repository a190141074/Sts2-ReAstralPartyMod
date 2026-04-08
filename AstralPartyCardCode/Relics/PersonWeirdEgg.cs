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

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    // 显示 1-3 的循环进度
    public override int DisplayAmount => (AstralParty_PersonWeirdEggCounter - 1) % 3 + 1;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonWeirdEggCounter = 1;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner) return;
        if (Owner?.Creature?.CombatState == null) return;

        // 第1、4、7...回合触发 (计数器为1,4,7...)
        if ((AstralParty_PersonWeirdEggCounter - 1) % 3 == 0)
        {
            Flash();
            var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTroubleMaker>(), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null) return;
        if (side != Owner.Creature.Side) return;

        // 【修改】只递增，不再判断 >=3 后重置
        AstralParty_PersonWeirdEggCounter++;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // 战斗结束时不重置计数器，保留到下场战斗
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }
}