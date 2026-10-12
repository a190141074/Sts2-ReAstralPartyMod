using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "person_xiao_lei_doppelganger")]
public sealed class PersonXiaoLeiDoppelganger : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonXiaoLeiDoppelgangerCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonXiaoLeiDoppelgangerPendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonXiaoLeiDoppelgangerCounter;
        set => AstralParty_PersonXiaoLeiDoppelgangerCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonXiaoLeiDoppelgangerPendingCombatStartCard;
        set => AstralParty_PersonXiaoLeiDoppelgangerPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillChainReaction>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeLakeMarshKing>(),
        HoverTipFactory.FromPower<TrueDragonFormRealPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeLakeMarshKing>(Owner);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null) return;
        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillChainReaction>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
