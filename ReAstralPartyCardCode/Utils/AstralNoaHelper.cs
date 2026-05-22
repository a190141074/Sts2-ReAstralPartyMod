using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralNoaHelper
{
    private const decimal BaseBlockRatio = 0.077m;
    private const decimal WingBlockBonusRatio = 0.15m;
    private const decimal DamageThresholdGrowthFactor = 1.474m;

    public static decimal CalculateOpeningBlock(Player owner, int wings)
    {
        if (owner.Creature == null)
            return 0m;

        var multiplier = 1m + WingBlockBonusRatio * Math.Max(wings, 0);
        var block = Math.Ceiling(owner.Creature.MaxHp * BaseBlockRatio * multiplier);
        return Math.Max(0m, block);
    }

    public static int GetStartingDivineSonStacksForAct(Player? owner)
    {
        return AstralDivinePersonaHelper.GetCurrentMapAct(owner) switch
        {
            <= 1 => 2,
            2 => 4,
            _ => 6
        };
    }

    public static int GetNoaBonusAmount(decimal originalAmount)
    {
        if (originalAmount <= 0m)
            return 0;

        return (int)Math.Floor(originalAmount / 10m) * 3;
    }

    public static int GetRoundedPowerAmount(decimal amount)
    {
        return Math.Max(0, (int)Math.Round(amount, MidpointRounding.AwayFromZero));
    }

    public static int GetNextThreshold(int currentThreshold)
    {
        return Math.Max(1, (int)Math.Ceiling(Math.Max(currentThreshold, 1) * DamageThresholdGrowthFactor));
    }

    public static bool IsPoisonOrDoom(PowerModel canonicalPower)
    {
        return canonicalPower is PoisonPower or DoomPower;
    }

    public static async Task EnsureGlitchRobot(Player? owner)
    {
        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeGlitchRobot>(owner);
    }

    public static async Task InitializeOpeningState(Player owner)
    {
        var glitchRobot = owner.GetRelic<PersonalityDerivativeGlitchRobot>();
        if (glitchRobot == null)
            return;

        await glitchRobot.InitializeOpeningStateForCombat();
    }

    public static async Task GrantOpeningBlockToAllPlayers(Player owner, AbstractModel? source)
    {
        var glitchRobot = owner.GetRelic<PersonalityDerivativeGlitchRobot>();
        var wings = glitchRobot?.GetCurrentWings() ?? 0;
        var block = CalculateOpeningBlock(owner, wings);
        if (block <= 0m)
            return;

        foreach (var player in AstralDivinePersonaHelper.GetStablePlayers(owner))
        {
            if (player.Creature == null || !player.Creature.IsAlive)
                continue;

            await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Move, null);
        }
    }

    public static async Task TriggerPoisonAndDoomOnce(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature? applier,
        CardModel? source)
    {
        var poisonAmount = GetRoundedPowerAmount(target.GetPowerAmount<PoisonPower>());
        if (poisonAmount > 0)
            await CreatureCmd.Damage(choiceContext, target, poisonAmount, ValueProp.Unblockable, applier, source);

        var doomAmount = GetRoundedPowerAmount(target.GetPowerAmount<DoomPower>());
        if (doomAmount > 0 && target.IsAlive && doomAmount >= target.CurrentHp)
            await CreatureCmd.Damage(choiceContext, target, target.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, applier, source);
    }

    public static async Task ClearPoisonAndDoom(Creature target)
    {
        await CandyMachineHelper.RemovePowerIfPresent<PoisonPower>(target);
        await CandyMachineHelper.RemovePowerIfPresent<DoomPower>(target);
    }

    public static async Task SyncGlitchRobot(Player? owner)
    {
        var glitchRobot = owner?.GetRelic<PersonalityDerivativeGlitchRobot>();
        if (glitchRobot == null)
            return;

        glitchRobot.SyncDisplay();
        await Task.CompletedTask;
    }
}
