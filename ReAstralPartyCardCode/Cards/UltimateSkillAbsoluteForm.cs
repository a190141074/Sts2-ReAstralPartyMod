using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool), StableEntryStem = "ultimate_skill_absolute_form")]
public class UltimateSkillAbsoluteForm : AstralPartyCardModel
{
    protected override string CardId => "ultimate_skill_absolute_form";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUltimateSkill, CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralUltimateSkillId),
        HoverTipFactory.FromPower<AbsoluteFormPower>()
    ];

    public UltimateSkillAbsoluteForm() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null || CombatState == null)
            return;

        await PowerCmd.Apply<AbsoluteFormPower>(Owner.Creature, 1m, Owner.Creature, this, false);

        var alivePlayers = EventCombatTargetHelper.GetAlivePlayers(CombatState).ToList();
        if (AbsoluteFormHelper.HasFullFormSetAcrossAllRunDecks(CombatState))
        {
            await AbsoluteFormHelper.AutoPlayAllFormsForPlayer(choiceContext, Owner);
        }
        else
        {
            var effectIndex = 0;
            foreach (var player in alivePlayers.Where(player => player != Owner))
                await AbsoluteFormHelper.AutoPlayRandomFormForPlayer(choiceContext, player, effectIndex++);

            await AbsoluteFormHelper.AutoPlayRandomFormForPlayer(choiceContext, Owner, effectIndex);
        }

        if (Owner.Creature.IsAlive)
            PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
}
