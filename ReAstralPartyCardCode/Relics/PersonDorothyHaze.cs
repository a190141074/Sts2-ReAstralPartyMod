using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonDorothyHaze : CooldownPersonaRelicBase
{
    public const int MaxWarmStacks = 5;
    private const int DorothyBaseMaxCounter = 3;
    private const decimal TurnStartHealAmount = 1m;
    private const decimal StolenStarLightAmount = 1m;
    private const decimal StolenGoldAmount = 1m;
    private const int DefaultNodeValue = 10;

    [SavedProperty] public int AstralParty_PersonDorothyHazeCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonDorothyHazePendingCombatStartCard { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonDorothyHazeCounter;
        set => AstralParty_PersonDorothyHazeCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonDorothyHazePendingCombatStartCard;
        set => AstralParty_PersonDorothyHazePendingCombatStartCard = value;
    }

    protected override int BaseMaxCounter => DorothyBaseMaxCounter;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTrueMe>(),
        HoverTipFactory.FromPower<WarmPower>(),
        HoverTipFactory.FromPower<DorothyNodePower>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        await PowerCmd.SetAmount<DorothyNodePower>(Owner.Creature, DefaultNodeValue, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var orderedPlayers = Owner.Creature.CombatState.Players
            .Where(entry => entry.Creature != null)
            .OrderBy(entry => entry.NetId)
            .ToList();
        if (orderedPlayers.Count == 0)
            return;

        var playerIndex = orderedPlayers.FindIndex(entry => entry.NetId == player.NetId);
        if (playerIndex < 0)
            return;

        var playerDigit = playerIndex % 10;
        var nodeDigit = GetCurrentNodeDigit();
        if (playerDigit != nodeDigit)
            return;

        var targetCreature = player.Creature;
        if (targetCreature == null || !targetCreature.IsAlive || !Owner.Creature.IsAlive)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, TurnStartHealAmount, true);
        if (targetCreature != Owner.Creature)
            await CreatureCmd.Heal(targetCreature, TurnStartHealAmount, true);

        if (player != Owner && player.Gold >= StolenGoldAmount)
            await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(StolenGoldAmount, player, GoldLossType.Spent);

        await PowerCmd.Apply<StarLightPower>(Owner.Creature, StolenStarLightAmount, Owner.Creature, null, false);
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTrueMe>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    internal static async Task TryGainWarmFromHeal(Player player, decimal amount)
    {
        if (player?.Creature == null || amount <= 0m)
            return;

        await DerivedHealResolutionHelper.EnqueueWarmAndFlush(player, 1, null, "dorothy_heal");
    }

    internal static async Task ApplyWarmFromDerivedSupport(Player player, int amount, CardModel? source)
    {
        if (player?.Creature == null || amount <= 0)
            return;

        var relic = player.GetRelic<PersonDorothyHaze>();
        if (relic == null)
            return;

        await relic.GainWarm(amount, source);
    }

    private async Task GainWarm(int amount, CardModel? source)
    {
        if (Owner?.Creature == null || amount <= 0)
            return;

        var newAmount = System.Math.Min(GetWarmAmount() + amount, MaxWarmStacks);
        await SetWarmAmount(newAmount, source);
    }

    private async Task SetWarmAmount(int amount, CardModel? source)
    {
        if (Owner?.Creature == null)
            return;

        var clampedAmount = System.Math.Clamp(amount, 0, MaxWarmStacks);
        var existingPower = Owner.Creature.GetPower<WarmPower>();
        if (clampedAmount <= 0)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);
            return;
        }

        await PowerCmd.SetAmount<WarmPower>(Owner.Creature, clampedAmount, Owner.Creature, source);
    }

    private int GetWarmAmount()
    {
        return Owner?.Creature == null
            ? 0
            : System.Math.Max((int)Owner.Creature.GetPowerAmount<WarmPower>(), 0);
    }

    private int GetCurrentNodeDigit()
    {
        var currentNode = Owner?.Creature == null
            ? 0
            : System.Math.Max((int)Owner.Creature.GetPowerAmount<DorothyNodePower>(), 0);
        return currentNode % 10;
    }
}
