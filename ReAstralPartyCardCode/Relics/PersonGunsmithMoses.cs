using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonGunsmithMoses : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonGunsmithMosesCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonGunsmithMosesPendingCombatStartCard { get; set; }

    protected override string RelicId => "person_gunsmith_moses";

    protected override int CounterValue
    {
        get => AstralParty_PersonGunsmithMosesCounter;
        set => AstralParty_PersonGunsmithMosesCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonGunsmithMosesPendingCombatStartCard;
        set => AstralParty_PersonGunsmithMosesPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillWeaknessAnalysis>(),
        HoverTipFactory.FromPower<WeaknessInsightPower>(),
        HoverTipFactory.FromPower<ExposedFlawPower>(),
        HoverTipFactory.FromPower<CounterPower>(),
        HoverTipFactory.FromPower<MosesNodePower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeMysteriousDodgingMan>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeMysteriousDodgingMan>(Owner);
        await MosesCombatHelper.EnsureNodeCarrier(Owner);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillWeaknessAnalysis>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
