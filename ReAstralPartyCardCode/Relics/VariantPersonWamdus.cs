using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonWamdus : CooldownPersonRelicBase, IRelicExtraIconAmountLabelsProvider,
    IRelicExtraIconAmountLabelsChangeSource
{
    [SavedProperty] public int AstralParty_VariantPersonWamdusCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonWamdusPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonWamdusManaAmplification { get; set; }

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    protected override int CounterValue
    {
        get => AstralParty_VariantPersonWamdusCounter;
        set => AstralParty_VariantPersonWamdusCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonWamdusPendingCombatStartCard;
        set => AstralParty_VariantPersonWamdusPendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSixDragonsAzure>(),
        HoverTipFactory.FromCard<SkillTokenTranscendDimensions>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>()
    ];

    public override Task BeforeCombatStart()
    {
        SetManaAmplification(0);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Skill)
            return;

        var gained = AttackCardCostHelper.GetPlayedCost(cardPlay);
        if (gained <= 0)
            return;

        AddManaAmplification(gained);
        await Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        await base.AfterCombatEnd(room);

        if (Owner?.Creature == null)
        {
            SetManaAmplification(0);
            return;
        }

        var manaAmplification = GetManaAmplification();
        var maxHpGain = manaAmplification / 10;
        if (maxHpGain > 0)
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);

        SetManaAmplification(0);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSixDragonsAzure>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots()
    {
        if (GetManaAmplification() <= 0)
            return [];

        return
        [
            ExtraIconAmountLabelSlot.At(ExtraIconAmountLabelCorner.TopRight, GetManaAmplification().ToString())
        ];
    }

    internal int GetManaAmplification()
    {
        return Math.Max(0, AstralParty_VariantPersonWamdusManaAmplification);
    }

    internal void AddManaAmplification(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_VariantPersonWamdusManaAmplification = Math.Max(0, AstralParty_VariantPersonWamdusManaAmplification) + amount;
        NotifyDisplayChanged();
    }

    private void SetManaAmplification(int amount)
    {
        AstralParty_VariantPersonWamdusManaAmplification = Math.Max(0, amount);
        NotifyDisplayChanged();
    }

    private void NotifyDisplayChanged()
    {
        InvokeDisplayAmountChanged();
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
    }
}
