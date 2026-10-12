using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StunGunMindSyncHelper
{
    public static int GetRecordedMindStacks(TokenExclusiveStunGun? stunGun)
    {
        return stunGun == null
            ? 0
            : Math.Max(0, stunGun.AstralParty_TokenExclusiveStunGunMindStacks);
    }

    public static int GetRecordedMindStacks(Player? owner)
    {
        return owner == null ? 0 : GetRecordedMindStacks(owner.GetRelic<TokenExclusiveStunGun>());
    }

    public static async Task<bool> SetRecordedMindStacks(
        TokenExclusiveStunGun? stunGun,
        int recordedMindStacks,
        Creature? applier,
        CardModel? cardSource)
    {
        if (stunGun?.Owner == null)
            return false;

        var clampedRecordedMind = Math.Max(0, recordedMindStacks);
        var previousRecordedMind = GetRecordedMindStacks(stunGun);
        var delta = clampedRecordedMind - previousRecordedMind;
        stunGun.AstralParty_TokenExclusiveStunGunMindStacks = clampedRecordedMind;
        stunGun.NotifyDisplayAmountChanged();

        if (delta != 0)
            await SyncCombatMindDelta(stunGun.Owner.Creature, delta, applier, cardSource);

        return true;
    }

    public static Task<bool> AdjustRecordedMindStacks(
        TokenExclusiveStunGun? stunGun,
        int delta,
        Creature? applier,
        CardModel? cardSource)
    {
        var current = GetRecordedMindStacks(stunGun);
        return SetRecordedMindStacks(stunGun, current + delta, applier, cardSource);
    }

    public static Task<bool> AdjustRecordedMindStacks(
        Player? owner,
        int delta,
        Creature? applier,
        CardModel? cardSource)
    {
        return AdjustRecordedMindStacks(owner?.GetRelic<TokenExclusiveStunGun>(), delta, applier, cardSource);
    }

    public static async Task ApplyRecordedMindAtCombatStart(TokenExclusiveStunGun? stunGun)
    {
        if (stunGun?.Owner?.Creature == null)
            return;

        var recordedMindStacks = GetRecordedMindStacks(stunGun);
        if (recordedMindStacks <= 0)
            return;

        await PowerCmd.Apply<MindPower>(stunGun.Owner.Creature, recordedMindStacks, stunGun.Owner.Creature, null,
            false);
    }

    private static async Task SyncCombatMindDelta(
        Creature? ownerCreature,
        int delta,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ownerCreature?.CombatState == null || delta == 0)
            return;

        var currentMind = Math.Max(0m, ownerCreature.GetPowerAmount<MindPower>());
        var desiredMind = Math.Max(0m, currentMind + delta);
        if (desiredMind == currentMind)
            return;

        await PowerCmd.SetAmount<MindPower>(ownerCreature, desiredMind, applier ?? ownerCreature, cardSource);
    }
}
