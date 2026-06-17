using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public sealed class SkillFateFirewoodStick : AstralPartyCardModel
{
    private const int MinNodeValue = 1;
    private const int MaxNodeValue = 6;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, AstralKeywords.FateFirewoodStickDuel];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<FateFirewoodNodePower>()
    ];

    public SkillFateFirewoodStick() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyAlly)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();

        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (owner == null || ownerCreature == null || target == null)
            return;
        if (!BaseAbilityHelper.IsOtherLivingPlayerTarget(owner, target))
            return;
        if (owner.GetRelic<VariantPersonManosabaLinHiro>() is not { } hiroRelic)
            return;

        var ownerNode = NodePowerRollHelper.RollNodeValue(owner, this, "fate_firewood_owner", MinNodeValue, MaxNodeValue);
        var targetPlayer = target.Player!;
        var targetNode = NodePowerRollHelper.RollNodeValue(targetPlayer, this, "fate_firewood_target", MinNodeValue, MaxNodeValue);

        await PowerCmd.SetAmount<FateFirewoodNodePower>(ownerCreature, ownerNode, ownerCreature, this);
        await PowerCmd.SetAmount<FateFirewoodNodePower>(target, targetNode, ownerCreature, this);

        try
        {
            if (ownerNode >= targetNode)
            {
                await ResolveSuccess(choiceContext, ownerCreature, target, hiroRelic);
                return;
            }

            await ResolveFailure(choiceContext, ownerCreature, hiroRelic);
        }
        finally
        {
            await RemoveNodePower(ownerCreature);
            await RemoveNodePower(target);
        }
    }

    private async Task ResolveSuccess(
        PlayerChoiceContext choiceContext,
        Creature ownerCreature,
        Creature target,
        VariantPersonManosabaLinHiro hiroRelic)
    {
        var totalWithAmount = GetLivingCreaturesStable(ownerCreature.CombatState)
            .Sum(creature => creature.GetPowerAmount<WithPower>());
        var successDamage = StableNumericStateHelper.FloorToNonNegativeInt(totalWithAmount * 0.5m);
        if (successDamage > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                successDamage,
                ValueProp.Move,
                ownerCreature,
                this);
        }

        await hiroRelic.TryTransferWithPowerAndRegainCard(target, this);
    }

    private async Task ResolveFailure(
        PlayerChoiceContext choiceContext,
        Creature ownerCreature,
        VariantPersonManosabaLinHiro hiroRelic)
    {
        var failureDamage = StableNumericStateHelper.FloorToNonNegativeInt(hiroRelic.GetCurrentWithPower() * 0.1m);
        if (failureDamage <= 0)
            return;

        var candidates = GetLivingCreaturesStable(ownerCreature.CombatState);
        if (candidates.Count == 0)
            return;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            Id.Entry,
            "failure_target",
            Owner?.RunState?.Rng?.StringSeed,
            Owner?.NetId,
            ownerCreature.CombatState?.RoundNumber ?? 0,
            candidates.Count);
        var randomTarget = candidates[selectedIndex];
        await CreatureCmd.Damage(
            choiceContext,
            randomTarget,
            failureDamage,
            ValueProp.Move,
            ownerCreature,
            this);
    }

    private static async Task RemoveNodePower(Creature creature)
    {
        if (creature.GetPower<FateFirewoodNodePower>() is { } nodePower)
            await PowerCmd.Remove(nodePower);
    }

    private static List<Creature> GetLivingCreaturesStable(CombatState? combatState)
    {
        if (combatState == null)
            return [];

        return combatState.Creatures
            .Where(creature => creature.IsAlive)
            .OrderBy(creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(creature => creature.ModelId.ToString())
            .ThenBy(creature => creature.SlotName ?? string.Empty)
            .ToList();
    }
}
