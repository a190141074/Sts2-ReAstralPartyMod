using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonProprietressDoppelganger : CooldownPersonRelicBase
{
    private const int GoldPerDamageBonusPercent = 30;
    private const int MaxDamageBonusPercent = 200;
    private const int EternalStarlightPerAct = 10;
    private const int MaximumEternalStarlightActs = 3;

    [SavedProperty] public int AstralParty_PersonProprietressDoppelgangerCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonProprietressDoppelgangerSpentGoldForDamage { get; set; }
    [SavedProperty] public int AstralParty_PersonProprietressDoppelgangerLastEternalStarlightActIndex { get; set; } = -1;

    protected override int CounterValue
    {
        get => AstralParty_PersonProprietressDoppelgangerCounter;
        set => AstralParty_PersonProprietressDoppelgangerCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard;
        set => AstralParty_PersonProprietressDoppelgangerPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetDamageBonusPercent();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillRemoteStore>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonProprietressDoppelgangerSpentGoldForDamage = 0;
        AstralParty_PersonProprietressDoppelgangerLastEternalStarlightActIndex = -1;
        InvokeDisplayAmountChanged();

        await TryGrantEternalStarlightForCurrentAct();
    }

    public override async Task AfterActEntered()
    {
        await base.AfterActEntered();
        await TryGrantEternalStarlightForCurrentAct();
    }

    public void RecordMerchantSpend(int spentGold)
    {
        if (spentGold <= 0 || GetDamageBonusPercent() >= MaxDamageBonusPercent)
            return;

        var maxTrackedGold = MaxDamageBonusPercent * GoldPerDamageBonusPercent;
        AstralParty_PersonProprietressDoppelgangerSpentGoldForDamage = Math.Min(
            maxTrackedGold,
            Math.Max(0, AstralParty_PersonProprietressDoppelgangerSpentGoldForDamage) + spentGold);
        InvokeDisplayAmountChanged();
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side || amount <= 0m)
            return 0m;

        return amount * GetDamageBonusPercent() / 100m;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillRemoteStore>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private int GetDamageBonusPercent()
    {
        return Math.Clamp(
            AstralParty_PersonProprietressDoppelgangerSpentGoldForDamage / GoldPerDamageBonusPercent,
            0,
            MaxDamageBonusPercent);
    }

    private async Task TryGrantEternalStarlightForCurrentAct()
    {
        var owner = Owner;
        var actIndex = owner?.RunState?.CurrentActIndex ?? -1;
        if (owner == null
            || actIndex < 0
            || actIndex >= MaximumEternalStarlightActs
            || actIndex <= AstralParty_PersonProprietressDoppelgangerLastEternalStarlightActIndex)
            return;

        AstralParty_PersonProprietressDoppelgangerLastEternalStarlightActIndex = actIndex;
        Flash();
        await TokenEternalStarlight.GrantStacks(
            owner,
            EternalStarlightPerAct,
            bypassProprietressDoppelgangerPenalty: true);
    }
}
