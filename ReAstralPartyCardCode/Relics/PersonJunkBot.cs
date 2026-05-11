using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonJunkBot : CooldownPersonaRelicBase
{
    private const int MarkedCombatCount = 16;
    private const int KillsPerWeaponFrameStack = 2;
    private const string JunkBotMapSalt = "PersonJunkBot";

    [SavedProperty] public int AstralParty_PersonJunkBotCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonJunkBotPendingCombatStartCard { get; set; }
    [SavedProperty] public int AstralParty_PersonJunkBotMarkedActIndex { get; set; } = -1;
    [SavedProperty] public bool AstralParty_PersonJunkBotMarkedCoordsSet { get; set; }
    [SavedProperty] public int AstralParty_PersonJunkBotMarkedKillProgress { get; set; }

    private int[] _markedCoordCols = [];
    private int[] _markedCoordRows = [];

    [SavedProperty]
    private string AstralParty_PersonJunkBotMarkedCoordColsJson
    {
        get => JsonSerializer.Serialize(_markedCoordCols);
        set => _markedCoordCols = DeserializeIntArray(value);
    }

    [SavedProperty]
    private string AstralParty_PersonJunkBotMarkedCoordRowsJson
    {
        get => JsonSerializer.Serialize(_markedCoordRows);
        set => _markedCoordRows = DeserializeIntArray(value);
    }

    public int[] AstralParty_PersonJunkBotMarkedCoordCols
    {
        get => _markedCoordCols;
        set => _markedCoordCols = value ?? [];
    }

    public int[] AstralParty_PersonJunkBotMarkedCoordRows
    {
        get => _markedCoordRows;
        set => _markedCoordRows = value ?? [];
    }

    protected override int CounterValue
    {
        get => AstralParty_PersonJunkBotCounter;
        set => AstralParty_PersonJunkBotCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonJunkBotPendingCombatStartCard;
        set => AstralParty_PersonJunkBotPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillComeHereYou>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeZ3000WeaponFrame>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await EnsureWeaponFrameRelic();
        InitializeMarkedCombatsForCurrentAct(forceRefresh: true);
    }

    public override async Task AfterActEntered()
    {
        await base.AfterActEntered();
        InitializeMarkedCombatsForCurrentAct(forceRefresh: true);
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        return AddMarkedRooms(map);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (dealer != Owner.Creature)
            return Task.CompletedTask;
        if (target.Side == Owner.Creature.Side)
            return Task.CompletedTask;
        if (!result.WasTargetKilled)
            return Task.CompletedTask;
        if (!IsCurrentCombatMarked())
            return Task.CompletedTask;

        Flash();
        ReduceCooldownProgress(2);
        MarkCurrentPointQuestCompleted();

        AstralParty_PersonJunkBotMarkedKillProgress++;
        if (AstralParty_PersonJunkBotMarkedKillProgress < KillsPerWeaponFrameStack)
            return Task.CompletedTask;

        AstralParty_PersonJunkBotMarkedKillProgress %= KillsPerWeaponFrameStack;
        if (Owner.GetRelic<PersonalityDerivativeZ3000WeaponFrame>() is { } weaponFrame)
            weaponFrame.AddStacks(1);

        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillComeHereYou>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    private async Task EnsureWeaponFrameRelic()
    {
        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeZ3000WeaponFrame>(Owner);
    }

    private void InitializeMarkedCombatsForCurrentAct(bool forceRefresh = false)
    {
        var runState = Owner?.RunState;
        if (runState == null)
            return;
        if (!forceRefresh
            && AstralParty_PersonJunkBotMarkedCoordsSet
            && AstralParty_PersonJunkBotMarkedActIndex == runState.CurrentActIndex)
            return;

        AstralParty_PersonJunkBotMarkedActIndex = runState.CurrentActIndex;
        var map = runState.Map;
        if (map == null)
            return;

        var candidates = map
            .GetAllMapPoints()
            .Where(static p =>
            {
                var pointType = p.PointType;
                return pointType is MapPointType.Monster or MapPointType.Elite;
            })
            .Where(p => !p.Quests.Any(q => q is PersonJunkBot))
            .ToList();

        var rng = new Rng((uint)((int)runState.Rng.Seed + (int)Owner!.NetId + StringHelper.GetDeterministicHashCode(JunkBotMapSalt)));
        candidates.UnstableShuffle(rng);

        var selected = candidates.Take(System.Math.Min(MarkedCombatCount, candidates.Count)).ToList();
        AstralParty_PersonJunkBotMarkedCoordCols = selected.Select(p => p.coord.col).ToArray();
        AstralParty_PersonJunkBotMarkedCoordRows = selected.Select(p => p.coord.row).ToArray();
        AstralParty_PersonJunkBotMarkedCoordsSet = true;
    }

    private ActMap AddMarkedRooms(ActMap? map)
    {
        if (map == null || Owner?.RunState == null)
            return map!;
        if (Owner.RunState.CurrentActIndex != AstralParty_PersonJunkBotMarkedActIndex)
            return map;

        var markedCoords = GetMarkedCoords();
        if (markedCoords == null)
            return map;

        var invalidCoords = markedCoords.Any(coord =>
            !map.HasPoint(coord)
            || map.GetPoint(coord).PointType is not (MapPointType.Monster or MapPointType.Elite));
        if (invalidCoords)
        {
            InitializeMarkedCombatsForCurrentAct(forceRefresh: true);
            markedCoords = GetMarkedCoords() ?? [];
        }

        foreach (var coord in markedCoords)
        {
            var point = map.GetPoint(coord);
            if (point != null && !point.Quests.Any(q => q is PersonJunkBot))
                point.AddQuest(this);
        }

        return map;
    }

    private bool IsCurrentCombatMarked()
    {
        var runState = Owner?.RunState;
        var currentCoord = runState?.CurrentMapPoint?.coord;
        if (currentCoord == null)
            return false;

        return GetMarkedCoords()?.Contains(currentCoord.Value) == true;
    }

    private List<MapCoord>? GetMarkedCoords()
    {
        if (!AstralParty_PersonJunkBotMarkedCoordsSet)
            return null;

        var result = new List<MapCoord>(AstralParty_PersonJunkBotMarkedCoordCols.Length);
        for (var i = 0; i < AstralParty_PersonJunkBotMarkedCoordCols.Length; i++)
        {
            result.Add(new MapCoord
            {
                col = AstralParty_PersonJunkBotMarkedCoordCols[i],
                row = AstralParty_PersonJunkBotMarkedCoordRows[i]
            });
        }

        return result;
    }

    private void MarkCurrentPointQuestCompleted()
    {
        if (Owner?.RunState?.CurrentMapPointHistoryEntry == null)
            return;

        var playerEntry = Owner.RunState.CurrentMapPointHistoryEntry.GetEntry(Owner.NetId);
        if (playerEntry.CompletedQuests.Contains(Id))
            return;

        playerEntry.CompletedQuests.Add(Id);
    }

    private static int[] DeserializeIntArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return JsonSerializer.Deserialize<int[]>(value) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
