using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class PandaMaxHpHelper
{
    private const decimal BonusRatio = 0.4m;

    [ThreadStatic] private static int _activeGrantDepth;

    public static async Task GainMaxHpFromRelic(Creature owner, decimal baseAmount, bool healBonus)
    {
        if (baseAmount <= 0m)
            return;

        await CreatureCmd.GainMaxHp(owner, baseAmount);

        if (_activeGrantDepth > 0)
            return;
        if (owner.Player?.GetRelic<PersonalityDerivativePandaMeng>() == null)
            return;

        var bonusAmount = Math.Ceiling(baseAmount * BonusRatio);
        if (bonusAmount <= 0m)
            return;

        try
        {
            _activeGrantDepth++;
            await CreatureCmd.GainMaxHp(owner, bonusAmount);
            if (healBonus)
                await CreatureCmd.Heal(owner, bonusAmount, false);
        }
        finally
        {
            _activeGrantDepth--;
        }
    }
}
