using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class ChargeTokenRelicBase : AstralPartyRelicModel
{
    public override bool ShouldReceiveCombatHooks => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EffectsEnergyPower>()];
    protected Task Add(int amount, CardModel? source = null) =>
        Owner?.Creature == null ? Task.CompletedTask : EnergyChargeHelper.Add(Owner.Creature, amount, Owner.Creature, source);
    protected Task Spend(int amount, CardModel? source = null) =>
        Owner?.Creature == null ? Task.CompletedTask : EnergyChargeHelper.Spend(Owner.Creature, amount, Owner.Creature, source);
    protected Task Add(PlayerChoiceContext context, int amount, CardModel? source = null) =>
        Owner?.Creature == null ? Task.CompletedTask : EnergyChargeHelper.Add(context, Owner.Creature, amount, Owner.Creature, source);
    protected Task Spend(PlayerChoiceContext context, int amount, CardModel? source = null) =>
        Owner?.Creature == null ? Task.CompletedTask : EnergyChargeHelper.Spend(context, Owner.Creature, amount, Owner.Creature, source);
    protected PlayerChoiceContext CreateHookContext()
    {
        if (Owner?.Creature?.CombatState != null && LocalContext.NetId.HasValue)
            return new HookPlayerChoiceContext(this, LocalContext.NetId.Value, Owner.Creature.CombatState, GameActionType.Combat);
        return new ThrowingPlayerChoiceContext();
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenBlueEnergyRecycler : ChargeTokenRelicBase
{
    [SavedProperty] public int AstralParty_EnergyRecyclerEnergyProgress { get; set; }
    public override RelicRarity Rarity => RelicRarity.Common;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner == null || play.Card.Owner != Owner)
            return Task.CompletedTask;

        var amount = Math.Max(0, play.Resources.EnergySpent);
        if (amount <= 0)
            return Task.CompletedTask;

        AstralParty_EnergyRecyclerEnergyProgress += amount;
        var triggers = AstralParty_EnergyRecyclerEnergyProgress / 5;
        AstralParty_EnergyRecyclerEnergyProgress %= 5;
        return triggers > 0 ? Add(context, triggers * 2, play.Card) : Task.CompletedTask;
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player) =>
        player == Owner && EnergyChargeHelper.Get(Owner?.Creature) > 5 ? Add(context, 1) : Task.CompletedTask;
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenBlueElectricBlade : ChargeTokenRelicBase
{
    // Run saves only support the primitive integer form for this binary strength bonus.
    [SavedProperty] public int AstralParty_ElectricBladeStrengthBonus { get; set; }
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner == null || play.Card.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(play.Card) ||
            play.Card.EnergyCost.CostsX || play.Card.EnergyCost.GetWithModifiers(CostModifiers.Local) < 1m)
            return;
        await Add(context, 1, play.Card);
        await SyncStrength(context, play.Card);
    }
    protected override Task AfterPowerAmountChangedWithContext(
        PlayerChoiceContext context,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? source) =>
        power is EffectsEnergyPower ? SyncStrength(context, source) : Task.CompletedTask;
    private async Task SyncStrength(PlayerChoiceContext context, CardModel? source)
    {
        if (Owner?.Creature == null) return;
        var desired = EnergyChargeHelper.Get(Owner.Creature) > 7 ? 2 : 0;
        var delta = desired - AstralParty_ElectricBladeStrengthBonus;
        if (delta == 0m) return;
        AstralParty_ElectricBladeStrengthBonus = desired;
        await PowerCmd.Apply<StrengthPower>(context, Owner.Creature, delta, Owner.Creature, source, true);
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleAirBag : ChargeTokenRelicBase
{
    private bool _preventing;
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? source)
    {
        if (Owner?.Creature != target || amount < target.CurrentHp || !EnergyChargeHelper.Has(target, 6))
            return amount;
        _preventing = true;
        return 0m;
    }
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? source)
    {
        if (Owner?.Creature != target || result.UnblockedDamage <= 0m) return;
        if (_preventing)
        {
            _preventing = false;
            await Spend(context, 6, source);
            return;
        }
        await Add(context, 1, source);
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleShockGloves : ChargeTokenRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play) =>
        Owner != null && play.Card.Owner == Owner && WarforgeEnchantmentHelper.CountsAsSkill(play.Card) ? Add(context, 2, play.Card) : Task.CompletedTask;

    public override async Task AfterTurnEnd(PlayerChoiceContext context, CombatSide side)
    {
        if (Owner?.Creature == null || side != Owner.Creature.Side || !EnergyChargeHelper.Has(Owner.Creature, 4)) return;
        await Spend(context, 4);
        var enemies = Owner.Creature.CombatState?.Enemies.Where(e => e.IsAlive).Take(2).ToList() ?? [];
        foreach (var enemy in enemies)
            await CreatureCmd.Damage(context, enemy, 6m, ValueProp.Move, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenBlueWarpEngine : ChargeTokenRelicBase
{
    [SavedProperty] public bool AstralParty_WarpEnginePending { get; set; }
    public override RelicRarity Rarity => RelicRarity.Common;
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType is RoomType.Shop or RoomType.RestSite) AstralParty_WarpEnginePending = true;
        return Task.CompletedTask;
    }
    public override async Task BeforeCombatStart()
    {
        if (!AstralParty_WarpEnginePending || Owner?.Creature == null) return;
        AstralParty_WarpEnginePending = false;
        var context = CreateHookContext();
        await Add(context, 2);
        await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, 2m, Owner.Creature, null, true);
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenPurpleElectricCore : ChargeTokenRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) =>
        Owner != null && card.Owner == Owner && PersonRelicHelper.IsPersonSkillCard(card) ? Add(context, 1, card) : Task.CompletedTask;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner == null || play.Card.Owner != Owner || !PersonRelicHelper.IsPersonSkillCard(play.Card)) return;
        await PlayerCmd.GainEnergy(1m, Owner);
        if (EnergyChargeHelper.Has(Owner.Creature, 5))
        {
            await Spend(context, 5, play.Card);
            PersonRelicHelper.ReduceCooldownRelics(Owner, 1);
        }
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenGoldPerpetualMotionMachine : ChargeTokenRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;
        var amount = Owner.Creature.CombatState?.Encounter?.RoomType == RoomType.Boss ? 2m : 1m;
        await PowerCmd.Apply<DexterityPower>(CreateHookContext(), Owner.Creature, amount, Owner.Creature, null, true);
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || Owner.Creature == null || EnergyChargeHelper.Get(Owner.Creature) >= 6) return;
        await Add(context, 6 - EnergyChargeHelper.Get(Owner.Creature));
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenGoldElectromagneticGun : ChargeTokenRelicBase
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;
        var amount = Owner.Creature.CombatState?.Encounter?.RoomType == RoomType.Boss ? 2m : 1m;
        await PowerCmd.Apply<StrengthPower>(CreateHookContext(), Owner.Creature, amount, Owner.Creature, null, true);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner?.Creature == null || play.Card.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(play.Card) ||
            !EnergyChargeHelper.Has(Owner.Creature, 4)) return;
        await Spend(context, 4, play.Card);
        var damage = (play.Card.DynamicVars.TryGetValue("Damage", out var value) ? value.BaseValue * 0.5m : 0m) +
                     Owner.Creature.GetPowerAmount<StrengthPower>();
        if (damage <= 0m) return;
        foreach (var enemy in Owner.Creature.CombatState?.Enemies.Where(e => e.IsAlive).Take(3).ToList() ?? [])
            await CreatureCmd.Damage(context, enemy, damage, ValueProp.Move, Owner.Creature, play.Card);
    }
}
