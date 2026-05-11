using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class AstralTemporaryAppliedPowerBase : ModTemporaryPowerTemplate
{
    private const string MissingPowerIconPath = "res://images/powers/missing_power.png";
    private ModelId _originModelId = ModelId.none;
    private ModelId _internalPowerId = ModelId.none;

    public override AbstractModel OriginModel => ResolveOriginModel();

    public override PowerModel InternallyAppliedPower => ResolveInternalPower();

    public abstract string DescriptionKey { get; }

    public abstract string SmartDescriptionKey { get; }

    public abstract PowerAssetProfile BaseGameAssetProfile { get; }

    protected abstract string IconPathOverride { get; }

    protected override IEnumerable<DynamicVar> AdditionalCanonicalVars =>
    [
        new RepeatVar(0)
    ];

    public override LocString Description => new("powers", DescriptionKey);

    protected override string SmartDescriptionLocKey => SmartDescriptionKey;

    public override PowerAssetProfile AssetProfile => new()
    {
        IconPath = ResolveIconPath(),
        BigIconPath = ResolveIconPath()
    };

    public AstralTemporaryAppliedPowerBase Configure(AbstractModel originModel, PowerModel internalPower)
    {
        AssertMutable();
        _originModelId = NormalizeOriginModel(originModel).Id;
        _internalPowerId = internalPower.Id;
        return this;
    }

    protected static AbstractModel NormalizeOriginModel(AbstractModel originModel)
    {
        ArgumentNullException.ThrowIfNull(originModel);
        return originModel.IsMutable ? ModelDb.GetById<AbstractModel>(originModel.Id) : originModel;
    }

    private AbstractModel ResolveOriginModel()
    {
        if (_originModelId == ModelId.none)
            return ResolveInternalPower();

        return ModelDb.GetById<AbstractModel>(_originModelId);
    }

    private PowerModel ResolveInternalPower()
    {
        var powerId = _internalPowerId == ModelId.none ? DefaultInternalPowerId : _internalPowerId;
        return ModelDb.GetById<PowerModel>(powerId);
    }

    protected virtual string ResolveIconPath()
    {
        return ResourceLoader.Exists(IconPathOverride) ? IconPathOverride : MissingPowerIconPath;
    }

    protected abstract ModelId DefaultInternalPowerId { get; }
}

public sealed class AstralTemporaryStrengthPower : AstralTemporaryAppliedPowerBase
{
    public override string DescriptionKey => "TEMPORARY_STRENGTH_POWER.description";

    public override string SmartDescriptionKey => "TEMPORARY_STRENGTH_POWER.smartDescription";

    public override PowerAssetProfile BaseGameAssetProfile => ContentAssetProfiles.Power("strength_power");

    protected override string IconPathOverride => "res://ReAstralPartyMod/images/temp_power/temp_power_attack_up.png";

    protected override ModelId DefaultInternalPowerId => ModelDb.GetId<StrengthPower>();

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
        if (amount < 0m)
            return AstralTemporaryStrengthLossPower.Apply(owner, -amount, originModel, applier, cardSource, silent);

        var power = (AstralTemporaryStrengthPower)ModelDb.Power<AstralTemporaryStrengthPower>().ToMutable();
        return ApplyInternal(
            power.Configure(originModel, ModelDb.Power<StrengthPower>()),
            owner,
            amount,
            applier,
            cardSource,
            silent);
    }

    private static Task ApplyInternal(AstralTemporaryAppliedPowerBase power, Creature owner, decimal amount,
        Creature? applier, CardModel? cardSource, bool silent)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        return PowerCmd.Apply(power, owner, amount, applier, cardSource, silent);
    }
}

public sealed class AstralTemporaryDexterityPower : AstralTemporaryAppliedPowerBase
{
    public override string DescriptionKey => "TEMPORARY_DEXTERITY_POWER.description";

    public override string SmartDescriptionKey => "TEMPORARY_DEXTERITY_POWER.smartDescription";

    public override PowerAssetProfile BaseGameAssetProfile => ContentAssetProfiles.Power("dexterity_power");

    protected override string IconPathOverride => "res://ReAstralPartyMod/images/temp_power/temp_power_def_up.png";

    protected override ModelId DefaultInternalPowerId => ModelDb.GetId<DexterityPower>();

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
        if (amount < 0m)
            return AstralTemporaryDexterityLossPower.Apply(owner, -amount, originModel, applier, cardSource, silent);

        var power = (AstralTemporaryDexterityPower)ModelDb.Power<AstralTemporaryDexterityPower>().ToMutable();
        return ApplyInternal(
            power.Configure(originModel, ModelDb.Power<DexterityPower>()),
            owner,
            amount,
            applier,
            cardSource,
            silent);
    }

    private static Task ApplyInternal(AstralTemporaryAppliedPowerBase power, Creature owner, decimal amount,
        Creature? applier, CardModel? cardSource, bool silent)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        return PowerCmd.Apply(power, owner, amount, applier, cardSource, silent);
    }
}

public sealed class AstralTemporaryStrengthLossPower : AstralPartyPowerModel
{
    private const string IconPathOverride = "res://ReAstralPartyMod/images/temp_power/temp_power_atttack_down.png";

    public override PowerType Type => PowerType.Debuff;

    public override bool IsInstanced => true;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_STRENGTH_LOSS_POWER.title");

    public override LocString Description
    {
        get
        {
            var description =
                new LocString("powers", "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_STRENGTH_LOSS_POWER.description");
            description.Add("Amount", Amount);
            return description;
        }
    }

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_STRENGTH_LOSS_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override string ResolveIconPath()
    {
        return ResourceLoader.Exists(IconPathOverride) ? IconPathOverride : base.ResolveIconPath();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount <= 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, -Amount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null, true);
    }

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        return PowerCmd.Apply(
            ModelDb.Power<AstralTemporaryStrengthLossPower>().ToMutable(),
            owner,
            amount,
            applier,
            cardSource,
            silent);
    }
}

public sealed class AstralTemporaryDexterityLossPower : AstralPartyPowerModel
{
    private const string IconPathOverride = "res://ReAstralPartyMod/images/temp_power/temp_power_def_down.png";

    public override PowerType Type => PowerType.Debuff;

    public override bool IsInstanced => true;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_DEXTERITY_LOSS_POWER.title");

    public override LocString Description
    {
        get
        {
            var description =
                new LocString("powers", "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_DEXTERITY_LOSS_POWER.description");
            description.Add("Amount", Amount);
            return description;
        }
    }

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_ASTRAL_TEMPORARY_DEXTERITY_LOSS_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override string ResolveIconPath()
    {
        return ResourceLoader.Exists(IconPathOverride) ? IconPathOverride : base.ResolveIconPath();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount <= 0m)
            return;

        await PowerCmd.Apply<DexterityPower>(Owner, -Amount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<DexterityPower>(Owner, Amount, Owner, null, true);
    }

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
        if (amount <= 0m)
            return Task.CompletedTask;

        return PowerCmd.Apply(
            ModelDb.Power<AstralTemporaryDexterityLossPower>().ToMutable(),
            owner,
            amount,
            applier,
            cardSource,
            silent);
    }
}
