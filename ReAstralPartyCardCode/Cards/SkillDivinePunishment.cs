using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillDivinePunishment : AstralPartyCardModel
{
    private const int TeamDivineSonThreshold = 8;
    private const int MaxDebuffsDispelled = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DivineSonPower>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeBookOfHeaven>()
    ];

    public SkillDivinePunishment() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner == null)
            return;

        await PlayerCmd.GainEnergy(1m, Owner);
        if (AstralDivinePersonaHelper.GetTotalDivineSonStacks(Owner) < TeamDivineSonThreshold)
            return;

        foreach (var player in AstralDivinePersonaHelper.GetStablePlayers(Owner))
            await PlayerCmd.GainEnergy(1m, player);

        await AstralDivinePersonaHelper.DispelDebuffsFromAllPlayers(Owner, MaxDebuffsDispelled, this);
    }
}
