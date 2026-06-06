using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Godot;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class EnigmaticOmenPowerBase : AstralPartyPowerModel
{
    private const string SharedOmenIconPath = "res://ReAstralPartyMod/images/powers/etherium_sword_recall_omen_power.png";
    private const string LegacySharedOmenIconPath = "res://ReAstralPartyMod/images/powers/yuzhao_power.png";

    [SavedProperty]
    public int AstralParty_OmenRemainingTurns { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => Math.Max(0, AstralParty_OmenRemainingTurns);

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ENIGMATIC_OMEN_POWER_BASE.title");

    protected override IEnumerable<IHoverTip> ExtraHoverTips => EffectHoverTips;

    public override LocString Description
    {
        get
        {
            var description = new LocString(
                "powers",
                "RE_ASTRAL_PARTY_MOD_POWER_ENIGMATIC_OMEN_POWER_BASE.description");
            description.Add("Turns", GetDisplayedTurnsForDescription());
            description.Add("Effect", new LocString("powers", EffectDescriptionLocKey));
            return description;
        }
    }

    protected abstract int DefaultTurns { get; }

    protected abstract string EffectDescriptionLocKey { get; }

    protected virtual IEnumerable<IHoverTip> EffectHoverTips => [];

    protected abstract Task OnTriggered(PlayerChoiceContext choiceContext, Player player);

    private int GetDisplayedTurnsForDescription()
    {
        return AstralParty_OmenRemainingTurns > 0
            ? AstralParty_OmenRemainingTurns
            : Math.Max(1, DefaultTurns);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (AstralParty_OmenRemainingTurns <= 0)
            AstralParty_OmenRemainingTurns = StableNumericStateHelper.ClampCeilingToInt(Amount, 1m, 999m);

        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    protected override string ResolveIconPath()
    {
        if (ResourceLoader.Exists(SharedOmenIconPath))
            return SharedOmenIconPath;
        if (ResourceLoader.Exists(LegacySharedOmenIconPath))
            return LegacySharedOmenIconPath;
        return base.ResolveIconPath();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player != player)
            return;

        if (AstralParty_OmenRemainingTurns <= 1)
        {
            await OnTriggered(choiceContext, player);
            await PowerCmd.Remove(this);
            return;
        }

        AstralParty_OmenRemainingTurns--;
        await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
        InvokeDisplayAmountChanged();
    }
}
