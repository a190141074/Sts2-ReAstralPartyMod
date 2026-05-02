using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class AstralTemporaryAppliedPowerBase : ModTemporaryPowerTemplate
{
    private ModelId _originModelId = ModelId.none;
    private ModelId _internalPowerId = ModelId.none;

    public override AbstractModel OriginModel => ResolveOriginModel();

    public override PowerModel InternallyAppliedPower => ResolveInternalPower();

    public abstract string DescriptionKey { get; }

    public abstract string SmartDescriptionKey { get; }

    public abstract PowerAssetProfile BaseGameAssetProfile { get; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new RepeatVar(0)
    ];

    public override LocString Description => new("powers", DescriptionKey);

    protected override string SmartDescriptionLocKey => SmartDescriptionKey;

    public override PowerAssetProfile AssetProfile => BaseGameAssetProfile;

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

    protected abstract ModelId DefaultInternalPowerId { get; }
}

public sealed class AstralTemporaryStrengthPower : AstralTemporaryAppliedPowerBase
{
    public override string DescriptionKey => "TEMPORARY_STRENGTH_POWER.description";

    public override string SmartDescriptionKey => "TEMPORARY_STRENGTH_POWER.smartDescription";

    public override PowerAssetProfile BaseGameAssetProfile => ContentAssetProfiles.Power("strength_power");

    protected override ModelId DefaultInternalPowerId => ModelDb.GetId<StrengthPower>();

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
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

    protected override ModelId DefaultInternalPowerId => ModelDb.GetId<DexterityPower>();

    public static Task Apply(Creature owner, decimal amount, AbstractModel originModel, Creature? applier,
        CardModel? cardSource, bool silent = false)
    {
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