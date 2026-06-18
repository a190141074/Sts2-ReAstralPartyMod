using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public sealed class SkillFateFirewoodStick : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override bool IsPlayable => FateFirewoodStickCombatHelper.HasAnyPlayableBranch(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<FateFirewoodNodePower>()
    ];

    public SkillFateFirewoodStick() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == this
            ? (PileType.Exhaust, position)
            : (pileType, position);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();

        var owner = Owner;
        var ownerCreature = owner?.Creature;
        if (owner == null || ownerCreature?.CombatState == null)
            return;
        if (owner.GetRelic<VariantPersonManosabaLinHiro>() is not { } hiroRelic)
            return;

        var branchOptions = FateFirewoodStickCombatHelper.GetAvailableBranchOptions(owner);
        if (branchOptions.Count == 0)
            return;

        var selectedBranch = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            owner,
            branchOptions,
            false,
            $"{Id.Entry}.branch");
        if (selectedBranch == null)
            return;

        var createdBranch = ownerCreature.CombatState.CreateCard(selectedBranch.CanonicalInstance ?? selectedBranch, owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(createdBranch, true, CardPilePosition.Top, this);
    }
}
