using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "variant_person_ling_yulin")]
public class VariantPersonLingYulin : CooldownPersonaRelicBase
{
    private const decimal MaxHpBonus = 10m;
    private const decimal WaterWrapTriggerThreshold = 8m;
    private const decimal WaterWrapGainAmount = 10m;

    [SavedProperty] public int AstralParty_VariantPersonLingYulinCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonLingYulinPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonLingYulinCounter;
        set => AstralParty_VariantPersonLingYulinCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonLingYulinPendingCombatStartCard;
        set => AstralParty_VariantPersonLingYulinPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 4;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSummonRain>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<WaterWrapPower>(),
        HoverTipFactory.FromPower<RainGracePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Creature != null)
            await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpBonus);
    }

    public override Task BeforeCombatStart()
    {
        return Task.CompletedTask;
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player != Owner)
            return;
        if (!Hook.ShouldFlush(Owner.Creature.CombatState, player))
            return;
        if (Owner.Creature.GetPowerAmount<HalfLifeHealPower>() <= WaterWrapTriggerThreshold)
            return;

        Flash();
        await PowerCmd.Apply<WaterWrapPower>(Owner.Creature, WaterWrapGainAmount, Owner.Creature, null, false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSummonRain>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
