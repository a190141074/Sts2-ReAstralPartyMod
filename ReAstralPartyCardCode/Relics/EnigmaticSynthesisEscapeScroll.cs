using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticSynthesisEscapeScroll : AstralPartyRelicModel
{
    private const int CooldownRounds = 40;
    private const decimal BaseMaxHpLossPercent = 0.14m;
    private const decimal BaseFlatMaxHpLoss = 14m;
    private const decimal SevenCursesMaxHpLossPercent = 0.07m;
    private const decimal SevenCursesFlatMaxHpLoss = 7m;
    private const int MissingSnapshotFallbackHp = 20;

    // Store the snapshot as an integer HP value so persistence and reward sync never depend on decimal SavedProperty support.
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEscapeScrollLastRestSiteHpSnapshot { get; set; } = MissingSnapshotFallbackHp;

    [SavedProperty] public int AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress { get; set; }
    [SavedProperty] public bool AstralParty_EnigmaticSynthesisEscapeScrollReady { get; set; } = true;

    protected override string RelicId => "enigmatic_synthesis_escape_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_EnigmaticSynthesisEscapeScrollReady
        ? 0
        : Math.Clamp(CooldownRounds - AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress, 0, CooldownRounds);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress = 0;
        AstralParty_EnigmaticSynthesisEscapeScrollReady = true;
        CaptureCurrentSnapshotIfAvailable();
        InvokeDisplayAmountChanged();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType == RoomType.RestSite)
            CaptureCurrentSnapshotIfAvailable();

        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || AstralParty_EnigmaticSynthesisEscapeScrollReady)
            return Task.CompletedTask;

        AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress++;
        if (AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress >= CooldownRounds)
        {
            AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress = 0;
            AstralParty_EnigmaticSynthesisEscapeScrollReady = true;
            Flash();
        }

        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner?.Creature)
            return true;

        return !AstralParty_EnigmaticSynthesisEscapeScrollReady;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner?.Creature || !AstralParty_EnigmaticSynthesisEscapeScrollReady)
            return;

        AstralParty_EnigmaticSynthesisEscapeScrollReady = false;
        AstralParty_EnigmaticSynthesisEscapeScrollCooldownProgress = 0;
        InvokeDisplayAmountChanged();
        Flash();

        using var protectionScope = EscapeScrollDeathProtectionHelper.Enter();

        var restoreHp = ResolveRestoreHp(creature);
        var maxHpLoss = ResolveMaxHpLoss(creature);

        MainFile.Logger.Info(
            $"[EnigmaticSynthesisEscapeScroll] Prevented death | owner={Owner?.NetId} | restoreHp={restoreHp} | maxHpLoss={maxHpLoss} | hadSevenCurses={Owner?.GetRelic<EnigmaticSevenCurses>() != null}");

        if (maxHpLoss > 0m)
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, maxHpLoss, false);

        var targetRestoreHp = Math.Clamp(restoreHp, 1m, creature.MaxHp);
        if (targetRestoreHp > creature.CurrentHp)
            await CreatureCmd.Heal(creature, targetRestoreHp - creature.CurrentHp, false);
        else if (targetRestoreHp < creature.CurrentHp)
            await CreatureCmd.SetCurrentHp(creature, targetRestoreHp);
    }

    private void CaptureCurrentSnapshotIfAvailable()
    {
        if (Owner?.Creature == null)
            return;

        AstralParty_EnigmaticSynthesisEscapeScrollLastRestSiteHpSnapshot = StableNumericStateHelper.ClampCeilingToInt(
            Owner.Creature.CurrentHp,
            1m,
            Owner.Creature.MaxHp);
    }

    private decimal ResolveRestoreHp(Creature creature)
    {
        var snapshot = AstralParty_EnigmaticSynthesisEscapeScrollLastRestSiteHpSnapshot;
        if (snapshot <= 0m)
            return Math.Clamp(MissingSnapshotFallbackHp, 1m, creature.MaxHp);

        return Math.Clamp(snapshot, 1m, creature.MaxHp);
    }

    private decimal ResolveMaxHpLoss(Creature creature)
    {
        var hasSevenCurses = Owner?.GetRelic<EnigmaticSevenCurses>() != null;
        var percentLoss = hasSevenCurses ? SevenCursesMaxHpLossPercent : BaseMaxHpLossPercent;
        var flatLoss = hasSevenCurses ? SevenCursesFlatMaxHpLoss : BaseFlatMaxHpLoss;
        return Math.Max(1m, Math.Ceiling(creature.MaxHp * percentLoss) + flatLoss);
    }
}
