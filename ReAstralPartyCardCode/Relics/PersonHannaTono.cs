using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using STS2RitsuLib.Keywords;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonHannaTono : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonHannaTonoCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonHannaTonoPendingCombatStartCard { get; set; }
    protected override int CounterValue { get => AstralParty_PersonHannaTonoCounter; set => AstralParty_PersonHannaTonoCounter = value; }
    protected override bool PendingCombatStartCard { get => AstralParty_PersonHannaTonoPendingCombatStartCard; set => AstralParty_PersonHannaTonoPendingCombatStartCard = value; }
    protected override int BaseMaxCounter => 4;
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillMagicLevitation>(),
        HoverTipFactory.FromPower<HannaNodePower>(),
        HoverTipFactory.FromPower<AstralLevitationPower>(),
        HoverTipFactory.FromPower<EffectsStarlightPower>(),
        ..new[] { AstralKeywords.AstralPassingThroughId }.ToHoverTips()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeDollMaking>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature != null)
        {
            PlayerChoiceContext context = Owner.Creature.CombatState != null && LocalContext.NetId.HasValue
                ? new HookPlayerChoiceContext(this, LocalContext.NetId.Value, Owner.Creature.CombatState, GameActionType.Combat)
                : new ThrowingPlayerChoiceContext();
            await PowerCmd.SetAmount<HannaNodePower>(context, Owner.Creature, 10m, Owner.Creature, null);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner) return;
        var node = (int)Owner.Creature.GetPowerAmount<HannaNodePower>();
        if (node < 6) return;
        await PowerCmd.Apply<EffectsStarlightPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
        if (Owner.GetRelic<PersonalityDerivativeDollMaking>()?.IsComplete == true)
            await TokenEternalStarlight.GrantStacks(Owner, 1);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null) return;
        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillMagicLevitation>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
