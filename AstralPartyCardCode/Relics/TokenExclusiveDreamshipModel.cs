using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveDreamshipModel : AstralPartyRelicModel
{
    private const int CombatStartDexterity = 2;
    private const int CardsPerCooldownAdvance = 15;

    [SavedProperty] public int AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDreamshipSeries)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat = 0;
        InvokeDisplayAmountChanged();

        Flash();
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, CombatStartDexterity, Owner.Creature, null, true);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Cards.CardPlay cardPlay)
    {
        if (Owner == null || cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;

        AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat++;
        var triggerCount = AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat / CardsPerCooldownAdvance;
        AstralParty_TokenExclusiveDreamshipModelCardsPlayedThisCombat %= CardsPerCooldownAdvance;
        InvokeDisplayAmountChanged();

        if (triggerCount <= 0)
            return Task.CompletedTask;

        Flash();
        PersonaRelicHelper.AdvanceCooldownRelics(Owner, triggerCount);
        return Task.CompletedTask;
    }
}