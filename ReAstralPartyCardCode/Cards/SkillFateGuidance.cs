using System;
using System.Reflection;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillFateGuidance : AstralPartyCardModel
{
    [SavedProperty] public string AstralParty_FateGuidanceSourceBlueWhalePlayerNetIdRaw { get; set; } = string.Empty;

    public ulong AstralParty_FateGuidanceSourceBlueWhalePlayerNetId
    {
        get => ulong.TryParse(AstralParty_FateGuidanceSourceBlueWhalePlayerNetIdRaw, out var value) ? value : 0UL;
        set => AstralParty_FateGuidanceSourceBlueWhalePlayerNetIdRaw = value.ToString();
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

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
        RecordTelemetryOnPlay();
        if (Owner == null)
            return;

        PersonaRelicHelper.AdvanceCooldownRelics(Owner, 1);

        await PlayerCmd.GainEnergy(1m, Owner);
    }
}

