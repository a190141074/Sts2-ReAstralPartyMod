using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillAbyssalTransformation : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<AbyssalRealmPower>(),
        HoverTipFactory.FromPower<AbyssalTentaclesPower>(),
        HoverTipFactory.FromPower<AbyssalErosionMarkPower>()
    ];

    public SkillAbyssalTransformation() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        await ExecuteAsync(choiceContext, Owner, cardPlay.Target);
    }

    private async Task ExecuteAsync(PlayerChoiceContext choiceContext, Player owner, Creature target)
    {
        if (!target.IsAlive || target.Side == owner.Creature.Side)
            return;

        await PowerCmd.Apply<AbyssalRealmPower>(target, 1m, owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(target, -2m, owner.Creature, this, false);
        await AbyssalTentacleCombatHelper.SummonAsync(owner, this);
        await AbyssalTentacleCombatHelper.ActivateAllAsync(choiceContext, owner, this);
    }
}
