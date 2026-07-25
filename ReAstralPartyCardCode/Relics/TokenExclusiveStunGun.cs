using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenExclusiveStunGun : AstralPartyRelicModel
{
    private const decimal MindOnEliteKill = 1m;
    private const int MinimumMindStacks = 1;

    [SavedProperty] public int AstralParty_TokenExclusiveStunGunMindStacks { get; set; } = MinimumMindStacks;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetNormalizedMindStacks();

    internal void NotifyDisplayAmountChanged()
    {
        InvokeDisplayAmountChanged();
    }

    internal int GetNormalizedMindStacks()
    {
        return Math.Max(AstralParty_TokenExclusiveStunGunMindStacks, MinimumMindStacks);
    }

    internal void EnsureMinimumMindStacks(int minimumMindStacks = MinimumMindStacks)
    {
        if (AstralParty_TokenExclusiveStunGunMindStacks >= minimumMindStacks)
            return;

        AstralParty_TokenExclusiveStunGunMindStacks = minimumMindStacks;
        InvokeDisplayAmountChanged();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MindPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralFinalArenaSeriesId)
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        EnsureMinimumMindStacks();
        Flash();
        await PowerCmd.Apply<MindPower>(
            Owner.Creature,
            AstralParty_TokenExclusiveStunGunMindStacks,
            Owner.Creature,
            null,
            false);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return;
        if (!result.WasTargetKilled)
            return;
        if (Owner.Creature.CombatState?.Encounter?.RoomType != RoomType.Elite)
            return;

        AstralParty_TokenExclusiveStunGunMindStacks = GetNormalizedMindStacks() + 1;
        Flash();
        InvokeDisplayAmountChanged();
        await PowerCmd.Apply<MindPower>(Owner.Creature, MindOnEliteKill, Owner.Creature, cardSource, false);
    }
}
