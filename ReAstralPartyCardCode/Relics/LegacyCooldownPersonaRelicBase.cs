namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class LegacyCooldownPersonaRelicBase : CooldownPersonaRelicBase
{
    private int _legacyCounter = 1;
    private bool _legacyPendingCombatStartCard;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartCard;

    protected override int CounterValue
    {
        get => _legacyCounter;
        set
        {
            _legacyCounter = NormalizeCanonicalCounter(value);
            _hasCanonicalCounter = true;
        }
    }

    protected override bool PendingCombatStartCard
    {
        get => _legacyPendingCombatStartCard;
        set
        {
            _legacyPendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    protected int GetCanonicalCounter()
    {
        return _legacyCounter;
    }

    protected void SetCanonicalCounter(int value)
    {
        CounterValue = value;
    }

    protected bool GetCanonicalPendingCombatStartCard()
    {
        return _legacyPendingCombatStartCard;
    }

    protected void SetCanonicalPendingCombatStartCard(bool value)
    {
        PendingCombatStartCard = value;
    }

    protected void SetLegacyCounterAliasIfMissing(int value)
    {
        if (!_hasCanonicalCounter && value != default)
            _legacyCounter = NormalizeLegacyCounterAlias(value);
    }

    protected void SetLegacyPendingAliasIfMissing(bool value)
    {
        if (!_hasCanonicalPendingCombatStartCard && ShouldApplyLegacyPendingAlias(value))
            _legacyPendingCombatStartCard = MapLegacyPendingAlias(value);
    }

    protected virtual int NormalizeCanonicalCounter(int value)
    {
        return value;
    }

    protected virtual int NormalizeLegacyCounterAlias(int value)
    {
        return NormalizeCanonicalCounter(value);
    }

    protected virtual bool ShouldApplyLegacyPendingAlias(bool value)
    {
        return value;
    }

    protected virtual bool MapLegacyPendingAlias(bool value)
    {
        return value;
    }
}
