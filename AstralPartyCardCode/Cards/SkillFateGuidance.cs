using System;
using System.Reflection;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillFateGuidance : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    public SkillFateGuidance() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        // Advance cooldown persona relics by one step without overflowing past their ready state.
        foreach (var relic in Owner.Relics)
            switch (relic)
            {
                case PersonWeirdEgg weirdEgg:
                    weirdEgg.AstralParty_PersonWeirdEggCounter = Math.Min(
                        weirdEgg.AstralParty_PersonWeirdEggCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(weirdEgg);
                    break;
                case PersonSamuraiPrawn samuraiPrawn:
                    samuraiPrawn.AstralParty_PersonSamuraiPrawnCounter = Math.Min(
                        samuraiPrawn.AstralParty_PersonSamuraiPrawnCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(samuraiPrawn);
                    break;
                case PersonSlimeLulu slimeLulu:
                    slimeLulu.AstralParty_PersonSlimeLuluCounter = Math.Min(
                        slimeLulu.AstralParty_PersonSlimeLuluCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(slimeLulu);
                    break;
                case PersonMousyLian mousyLian:
                    mousyLian.AstralParty_PersonMousyLianCounter = Math.Min(
                        mousyLian.AstralParty_PersonMousyLianCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(mousyLian);
                    break;
                case PersonProprietress proprietress:
                    proprietress.AstralParty_PersonProprietressCounter = Math.Min(
                        proprietress.AstralParty_PersonProprietressCounter + 1,
                        3
                    );
                    RefreshRelicDisplayAmount(proprietress);
                    break;
                case PersonBlueWhale blueWhale:
                    blueWhale.AstralParty_PersonBlueWhaleCounter = Math.Min(
                        blueWhale.AstralParty_PersonBlueWhaleCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(blueWhale);
                    break;
                case PersonMascotGirlMimi mascotGirlMimi:
                    mascotGirlMimi.AstralParty_PersonMascotGirlMimiCounter = Math.Min(
                        mascotGirlMimi.AstralParty_PersonMascotGirlMimiCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(mascotGirlMimi);
                    break;
                case PersonOasisQueen oasisQueen:
                    oasisQueen.AstralParty_PersonOasisQueenCounter = Math.Min(
                        oasisQueen.AstralParty_PersonOasisQueenCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(oasisQueen);
                    break;
                case PersonInkShadowHunter inkShadowHunter:
                    inkShadowHunter.AstralParty_PersonInkShadowHunterCounter = Math.Min(
                        inkShadowHunter.AstralParty_PersonInkShadowHunterCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(inkShadowHunter);
                    break;
                case PersonJillSteinle jillSteinle:
                    jillSteinle.AstralParty_PersonJillSteinleCounter = Math.Min(
                        jillSteinle.AstralParty_PersonJillSteinleCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(jillSteinle);
                    break;
                case PersonSocialFearNun socialFearNun:
                    socialFearNun.AstralParty_PersonSocialFearNunCounter = Math.Min(
                        socialFearNun.AstralParty_PersonSocialFearNunCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(socialFearNun);
                    break;
                case PersonSupermanMegas supermanMegas:
                    supermanMegas.AstralParty_PersonSupermanMegasCounter = Math.Min(
                        supermanMegas.AstralParty_PersonSupermanMegasCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(supermanMegas);
                    break;
                case PersonXiaoLei xiaoLei:
                    xiaoLei.AstralParty_PersonXiaoLeiCounter = Math.Min(
                        xiaoLei.AstralParty_PersonXiaoLeiCounter + 1,
                        4
                    );
                    RefreshRelicDisplayAmount(xiaoLei);
                    break;
                case PersonBionicJasmine bionicJasmine:
                    bionicJasmine.AddSteps(1);
                    break;
            }

        await PlayerCmd.GainEnergy(1m, Owner);
    }

    private static void RefreshRelicDisplayAmount(object relic)
    {
        // Cooldown relics expose the counter refresh helper as a non-public base method.
        // Walk the inheritance chain so the card can reliably refresh any supported relic.
        for (var currentType = relic.GetType(); currentType != null; currentType = currentType.BaseType)
        {
            var refreshMethod = currentType.GetMethod(
                "InvokeDisplayAmountChanged",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (refreshMethod == null)
                continue;

            refreshMethod.Invoke(relic, null);
            return;
        }
    }
}