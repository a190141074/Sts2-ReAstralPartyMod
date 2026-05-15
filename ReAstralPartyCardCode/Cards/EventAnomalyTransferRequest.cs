using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyTransferRequest : AstralPartyCardModel
{
    private const int GoldLossPerOtherPlayer = 3;
    private const int StarLightPerOtherPlayer = 5;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public EventAnomalyTransferRequest() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || CombatState == null)
            return;

        var otherPlayers = CombatState.Players.Where(player => player != Owner).ToList();
        foreach (var player in otherPlayers)
        {
            if (player.Gold < GoldLossPerOtherPlayer)
                continue;

            await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(GoldLossPerOtherPlayer, player,
                GoldLossType.Spent);
        }

        var starLightAmount = otherPlayers.Count * StarLightPerOtherPlayer;
        if (starLightAmount > 0)
            await PowerCmd.Apply<StarLightPower>(Owner.Creature, starLightAmount, Owner.Creature, this, false);
    }
}
