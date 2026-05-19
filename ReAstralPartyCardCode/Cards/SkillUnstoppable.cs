using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillUnstoppable : AstralPartyCardModel
{
    private const decimal DrawAmount = 12m;
    private static readonly int[] BonusMudTruckOnesDigits = [1, 6];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ReadyToStrikePower>(),
        HoverTipFactory.FromCard<SkillMudTruckCrash>(),
        HoverTipFactory.FromPower<FracturePower>()
    ];

    public SkillUnstoppable() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var readyToStrike = (ReadyToStrikePower)ModelDb.Power<ReadyToStrikePower>().ToMutable();
        await PowerCmd.Apply(readyToStrike, Owner.Creature, 1m, Owner.Creature, this, false);

        var attackCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (attackCards.Count > 0)
            await CardCmd.Discard(choiceContext, attackCards);

        var mudTruck = MidnightFlashHelper.CreateMudTruckCard(Owner);
        if (mudTruck != null)
            await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
                mudTruck,
                true,
                CardPilePosition.Top,
                this);

        using (readyToStrike.BeginManualDrawResolution())
        {
            await CardGainAttribution.RunWithSource(this, async () =>
            {
                var drawCount = (int)DrawAmount;
                for (var i = 0; i < drawCount; i++)
                {
                    var drawnCards = await CardPileCmd.Draw(choiceContext, 1m, Owner);
                    var drawnCard = drawnCards.FirstOrDefault();
                    if (drawnCard == null)
                        break;

                    await readyToStrike.ResolveDrawnCard(choiceContext, drawnCard);
                }
            });
        }

        await GrantBonusMudTrucksAfterDrawResolution();
    }

    private async Task GrantBonusMudTrucksAfterDrawResolution()
    {
        if (Owner?.Creature == null)
            return;

        var bonusCardCount = 0;
        if (HasBonusMudTruckOnesDigit(Owner.Creature.GetPowerAmount<VigorPower>()))
            bonusCardCount++;
        if (HasBonusMudTruckOnesDigit(Owner.Creature.GetPowerAmount<StrengthPower>()))
            bonusCardCount++;

        for (var i = 0; i < bonusCardCount; i++)
        {
            var mudTruck = MidnightFlashHelper.CreateMudTruckCard(Owner);
            if (mudTruck == null)
                break;

            await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
                mudTruck,
                true,
                CardPilePosition.Top,
                this);
        }
    }

    private static bool HasBonusMudTruckOnesDigit(decimal amount)
    {
        var onesDigit = Math.Abs((int)decimal.Truncate(amount)) % 10;
        return BonusMudTruckOnesDigits.Contains(onesDigit);
    }
}

