using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Patches;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace AstralPartyMod.AstralPartyCardCode.Potions;

[Pool(typeof(SharedPotionPool))]
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

        // Shuffle with the run RNG so the revealed relic set stays deterministic.
        var relicOptions = availableRelics.ToList();
        relicOptions.UnstableShuffle(Owner.RunState.Rng.Niche);
        relicOptions = relicOptions.Take(Math.Min(RevealCount, relicOptions.Count)).ToList();

        // Override the generic relic picker banner so the player sees a persona-specific title here.
        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("potions", $"{Id.Entry}.selectionScreenHeader").GetRawText());

        // Show canonical relics in the picker, then grant a mutable copy of the chosen relic.
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner, relicOptions);
        if (selectedRelic == null)
            return;

        await RelicCmd.Obtain(selectedRelic.ToMutable(), Owner);
    }

    private static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return PersonaRelicRegistry.GetAvailablePersonaRelics(owner);
    }
}
