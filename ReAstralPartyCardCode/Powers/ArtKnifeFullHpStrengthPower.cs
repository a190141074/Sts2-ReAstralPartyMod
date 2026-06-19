using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ArtKnifeFullHpStrengthPower : AstralPartyPowerModel
{
    private decimal _appliedStrengthBonus;

    [SavedProperty]
    private string AstralParty_AppliedStrengthBonusSerialized
    {
        get => StableNumericStateHelper.SerializeDecimal(_appliedStrengthBonus);
        set => _appliedStrengthBonus = StableNumericStateHelper.DeserializeDecimal(value);
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
                new LocString("powers", GetDescriptionLocKey());
            description.Add("Amount", Amount);
            return description;
        }
    }

    protected override string SmartDescriptionLocKey => GetSmartDescriptionLocKey();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

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

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner != null && _appliedStrengthBonus != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrengthBonus, oldOwner, null, true);

        _appliedStrengthBonus = 0m;
    }

    private async Task SyncStrengthBonus(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var desiredStrengthBonus = IsAtFullHp() ? Amount : 0m;
        var appliedBefore = _appliedStrengthBonus;
        var delta = desiredStrengthBonus - appliedBefore;
        if (delta == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
        _appliedStrengthBonus = desiredStrengthBonus;

        MainFile.Logger.Info(
            $"[ArtKnife] art knife strength sync | owner={Owner.Player?.NetId.ToString() ?? "<none>"} | desired={desiredStrengthBonus} | applied_before={appliedBefore} | delta={delta} | hp={Owner.CurrentHp} | maxHp={Owner.MaxHp} | active={desiredStrengthBonus > 0m}");
    }

    private bool IsAtFullHp()
    {
        return ArtKnifeActivationHelper.IsActivationSatisfied(Owner);
    }

    private string GetDescriptionLocKey()
    {
        try
        {
            return ArtKnifeActivationHelper.HasLingYulinThreshold(Owner)
                ? "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.description_ling_yu_lin"
                : "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.description";
        }
        catch (CanonicalModelException)
        {
            return "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.description";
        }
    }

    private string GetSmartDescriptionLocKey()
    {
        try
        {
            return ArtKnifeActivationHelper.HasLingYulinThreshold(Owner)
                ? "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.smartDescription_ling_yu_lin"
                : "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.smartDescription";
        }
        catch (CanonicalModelException)
        {
            return "RE_ASTRAL_PARTY_MOD_POWER_ART_KNIFE_FULL_HP_STRENGTH_POWER.smartDescription";
        }
    }
}
