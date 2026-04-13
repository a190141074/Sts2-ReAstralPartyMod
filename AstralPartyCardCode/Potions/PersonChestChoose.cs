using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Patches;
using AstralPartyMod.AstralPartyCardCode.Relics;
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
    public PersonChestChoose() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.Self;

    public override bool PassesCustomUsabilityCheck =>
        CombatManager.Instance?.IsInProgress != true && GetAvailablePersonaRelics().Count > 0;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var availableRelics = GetAvailablePersonaRelics();
        if (availableRelics.Count == 0)
            return;

        // Shuffle with the run RNG so the revealed relic set stays deterministic.
        var relicOptions = availableRelics.ToList();
        relicOptions.UnstableShuffle(Owner.RunState.Rng.Niche);
        relicOptions = relicOptions.Take(Math.Min(4, relicOptions.Count)).ToList();

        // Override the generic relic picker banner so the player sees a persona-specific title here.
        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("potions", $"{Id.Entry}.selectionScreenHeader").GetRawText());

        // Show canonical relics in the picker, then grant a mutable copy of the chosen relic.
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner, relicOptions);
        if (selectedRelic == null)
            return;

        await RelicCmd.Obtain(selectedRelic.ToMutable(), Owner);
    }

    private IReadOnlyList<RelicModel> GetAvailablePersonaRelics()
    {
        var ownedRelicIds = Owner.Relics
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

        return new List<RelicModel>
            {
                ModelDb.Relic<PersonWeirdEgg>(),
                ModelDb.Relic<PersonSamuraiPrawn>(),
                ModelDb.Relic<PersonSlimeLulu>(),
                ModelDb.Relic<PersonBionicJasmine>(),
                ModelDb.Relic<PersonProprietress>(),
                ModelDb.Relic<PersonMousyLian>(),
                ModelDb.Relic<PersonBlueWhale>(),
                ModelDb.Relic<PersonOasisQueen>(),
                ModelDb.Relic<PersonInkShadowHunter>(),
                ModelDb.Relic<PersonMascotGirlMimi>(),
            }
            .Where(relic => !ownedRelicIds.Contains(relic.Id))
            .ToList();
    }
}