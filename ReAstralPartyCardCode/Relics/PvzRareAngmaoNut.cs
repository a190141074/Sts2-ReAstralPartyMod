using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PvzRareAngmaoNut : AstralPartyRelicModel
{
    private static readonly decimal[] DamageCycle = [1m, 4m, 3m, 7m];

    [SavedProperty] public int AstralParty_PvzRareAngmaoNutCycleIndex { get; set; }

    protected override string RelicId => "pvz_rare_angmao_nut";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => (int)GetCurrentDamage();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzRareAngmaoNutCycleIndex = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null || !PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return;
        if (result.UnblockedDamage <= 0m)
            return;

        var damage = GetCurrentDamage();
        Flash();

        var enemies = ownerCreature.CombatState?.GetOpponentsOf(ownerCreature).ToList() ?? [];
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive)
                continue;

            await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Move, ownerCreature, null);
        }

        AstralParty_PvzRareAngmaoNutCycleIndex = (AstralParty_PvzRareAngmaoNutCycleIndex + 1) % DamageCycle.Length;
        InvokeDisplayAmountChanged();
        MainFile.Logger.Info(
            $"[PvzRareAngmaoNut] Triggered damage cycle | owner={Owner?.NetId} | damage={damage} | next={GetCurrentDamage()}");
    }

    private decimal GetCurrentDamage()
    {
        var index = Math.Clamp(AstralParty_PvzRareAngmaoNutCycleIndex, 0, DamageCycle.Length - 1);
        return DamageCycle[index];
    }
}
