using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;
using System.Threading.Tasks;

namespace AstralPartyMod.AstralPartyCardCode.cards;

// 4. 拥挤通道：给所有友方单位上一层缓冲+易伤
[Pool(typeof(EventCardPool))]
public class EventCrowdedPassage : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new("Buffer", 1m), new("Vulnerable", 1m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BufferPower>(), HoverTipFactory.FromPower<VulnerablePower>()];

    public EventCrowdedPassage() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        foreach (var player in CombatState.Players)
        {
            await PowerCmd.Apply<BufferPower>(player.Creature, DynamicVars["Buffer"].BaseValue, null, null);
            await PowerCmd.Apply<VulnerablePower>(player.Creature, DynamicVars["Vulnerable"].BaseValue, null, null);
        }
    }
}