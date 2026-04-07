// using System.Linq;
// using System.Threading.Tasks;
// using AstralPartyMod.AstralPartyCardCode.Powers;
// using BaseLib.Extensions;
// using BaseLib.Utils;
// using MegaCrit.Sts2.Core.CardSelection;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Localization;
// using MegaCrit.Sts2.Core.Localization.DynamicVars;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.CardPools;
//
// namespace AstralPartyMod.AstralPartyCardCode.cards;
//
// public class SkillStarShop: AstralPartyCardModel
// {
//     public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
//
//     public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
//
//     public override string PortraitPath => "res://AstralPartyMod/images/card_portraits/event_deus_ex_machina.png";
//
//     public override string? CustomPortraitPath => PortraitPath;
//
//     public SkillStarShop() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
//     {
//     }
//
//     protected override void OnUpgrade()
//     {
//
//     }
//
//     protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//     {
//         if (CombatState == null || Owner == null) return;
//
//         var offeredCards = ModelDb.AllCards
//             .Where(card => card is AstralPartyCardModel)
//             .Where(card => card.GetType().Name.StartsWith("CoreCard"))
//             .Where(card => card.GetType() != typeof(SkillTroubleMaker))
//             .OrderBy(_ => Owner.RunState.Rng.Niche.NextInt(int.MaxValue))
//             .Select(card =>
//             {
//                 var mutableCard = card.ToMutable();
//                 mutableCard.Owner = Owner;
//                 return mutableCard;
//             })
//             .Take(5)
//             .ToList();
//
//         if (offeredCards.Count == 0) return;
//     }
// }

