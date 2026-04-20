using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Godot;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonalityDerivativeProprietressWealthism : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeProprietressWealthismCounter { get; set; }

    [SavedProperty] public int AstralParty_PersonalityDerivativeProprietressWealthismTotalSpent { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeProprietressWealthismTotalEarned { get; set; }

    [SavedProperty]
    public bool AstralParty_PersonalityDerivativeProprietressWealthismPaysOnNextRoom { get; set; } =
        true;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTransfer>(),
        BuildStatsHoverTip()
    ];

    public override int DisplayAmount => AstralParty_PersonalityDerivativeProprietressWealthismCounter;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeProprietressWealthismCounter = 0;
        AstralParty_PersonalityDerivativeProprietressWealthismTotalSpent = 0;
        AstralParty_PersonalityDerivativeProprietressWealthismTotalEarned = 0;
        AstralParty_PersonalityDerivativeProprietressWealthismPaysOnNextRoom = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null)
            return;

        if (AstralParty_PersonalityDerivativeProprietressWealthismPaysOnNextRoom)
        {
            var goldToGain = AstralParty_PersonalityDerivativeProprietressWealthismCounter;
            RecordIncome(goldToGain);
            Flash();
            await PlayerCmd.GainGold(goldToGain, Owner);
        }

        // This relic pays out on the 1st, 3rd, 5th... room entries after it is obtained.
        AstralParty_PersonalityDerivativeProprietressWealthismPaysOnNextRoom =
            !AstralParty_PersonalityDerivativeProprietressWealthismPaysOnNextRoom;
        InvokeDisplayAmountChanged();
    }

    public void IncreaseWealthCounter(int amount)
    {
        if (amount <= 0)
            return;

        // Wealth has no cap and only grows from explicit card effects such as Transfer.
        AstralParty_PersonalityDerivativeProprietressWealthismCounter += amount;
        Flash();
        InvokeDisplayAmountChanged();
    }

    public void RecordTransferSpend(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonalityDerivativeProprietressWealthismTotalSpent += amount;
        InvokeDisplayAmountChanged();
    }

    private void RecordIncome(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonalityDerivativeProprietressWealthismTotalEarned += amount;
        InvokeDisplayAmountChanged();
    }

    private HoverTip BuildStatsHoverTip()
    {
        var title = new LocString("relics", "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_PROPRIETRESS_WEALTHISM.stats_title");
        var bodyTemplate = new LocString("relics",
                "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_PROPRIETRESS_WEALTHISM.stats_description")
            .GetRawText();
        var body = string.Format(
            bodyTemplate,
            AstralParty_PersonalityDerivativeProprietressWealthismTotalEarned,
            AstralParty_PersonalityDerivativeProprietressWealthismTotalSpent);
        return new HoverTip(title, body, GD.Load<Texture2D>(PackedIconPath));
    }
}
