using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using STS2RitsuLib.Keywords;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonalityDerivativeDollMaking : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeDollMakingStacks { get; set; }
    [SavedProperty] public bool AstralParty_PersonalityDerivativeDollMakingComplete { get; set; }
    public bool IsComplete => AstralParty_PersonalityDerivativeDollMakingComplete;
    public override LocString Title => new("relics", IsComplete
        ? "RE_ASTRAL_PARTY_MOD_RELIC_PERSONALITY_DERIVATIVE_DOLL_COMPLETE.title"
        : "RE_ASTRAL_PARTY_MOD_RELIC_PERSONALITY_DERIVATIVE_DOLL_MAKING.title");
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => AstralParty_PersonalityDerivativeDollMakingStacks;
    protected override string IconBasePath => $"res://ReAstralPartyMod/images/relic/{(IsComplete ? "personality_derivative_doll_complete" : "personality_derivative_doll_making")}";
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EffectsStarlightPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>(),
        HoverTipFactory.FromPower<AstralLevitationPower>(),
        ..new[] { AstralKeywords.AstralPassingThroughId }.ToHoverTips()
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null || player == Owner) return;
        if (!AstralPassingThroughHelper.IsPassingThrough(Owner, player, (int)Owner.Creature.GetPowerAmount<HannaNodePower>())) return;
        AstralParty_PersonalityDerivativeDollMakingStacks = Math.Min(7, AstralParty_PersonalityDerivativeDollMakingStacks + 1);
        await PowerCmd.Apply<EffectsStarlightPower>(Owner.Creature, 1m, Owner.Creature, null, false);
        if (Owner.Creature.GetPower<AstralLevitationPower>() != null)
            await PowerCmd.Apply<EffectsStarlightPower>(player.Creature, 3m, Owner.Creature, null, false);
        if (AstralParty_PersonalityDerivativeDollMakingStacks >= 7)
            AstralParty_PersonalityDerivativeDollMakingComplete = true;
        if (AstralParty_PersonalityDerivativeDollMakingComplete)
            await AstralTemporaryDexterityPower.Apply(player.Creature, 2m, this, Owner.Creature, null, false);
        Flash();
        InvokeDisplayAmountChanged();
    }
}
