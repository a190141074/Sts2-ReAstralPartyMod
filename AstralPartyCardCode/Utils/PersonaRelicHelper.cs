using System;
using System.Reflection;
using AstralPartyMod.AstralPartyCardCode.Relics;
using AstralPartyMod.AstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class PersonaRelicHelper
{
    public static bool IsPersonaSkillCard(CardModel card)
    {
        return card.Type == CardType.Skill && AstralPartyCardModel.ShouldAutoApplyCooldown(card);
    }

    public static void AdvanceCooldownRelics(Player owner, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var relic in owner.Relics)
            switch (relic)
            {
                case PersonWeirdEgg weirdEgg:
                    weirdEgg.AstralParty_PersonWeirdEggCounter = Math.Min(
                        weirdEgg.AstralParty_PersonWeirdEggCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(weirdEgg);
                    break;
                case PersonSamuraiPrawn samuraiPrawn:
                    samuraiPrawn.AstralParty_PersonSamuraiPrawnCounter = Math.Min(
                        samuraiPrawn.AstralParty_PersonSamuraiPrawnCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(samuraiPrawn);
                    break;
                case PersonSlimeLulu slimeLulu:
                    slimeLulu.AstralParty_PersonSlimeLuluCounter = Math.Min(
                        slimeLulu.AstralParty_PersonSlimeLuluCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(slimeLulu);
                    break;
                case PersonMousyLian mousyLian:
                    mousyLian.AstralParty_PersonMousyLianCounter = Math.Min(
                        mousyLian.AstralParty_PersonMousyLianCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(mousyLian);
                    break;
                case PersonProprietress proprietress:
                    proprietress.AstralParty_PersonProprietressCounter = Math.Min(
                        proprietress.AstralParty_PersonProprietressCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 3)
                    );
                    RefreshRelicDisplayAmount(proprietress);
                    break;
                case PersonBlueWhale blueWhale:
                    blueWhale.AstralParty_PersonBlueWhaleCounter = Math.Min(
                        blueWhale.AstralParty_PersonBlueWhaleCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(blueWhale);
                    break;
                case PersonMascotGirlMimi mascotGirlMimi:
                    mascotGirlMimi.AstralParty_PersonMascotGirlMimiCounter = Math.Min(
                        mascotGirlMimi.AstralParty_PersonMascotGirlMimiCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(mascotGirlMimi);
                    break;
                case PersonOasisQueen oasisQueen:
                    oasisQueen.AstralParty_PersonOasisQueenCounter = Math.Min(
                        oasisQueen.AstralParty_PersonOasisQueenCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(oasisQueen);
                    break;
                case PersonInkShadowHunter inkShadowHunter:
                    inkShadowHunter.AstralParty_PersonInkShadowHunterCounter = Math.Min(
                        inkShadowHunter.AstralParty_PersonInkShadowHunterCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(inkShadowHunter);
                    break;
                case PersonJillSteinle jillSteinle:
                    jillSteinle.AstralParty_PersonJillSteinleCounter = Math.Min(
                        jillSteinle.AstralParty_PersonJillSteinleCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(jillSteinle);
                    break;
                case PersonSocialFearNun socialFearNun:
                    socialFearNun.AstralParty_PersonSocialFearNunCounter = Math.Min(
                        socialFearNun.AstralParty_PersonSocialFearNunCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(socialFearNun);
                    break;
                case PersonSupermanMegas supermanMegas:
                    supermanMegas.AstralParty_PersonSupermanMegasCounter = Math.Min(
                        supermanMegas.AstralParty_PersonSupermanMegasCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(supermanMegas);
                    break;
                case PersonXiaoLei xiaoLei:
                    xiaoLei.AstralParty_PersonXiaoLeiCounter = Math.Min(
                        xiaoLei.AstralParty_PersonXiaoLeiCounter + amount,
                        ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(owner, 4)
                    );
                    RefreshRelicDisplayAmount(xiaoLei);
                    break;
                case PersonBionicJasmine bionicJasmine:
                    bionicJasmine.AddSteps(amount);
                    break;
            }
    }

    public static void RefreshRelicDisplayAmount(object relic)
    {
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