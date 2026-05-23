using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralDivinePersonaHelper
{
    private const int SaraChargeThreshold = 7;
    private const decimal DivinePowerAmount = 1m;
    public static IReadOnlyList<Player> GetStablePlayers(Player owner)
    {
        return PersonaMultiplayerEffectHelper.GetStableCombatPlayers(owner);
    }

    public static async Task<PersonalityDerivativeDivineThrone?> EnsureDivineThrone(Player? owner)
    {
        return await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeDivineThrone>(owner)
               as PersonalityDerivativeDivineThrone;
    }

    public static async Task<PersonalityDerivativeBookOfHeaven?> EnsureBookOfHeaven(Player? owner)
    {
        return await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeBookOfHeaven>(owner)
               as PersonalityDerivativeBookOfHeaven;
    }

    public static async Task GrantDivineSonToAllPlayers(Player owner, AbstractModel? source)
    {
        foreach (var player in GetStablePlayers(owner))
        {
            if (player.Creature == null)
                continue;

            await PowerCmd.Apply<DivineSonPower>(player.Creature, DivinePowerAmount, owner.Creature, source as CardModel, false);
        }
    }

    public static async Task GrantDivineThroneToAllPlayers(Player owner, AbstractModel? source)
    {
        foreach (var player in GetStablePlayers(owner))
        {
            if (player.Creature == null)
                continue;

            await AstralTemporaryStrengthPower.Apply(player.Creature, 1m, source ?? ModelDb.Power<DivineThronePower>(),
                owner.Creature, source as CardModel, true);
            await PowerCmd.Apply<DivineThronePower>(player.Creature, DivinePowerAmount, owner.Creature, source as CardModel,
                false);
        }
    }

    public static async Task SyncSaraMilestone(Player owner, int chargeAmount, AbstractModel? source)
    {
        if (chargeAmount <= 0)
            return;

        var milestones = chargeAmount / SaraChargeThreshold;
        if (milestones <= 0)
            return;

        for (var i = 0; i < milestones; i++)
        {
            await GrantDivineSonToAllPlayers(owner, source);
            await GrantDivineThroneToAllPlayers(owner, source);
        }

        foreach (var player in GetStablePlayers(owner))
        {
            var derivative = await EnsureDivineThrone(player) as PersonalityDerivativeDivineThrone;
            derivative?.SetDisplayedCharge(chargeAmount);
        }
    }

    public static async Task SyncSaraChargeDisplay(Player owner, int chargeAmount)
    {
        foreach (var player in GetStablePlayers(owner))
        {
            var derivative = await EnsureDivineThrone(player) as PersonalityDerivativeDivineThrone;
            derivative?.SetDisplayedCharge(chargeAmount);
        }
    }

    public static int GetCurrentMapAct(Player? owner)
    {
        return Math.Max((owner?.RunState?.CurrentActIndex ?? 0) + 1, 1);
    }

    public static int GetDivineNodeThresholdForAct(Player? owner)
    {
        return GetCurrentMapAct(owner) switch
        {
            <= 1 => 6,
            2 => 5,
            _ => 4
        };
    }

    public static int GetStrongestBookOfHeavenStacks(Player owner)
    {
        return GetStablePlayers(owner)
            .Select(player => player.GetRelic<PersonalityDerivativeBookOfHeaven>()?.Stacks ?? 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    public static int GetBonusMaxEnergyFromBookOfHeaven(Player owner)
    {
        var stacks = GetStrongestBookOfHeavenStacks(owner);
        var bonus = 0;
        if (stacks >= 6)
            bonus++;
        if (stacks >= 10)
            bonus++;

        return bonus;
    }

    public static int GetTotalDivineSonStacks(Player owner)
    {
        return GetStablePlayers(owner)
            .Where(player => player.Creature != null)
            .Sum(player => (int)player.Creature!.GetPowerAmount<DivineSonPower>());
    }

    public static async Task DispelDebuffsFromAllPlayers(Player owner, int maxPerPlayer, AbstractModel? source)
    {
        foreach (var player in GetStablePlayers(owner))
        {
            var creature = player.Creature;
            if (creature == null)
                continue;

            var debuffs = creature.Powers
                .Where(power => power.Type == PowerType.Debuff)
                .OrderBy(power => power.Id.Entry, StringComparer.Ordinal)
                .Take(maxPerPlayer)
                .ToList();
            foreach (var debuff in debuffs)
                await PowerCmd.Remove(debuff);
        }
    }

    public static async Task HandleShatterStarKillExtraTurn(Player owner, VariantPersonSara sara, AbstractModel? source)
    {
        sara.QueuePendingShatterStarExtraTurn();
        MainFile.Logger.Info(
            $"[AstralDivine] Queued Sara extra turn from Shatter Star kill for current combat | owner={owner.NetId} | pending={sara.GetPendingExtraTurnCount()}");
        await AstralMoveAgainDisplayHelper.Sync(owner);
    }
}
