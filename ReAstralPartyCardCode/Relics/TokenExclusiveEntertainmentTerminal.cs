using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Interactions.RightClick;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class TokenExclusiveEntertainmentTerminal : AstralPartyRelicModel, IModRightClickableRelic
{
    private const int RecordedMindCost = 1;
    private const decimal TemporaryStrengthAmount = 4m;
    private const decimal TemporaryDexterityAmount = 4m;

    [SavedProperty] public bool AstralParty_TokenExclusiveEntertainmentTerminalUsedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MindPower>(),
        HoverTipFactory.FromPower<AstralTemporaryStrengthPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFinalArenaSeriesId)
    ];

    public override Task BeforeCombatStart()
    {
        AstralParty_TokenExclusiveEntertainmentTerminalUsedThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenExclusiveEntertainmentTerminalUsedThisCombat = false;
        return Task.CompletedTask;
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Player == Owner && Owner?.Creature?.CombatState != null;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        return context.Player == Owner
               && Owner?.Creature?.CombatState != null
               && !AstralParty_TokenExclusiveEntertainmentTerminalUsedThisCombat
               && StunGunMindSyncHelper.GetRecordedMindStacks(Owner) >= RecordedMindCost;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (!CanExecuteRightClick(context))
            return;
        if (Owner?.Creature == null)
            return;

        var stunGun = Owner.GetRelic<TokenExclusiveStunGun>();
        if (stunGun == null)
            return;

        var consumed = await StunGunMindSyncHelper.AdjustRecordedMindStacks(
            stunGun,
            -RecordedMindCost,
            Owner.Creature,
            null);
        if (!consumed)
            return;

        AstralParty_TokenExclusiveEntertainmentTerminalUsedThisCombat = true;
        Flash();
        await AstralTemporaryStrengthPower.Apply(Owner.Creature, TemporaryStrengthAmount, this, Owner.Creature, null,
            true);
        await AstralTemporaryDexterityPower.Apply(Owner.Creature, TemporaryDexterityAmount, this, Owner.Creature, null,
            true);
    }
}
