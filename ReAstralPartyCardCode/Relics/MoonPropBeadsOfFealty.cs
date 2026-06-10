using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropBeadsOfFealty : AstralPartyRelicModel
{
    private const string EliteRollContextId = "moon_prop_beads_of_fealty_elite";
    private const string RewardRollContextId = "moon_prop_beads_of_fealty_reward";

    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect { get; set; }
    [SavedProperty] public bool AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMoonPropId),
        ..HoverTipFactory.FromCardWithCardHoverTips<Greed>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null)
            return;

        AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect = true;
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = false;
        await CardPileCmd.AddCurseToDeck<Greed>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (!AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect
            || AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat
            || Owner?.Creature?.CombatState is not { } combatState
            || Owner.RunState is not { } runState
            || Owner.RunState?.CurrentRoom is not CombatRoom)
            return;

        var eliteCandidates = GetEliteEncounterCandidates(Owner).ToList();
        if (eliteCandidates.Count == 0)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] MoonPropBeadsOfFealty could not find any elite encounter candidates for player {Owner.NetId}.");
            return;
        }

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            eliteCandidates.Count,
            MainFile.ModId,
            RelicId,
            EliteRollContextId,
            Owner.RunState?.Rng.StringSeed ?? "<null_seed>",
            Owner.RunState?.CurrentActIndex ?? -1,
            Owner.RunState?.TotalFloor ?? -1,
            Owner.NetId,
            combatState.Encounter?.Id.Entry ?? "<null_encounter>");
        var selectedEncounter = eliteCandidates[selectedIndex].ToMutable();
        selectedEncounter.GenerateMonstersWithSlots(runState);

        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = true;
        Flash();
        await AddEliteEncounterGroup(combatState, selectedEncounter);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat || Owner == null)
            return;

        room.AddExtraReward(Owner, new GoldReward(700, Owner, false));

        var excludedRelicIds = new[] { ModelDb.Relic<MoonPropBeadsOfFealty>().Id };
        foreach (var otherPlayer in Owner.RunState.Players
                     .Where(player => player != Owner)
                     .OrderBy(static player => player.NetId))
        {
            var rewardRelic = MoonPropShopExtraRelicsHelper.CreateDeterministicMoonPropRelicExcluding(
                otherPlayer,
                RewardRollContextId,
                excludedRelicIds,
                Owner.NetId,
                otherPlayer.NetId,
                room.CombatState?.Encounter?.Id.Entry ?? "<null_encounter>");
            room.AddExtraReward(otherPlayer, new RelicReward(rewardRelic, otherPlayer));
        }

        AstralParty_MoonPropBeadsOfFealtyPendingNextCombatEffect = false;
        AstralParty_MoonPropBeadsOfFealtyTriggeredThisCombat = false;
        await RelicCmd.Remove(this);
    }

    private static IEnumerable<EncounterModel> GetEliteEncounterCandidates(Player owner)
    {
        return owner.RunState?.Act?.AllEliteEncounters
               ?? [];
    }

    private static async Task AddEliteEncounterGroup(CombatState combatState, EncounterModel encounter)
    {
        var addedMonsters = new List<MonsterModel>();
        foreach (var (monster, _) in encounter.MonstersWithSlots)
        {
            var slot = GetNextAvailableSlot(combatState);
            var creature = combatState.CreateCreature(monster, CombatSide.Enemy, slot);
            combatState.AddCreature(creature);
            CombatManager.Instance.AddCreature(creature);
            NCombatRoom.Instance?.AddCreature(creature);
            await creature.AfterAddedToRoom();
            await Hook.AfterCreatureAddedToCombat(combatState, creature);
            addedMonsters.Add(creature.Monster);
        }

        foreach (var monster in addedMonsters)
        {
            await monster.BeforeCombatStart();
            monster.InvokeExecutionFinished();
        }

        foreach (var monster in addedMonsters)
        {
            await monster.BeforeCombatStartLate();
            monster.InvokeExecutionFinished();
        }
    }

    private static string? GetNextAvailableSlot(CombatState combatState)
    {
        var slot = combatState.Encounter?.GetNextSlot(combatState);
        return string.IsNullOrWhiteSpace(slot) ? null : slot;
    }
}
