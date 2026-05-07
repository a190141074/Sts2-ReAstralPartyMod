using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
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

        var availableRelics = GetAvailablePersonaRelics(Owner);
        if (availableRelics.Count == 0)
            return;

        // Build a stable snapshot before reserving the synced relic choice id.
        var relicOptions = PersonaMultiplayerEffectHelper.CreateDeterministicRelicChoiceOptions(
                availableRelics,
                RevealCount,
                MainFile.ModId,
                Id.Entry,
                Owner.RunState.Rng.StringSeed,
                Owner.RunState.CurrentActIndex,
                Owner.NetId)
            .ToList();

        // Override the generic relic picker banner so the player sees a persona-specific title here.
        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("potions", $"{Id.Entry}.selectionScreenHeader").GetRawText());

        // Show canonical relics in the picker, then grant a mutable copy of the chosen relic.
        var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            Owner,
            relicOptions,
            $"{Id.Entry}.use");
        if (selectedRelic == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, selectedRelic);
    }

    private static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return PersonaRelicRegistry.GetAvailablePersonaRelics(owner);
    }
}
