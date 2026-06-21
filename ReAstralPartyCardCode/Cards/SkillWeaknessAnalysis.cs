using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillWeaknessAnalysis : AstralPartyCardModel
{
    private static readonly Dictionary<string, int> UsesThisTurnByPlayer = new();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeaknessInsightPower>(),
        HoverTipFactory.FromPower<DefenseStancePower>(),
        HoverTipFactory.FromPower<DodgeStancePower>(),
        HoverTipFactory.FromPower<ExposedFlawPower>(),
        HoverTipFactory.FromPower<MosesNodePower>()
    ];

    public SkillWeaknessAnalysis() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override bool IsPlayable => Owner != null && GetRemainingUsesThisTurn() > 0;

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
            ? (PileType.Hand, CardPilePosition.Top)
            : (pileType, position);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (owner == null || ownerCreature == null || target == null || !target.IsAlive || target.Side == ownerCreature.Side)
            return;

        IncrementUsesThisTurn();

        if (!MosesCombatHelper.HasExposedFlaw(target))
        {
            var exposedFlawAmount = MosesCombatHelper.RollExposedFlawAmount(owner, target, this);
            if (exposedFlawAmount > 0)
                await PowerCmd.Apply<ExposedFlawPower>(target, exposedFlawAmount, ownerCreature, this, false);
        }

        var choiceOptions = MosesCombatHelper.CreateStanceChoiceOptions(owner);
        var selectedOption = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            owner,
            choiceOptions,
            false,
            $"{Id.Entry}.stance");
        if (selectedOption == null)
            return;

        var chooseDodge = selectedOption.Id == ModelDb.GetId<SkillWeaknessAnalysisChooseDodge>();
        await MosesCombatHelper.ReplaceStance(owner, this, chooseDodge);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (Owner == null || player != Owner)
            return Task.CompletedTask;

        ResetUsesThisTurn();
        return Task.CompletedTask;
    }

    private int GetRemainingUsesThisTurn()
    {
        var limit = MosesCombatHelper.GetWeaknessAnalysisTurnLimit(Owner);
        var used = GetUsesThisTurn();
        return Math.Max(limit - used, 0);
    }

    private int GetUsesThisTurn()
    {
        var ownerKey = GetOwnerTurnKey();
        return ownerKey != null && UsesThisTurnByPlayer.TryGetValue(ownerKey, out var value)
            ? Math.Max(value, 0)
            : 0;
    }

    private void IncrementUsesThisTurn()
    {
        var ownerKey = GetOwnerTurnKey();
        if (ownerKey == null)
            return;

        UsesThisTurnByPlayer[ownerKey] = GetUsesThisTurn() + 1;
    }

    private void ResetUsesThisTurn()
    {
        var ownerKey = GetOwnerTurnKey();
        if (ownerKey == null)
            return;

        UsesThisTurnByPlayer[ownerKey] = 0;
    }

    private string? GetOwnerTurnKey()
    {
        if (Owner == null)
            return null;

        return $"{Owner.NetId}:{Id.Entry}";
    }
}
