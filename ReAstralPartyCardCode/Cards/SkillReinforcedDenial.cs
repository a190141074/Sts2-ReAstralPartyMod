using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public sealed class SkillReinforcedDenial : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Retain, CardKeyword.Eternal];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LingHunLianJiePower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public SkillReinforcedDenial() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        await AstralTemporaryStrengthPower.Apply(Owner.Creature, 2m, this, Owner.Creature, this);

        var enemies = Owner.Creature.CombatState
            .GetOpponentsOf(Owner.Creature)
            .Where(enemy => enemy.IsAlive)
            .ToList();

        foreach (var enemy in enemies)
            await PowerCmd.Apply<LingHunLianJiePower>(enemy, 1m, Owner.Creature, this, false);

        var attackIntentEnemies = enemies
            .Where(PandaPersonaHelper.HasAttackIntent)
            .OrderBy(enemy => enemy.CombatId ?? uint.MaxValue)
            .ThenBy(enemy => enemy.ModelId.ToString())
            .ThenBy(enemy => enemy.SlotName ?? string.Empty)
            .ToList();

        if (attackIntentEnemies.Count == 0)
            return;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            attackIntentEnemies.Count,
            MainFile.ModId,
            Id.Entry,
            nameof(SkillReinforcedDenial),
            Owner.RunState?.Rng.StringSeed ?? string.Empty,
            Owner.NetId,
            Owner.Creature.CombatState.RoundNumber,
            attackIntentEnemies.Count);
        var target = attackIntentEnemies[selectedIndex];
        if (target.IsAlive)
            await CreatureCmd.Stun(target);
    }
}
