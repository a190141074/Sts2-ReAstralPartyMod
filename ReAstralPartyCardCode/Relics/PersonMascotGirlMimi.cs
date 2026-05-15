using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonMascotGirlMimi : CooldownPersonaRelicBase
{
    private const int DrawsPerTokenMemoryChoice = 25;
    private const int PreferredBankCardWeight = 3;

    [SavedProperty] public int AstralParty_PersonMascotGirlMimiCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonMascotGirlMimiPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonMascotGirlMimiCounter;
        set => AstralParty_PersonMascotGirlMimiCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonMascotGirlMimiPendingCombatStartCard;
        set => AstralParty_PersonMascotGirlMimiPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillProductRestocking>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress = 0;
        await EnsureTokenMemoryRelic();
    }

    public async Task HandleProductRestockingDraw(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        int cardsDrawn)
    {
        if (Owner?.Creature?.CombatState == null || cardsDrawn <= 0)
            return;

        var memoryRelic = await EnsureTokenMemoryRelic();
        if (memoryRelic == null)
            return;

        AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress += cardsDrawn;
        memoryRelic.RefreshProgressDisplay();

        while (AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress >= DrawsPerTokenMemoryChoice)
        {
            var granted = await TryGrantTokenAbilityChoice(choiceContext, sourceCard, memoryRelic);
            if (!granted)
                break;

            AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress -= DrawsPerTokenMemoryChoice;
            memoryRelic.RefreshProgressDisplay();
        }
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillProductRestocking>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private async Task<PersonalityDerivativeMascotGirlMimiTokenMemory?> EnsureTokenMemoryRelic()
    {
        if (Owner == null)
            return null;

        var memoryRelic = MascotGirlMimiTokenMemoryHelper.GetMemoryRelic(Owner);
        if (memoryRelic != null)
            return memoryRelic;

        await PersonaMultiplayerEffectHelper
            .ObtainDerivativeRelicIfMissing<PersonalityDerivativeMascotGirlMimiTokenMemory>(
                Owner);

        return MascotGirlMimiTokenMemoryHelper.GetMemoryRelic(Owner);
    }

    private async Task<bool> TryGrantTokenAbilityChoice(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        PersonalityDerivativeMascotGirlMimiTokenMemory memoryRelic)
    {
        if (Owner?.Creature == null)
            return false;

        var availableTokenRelics = MascotGirlMimiTokenMemoryHelper
            .GetBridgeableUnownedTokenRelics(Owner, Owner.Creature, false)
            .ToList();

        if (availableTokenRelics.Count == 0)
            return false;

        var selectionOptions = PersonaMultiplayerEffectHelper.CreateWeightedDeterministicRelicChoiceOptions(
            availableTokenRelics,
            3,
            relic => TokenRelicRegistry.IsBankCardTokenRelic(relic) ? PreferredBankCardWeight : 1,
            MainFile.ModId,
            Id.Entry,
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.NetId,
            AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress,
            sourceCard.Id.Entry);

        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("relics", $"{memoryRelic.Id.Entry}.selectionScreenHeader").GetRawText());

        var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            Owner,
            selectionOptions,
            $"{Id.Entry}.token-memory");
        if (selectedRelic == null)
            return false;

        var bridgePower = await TokenRelicBridgeHelper.ApplyTokenRelicPower(
            Owner.Creature,
            selectedRelic.Id,
            Owner.Creature,
            sourceCard,
            false,
            TokenRelicBridgeInitializationMode.RunAfterObtainedSkipOneTimeRewards);

        if (bridgePower == null)
            return false;

        Flash();
        memoryRelic.Flash();
        memoryRelic.RecordTemporaryTokenGain(selectedRelic.Id);
        return true;
    }
}
