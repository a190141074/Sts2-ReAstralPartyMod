using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

[RegisterPotion(typeof(DisabledPotionPool))]
public class PersonChestChoose : AstralPartyPotionModel
{
    private const int RevealCount = 4;

    public PersonChestChoose() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.Self;

    public override bool PassesCustomUsabilityCheck =>
        Owner != null
        && CombatManager.Instance?.IsInProgress != true
        && GetAvailablePersonaRelics(Owner).Count > 0;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null)
            return;
        if (Owner.RunState is RunState runState)
            await ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);

        var availableRelics = GetAvailablePersonaRelics(Owner);
        if (availableRelics.Count == 0)
            return;

        // Build a stable snapshot before reserving the synced relic choice id.
        var relicOptions = PersonMultiplayerEffectHelper.CreateDeterministicRelicChoiceOptions(
                availableRelics,
                RevealCount,
                MainFile.ModId,
                Id.Entry,
                Owner.RunState.Rng.StringSeed,
                Owner.RunState.CurrentActIndex,
                Owner.NetId)
            .ToList();

        // This is an owned overlay screen, so it can be safely opened above the map and removed on close.
        var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectOverlayRelicForPlayer(
            Owner,
            relicOptions,
            new LocString("potions", $"{Id.Entry}.selectionScreenHeader").GetRawText(),
            new LocString("potions", $"{Id.Entry}.selectionScreenPrompt").GetRawText(),
            $"{Id.Entry}.use");
        if (selectedRelic == null)
            return;

        await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, selectedRelic);
    }

    private static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var bannedPersonaRelicIds = ReAstralPartyModSettingsManager.GetBannedPersonaRelicIds(owner.RunState);
        var available = PersonRelicRegistry.GetAvailablePersonaRelics(owner, bannedPersonaRelicIds);
        if (available.Count > 0)
            return available;

        MainFile.Logger.Warn("ban list filtered all persona options; falling back to the full persona pool.");
        return PersonRelicRegistry.GetAvailablePersonaRelics(owner);
    }
}
