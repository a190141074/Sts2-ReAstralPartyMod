using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(SharedRelicPool))]
public class SkillTyrantForm : AstralPartyCardModel
{
    private const int BaseForge = 18;
    private const int BaseStarCostValue = 6;

    protected override string CardId => "tyrant_form";

    public override int CanonicalStarCost => BaseStarCostValue;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ForgeVar(BaseForge)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromForge(),
        HoverTipFactory.FromCard<SovereignBlade>(),
        HoverTipFactory.FromPower<TyrantFormPower>()
    ];

    public SkillTyrantForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await ForgeCmd.Forge(DynamicVars.Forge.IntValue, Owner, this);

        await PowerCmd.Apply(
            ModelDb.Power<TyrantFormPower>().ToMutable(),
            Owner.Creature,
            1m,
            Owner.Creature,
            this,
            false);

        var endTurnAction = new EndPlayerTurnAction(Owner, 0);
        await endTurnAction.Execute();
    }
}
