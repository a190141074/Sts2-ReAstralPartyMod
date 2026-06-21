using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonLingLing : PersonRelicBase
{
    [SavedProperty] public bool AstralParty_PersonLingLingPendingNextCombatEffect { get; set; }

    protected override string RelicId => "person_ling_ling";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillReinforcedDenial>(),
        HoverTipFactory.FromPower<LingHunLianJiePower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (!AstralParty_PersonLingLingPendingNextCombatEffect)
            return;
        if (Owner?.Creature?.CombatState == null)
            return;

        AstralParty_PersonLingLingPendingNextCombatEffect = false;
        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillReinforcedDenial>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
        await AstralTemporaryDexterityPower.Apply(Owner.Creature, 2m, this, Owner.Creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonLingLingPendingNextCombatEffect = IsVictoryForOwner();
        return Task.CompletedTask;
    }

    private bool IsVictoryForOwner()
    {
        var ownerCreature = Owner?.Creature;
        var combatState = ownerCreature?.CombatState;
        if (ownerCreature == null || combatState == null || !ownerCreature.IsAlive)
            return false;

        var anyLivingPlayer = combatState.Players.Any(player => player.Creature?.IsAlive == true);
        if (!anyLivingPlayer)
            return false;

        return combatState
            .GetOpponentsOf(ownerCreature)
            .All(enemy => !enemy.IsAlive);
    }
}
