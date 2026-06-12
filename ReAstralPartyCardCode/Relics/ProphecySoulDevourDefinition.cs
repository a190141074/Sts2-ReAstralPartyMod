using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

internal enum ProphecySoulDevourKind
{
    None = 0,
    UndergroundTrade = 1,
    TreasureHunting = 2,
    MimicLarva = 3,
    Ascension = 4,
    GoldMiner = 5,
    HumanWaveTactics = 6,
    EventStop = 7,
    EnergyConversion = 8,
    FastFluctuation = 9,
    PhaseReaction = 10,
    DivineMiracle = 11,
    MatrixRecycling = 12,
    HiddenStrikeCard = 13,
    HiddenStrikeRelic = 14,
    AncientRuins = 15,
    MineralRecovery = 16,
    EnergySavingStrategy = 17
}

internal enum ProphecySoulDevourPersistence
{
    OneShot = 0,
    Permanent = 1
}

internal sealed record ProphecySoulDevourDefinition(
    ProphecySoulDevourKind Kind,
    ProphecySoulDevourPersistence Persistence,
    bool AllowRepeatPermanent,
    string TitleLocKey,
    string DescriptionLocKey)
{
    public LocString TitleLocString => new("relics", TitleLocKey);
    public LocString DescriptionLocString => new("relics", DescriptionLocKey);
};

internal sealed record ProphecySoulDevourDelayedCardGrant(
    ModelId CardId,
    int UpgradeLevel,
    int RemainingNodes);

internal sealed record ProphecySoulDevourDelayedRelicGrant(
    ModelId RelicId,
    int RemainingNodes);

internal sealed record ProphecySoulDevourMimicSnapshot(
    ModelId CardId,
    int UpgradeLevel);
