using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.RestSite;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldInitialPoint : AstralPartyRelicModel
{
    private const int InitialRefreshPool = 3;
    private const int MaxAscensions = 3;
    private const int AscensionChoiceCount = 3;
    private const int FirstAscensionCost = 60;
    private const int SecondAscensionCost = 90;
    private const int ThirdAscensionCost = 120;
    private const decimal HealPercent = 0.15m;

    [SavedProperty] public int AstralParty_TokenGoldInitialPointAscensionCount { get; set; }
    [SavedProperty] public int AstralParty_TokenGoldInitialPointRemainingRerolls { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenGoldInitialPointAscensionCount;

    protected override string IconBasePath => "res://ReAstralPartyMod/images/relic/personality_land_center";

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldInitialPointAscensionCount = 0;
        AstralParty_TokenGoldInitialPointRemainingRerolls = InitialRefreshPool;
        InvokeDisplayAmountChanged();
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (Owner == null || player != Owner)
            return false;

        options.Add(new InitialPointRestSiteOption(player));
        return true;
    }

    public bool IsRestSiteOptionEnabled(Player player)
    {
        return Owner != null
               && player == Owner
               && (IsMaxedAscension() || player.Gold >= GetCurrentAscensionCost());
    }

    public LocString BuildRestSiteDescription(Player player)
    {
        if (Owner == null || player != Owner)
            return new LocString("rest_site_ui", $"OPTION_{InitialPointRestSiteOption.OptionKey}.description_stage1");

        return new LocString("rest_site_ui", GetDescriptionKey());
    }

    public async Task<bool> ResolveRestSiteOptionSelection(Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return false;

        Flash();

        if (!IsMaxedAscension())
        {
            var goldCost = GetCurrentAscensionCost();
            if (player.Gold < goldCost)
                return false;

            var rewardOptions = BuildAscensionRewardOptions(player);
            if (rewardOptions.Count == 0)
                return false;

            var selectionTitle = new LocString("relics", $"{Id.Entry}.selectionScreenHeader").GetRawText();
            var selectionResult = await DeterministicMultiplayerChoiceHelper.SelectRefreshableRelicForPlayer(
                player,
                rewardOptions,
                AstralParty_TokenGoldInitialPointRemainingRerolls,
                selectionTitle,
                "剩余刷新次数：",
                "筹码概率：按当前升星阶段",
                $"{Id.Entry}.ascension-choice",
                RerollAscensionRewardOptions,
                RebuildAscensionRewardOptionsFromHistory);
            if (selectionResult.SelectedRelic == null)
                return false;

            await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, player, GoldLossType.Spent);
            AstralParty_TokenGoldInitialPointRemainingRerolls = selectionResult.RemainingRerolls;
            await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(player, selectionResult.SelectedRelic);
            AstralParty_TokenGoldInitialPointAscensionCount =
                Math.Min(MaxAscensions, AstralParty_TokenGoldInitialPointAscensionCount + 1);
            InvokeDisplayAmountChanged();
            AstralParty_TokenGoldInitialPointRemainingRerolls++;
        }

        await HealPlayer(player);
        return true;
    }

    private bool IsMaxedAscension()
    {
        return AstralParty_TokenGoldInitialPointAscensionCount >= MaxAscensions;
    }

    private int GetCurrentAscensionCost()
    {
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => FirstAscensionCost,
            1 => SecondAscensionCost,
            2 => ThirdAscensionCost,
            _ => 0
        };
    }

    private string GetDescriptionKey()
    {
        var optionLocKey = $"OPTION_{InitialPointRestSiteOption.OptionKey}";
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => $"{optionLocKey}.description_stage1",
            1 => $"{optionLocKey}.description_stage2",
            2 => $"{optionLocKey}.description_stage3",
            _ => $"{optionLocKey}.description_maxed"
        };
    }

    private async Task HealPlayer(Player player)
    {
        if (player.Creature == null)
            return;

        var healAmount = Math.Max(1m, Math.Ceiling(player.Creature.MaxHp * HealPercent));
        await CreatureCmd.Heal(player.Creature, healAmount, true);
    }

    private List<RelicModel> BuildAscensionRewardOptions(Player player)
    {
        if (Owner == null || player != Owner)
            return [];

        return TokenRewardRerollHelper.BuildInitialOptions(
            player,
            AscensionChoiceCount,
            RollAscensionRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private IReadOnlyList<RelicModel> RerollAscensionRewardOptions(
        IReadOnlyList<RelicModel> currentOptions,
        int rerollOrdinal,
        IReadOnlySet<ModelId> historicalIds)
    {
        return TokenRewardRerollHelper.RerollOptions(
            Owner!,
            currentOptions,
            rerollOrdinal + 1,
            historicalIds,
            RollAscensionRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private IReadOnlyList<RelicModel> RebuildAscensionRewardOptionsFromHistory(IReadOnlyList<int> rerollHistory)
    {
        return TokenRewardRerollHelper.RebuildOptionsFromHistory(
            Owner!,
            AscensionChoiceCount,
            rerollHistory,
            RollAscensionRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private RelicRarity RollAscensionRewardRarity(Player owner, int slotIndex, int rerollOrdinal)
    {
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            MainFile.ModId,
            Id.Entry,
            "initial_point_rarity",
            owner.RunState.Rng.StringSeed,
            owner.RunState.CurrentActIndex,
            owner.NetId,
            AstralParty_TokenGoldInitialPointAscensionCount,
            slotIndex,
            rerollOrdinal);
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => roll < 65 ? RelicRarity.Common : roll < 95 ? RelicRarity.Uncommon : RelicRarity.Rare,
            1 => roll < 45 ? RelicRarity.Common : roll < 85 ? RelicRarity.Uncommon : RelicRarity.Rare,
            2 => roll < 25 ? RelicRarity.Common : roll < 75 ? RelicRarity.Uncommon : RelicRarity.Rare,
            _ => RelicRarity.Rare
        };
    }

    private List<RelicModel> GetCandidates(Player player, RelicRarity rarity, bool excludeOwned,
        IReadOnlySet<ModelId> selectedIds)
    {
        return TokenRelicRegistry.GetAvailableNonDiceTokenRelicsByRarity(player.RunState, rarity)
            .Where(relic => !selectedIds.Contains(TokenRewardSelectionHelper.GetCanonicalId(relic)))
            .Where(relic => IsValidRewardCandidate(player, relic, excludeOwned))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    private bool IsValidRewardCandidate(Player player, RelicModel relic, bool excludeOwned)
    {
        if (Owner == null || player != Owner)
            return false;
        if (relic.Rarity is not (RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare))
            return false;
        if (relic.CanonicalInstance.Id == CanonicalInstance.Id)
            return false;

        return !excludeOwned || player.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id);
    }
}
