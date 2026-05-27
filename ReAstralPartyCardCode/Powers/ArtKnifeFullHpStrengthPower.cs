using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ArtKnifeFullHpStrengthPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal AppliedStrengthBonus;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override bool ShouldReceiveCombatHooks => true;

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.title");

    public override LocString Description
    {
        get
        {
            var description =
                new LocString("powers", "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.description");
            description.Add("Amount", Amount);
            return description;
        }
    }

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return ModelDb.Relic<TokenPurpleArtKnifeBeginner>().PackedIconPath;

        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrengthBonus(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return;

        await SyncStrengthBonus(applier, cardSource);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner)
            return;

        await SyncStrengthBonus(Owner, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;

        await SyncStrengthBonus(dealer, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        var data = GetInternalData<Data>();
        if (oldOwner != null && data.AppliedStrengthBonus > 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -data.AppliedStrengthBonus, oldOwner, null, true);

        data.AppliedStrengthBonus = 0m;
    }

    private async Task SyncStrengthBonus(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var desiredStrengthBonus = IsAtFullHp() ? Amount : 0m;
        var delta = desiredStrengthBonus - data.AppliedStrengthBonus;
        if (delta == 0m)
            return;

        data.AppliedStrengthBonus = desiredStrengthBonus;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }

    private bool IsAtFullHp()
    {
        return Owner != null
               && Owner.MaxHp > 0m
               && Owner.CurrentHp >= Owner.MaxHp;
    }
}
