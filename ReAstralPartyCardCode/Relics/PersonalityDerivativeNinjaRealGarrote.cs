using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonalityDerivativeNinjaRealGarrote : AstralPartyRelicModel,
    IRelicExtraIconAmountLabelsProvider,
    IRelicExtraIconAmountLabelsChangeSource
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage { get; set; }

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(0, AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromRelic<VariantPersonNinja>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage =
            Math.Max(0, AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage);
        InvokeDisplayAmountChanged();
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side || amount <= 0m)
            return 0m;
        if (cardSource?.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsSkill(cardSource))
            return 0m;

        return Math.Max(0, AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage);
    }

    internal void HandleThirdGlobalSkill()
    {
        if (Owner == null)
            return;

        Owner.GetRelic<VariantPersonNinja>()?.AdvanceCooldownProgressFromExternalEffect(1);
        AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage =
            Math.Max(0, AstralParty_PersonalityDerivativeNinjaRealGarroteSkillDamage) + 1;
        InvokeDisplayAmountChanged();
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
        Flash();
    }

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots()
    {
        var stage = Owner?.GetRelic<VariantPersonNinja>()?.GetGlobalSkillSequenceStage() ?? 1;
        return
        [
            ExtraIconAmountLabelSlot.At(ExtraIconAmountLabelCorner.TopRight, stage.ToString())
        ];
    }
}
