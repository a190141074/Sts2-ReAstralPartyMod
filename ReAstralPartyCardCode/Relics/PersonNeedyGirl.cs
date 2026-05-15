using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonNeedyGirl : CooldownPersonaRelicBase
{
    private const int StartingLoveStacks = 2;
    private const int MaxLoveStacks = 4;

    [SavedProperty] public int AstralParty_PersonNeedyGirlCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonNeedyGirlPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonNeedyGirlCounter;
        set => AstralParty_PersonNeedyGirlCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonNeedyGirlPendingCombatStartCard;
        set => AstralParty_PersonNeedyGirlPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillEmotionalOverdose>(),
        HoverTipFactory.FromPower<LovePower>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        await SetLoveAmount(StartingLoveStacks, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (!IsTrackedTarget(target))
            return;

        await AddLove(1, cardSource);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillEmotionalOverdose>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private bool IsTrackedTarget(Creature target)
    {
        if (Owner?.Creature == null)
            return false;
        if (target == Owner.Creature)
            return true;

        return KawaiiPersonaHelper.GetSameSidePlayersWithRelic<PersonKawaiiAngel>(Owner)
            .Any(player => player.Creature == target);
    }

    private async Task AddLove(int amount, CardModel? source)
    {
        if (Owner?.Creature == null || amount <= 0)
            return;

        var newAmount = Math.Min(GetLoveAmount() + amount, MaxLoveStacks);
        await SetLoveAmount(newAmount, source);
        Flash();
    }

    private async Task SetLoveAmount(int amount, CardModel? source)
    {
        if (Owner?.Creature == null)
            return;

        var clampedAmount = Math.Clamp(amount, 0, MaxLoveStacks);
        var existingPower = Owner.Creature.GetPower<LovePower>();
        if (clampedAmount <= 0)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);
            return;
        }

        await PowerCmd.SetAmount<LovePower>(Owner.Creature, clampedAmount, Owner.Creature, source);
    }

    private int GetLoveAmount()
    {
        return Owner?.Creature == null
            ? 0
            : Math.Max((int)Owner.Creature.GetPowerAmount<LovePower>(), 0);
    }
}
