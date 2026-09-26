using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VariantPersonShadowScion : CooldownPersonRelicBase
{
    private const int EternalStarlightThreshold = 13;

    [SavedProperty] public int AstralParty_VariantPersonShadowScionCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_VariantPersonShadowScionPendingCard { get; set; }
    [SavedProperty] public int AstralParty_VariantPersonShadowScionPendingTransferredStarLight { get; set; }

    protected override string RelicId => "variant_person_shadow_scion";
    protected override int CounterValue
    {
        get => AstralParty_VariantPersonShadowScionCounter;
        set => AstralParty_VariantPersonShadowScionCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_VariantPersonShadowScionPendingCard;
        set => AstralParty_VariantPersonShadowScionPendingCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillPowerfulPityPlus>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeFallenHeavenlyKing>(),
        HoverTipFactory.FromPower<EffectsStarlightPower>(),
        TokenEternalStarlight.BuildReferenceHoverTip()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeFallenHeavenlyKing>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        var pendingTransferredStarLight = AstralParty_VariantPersonShadowScionPendingTransferredStarLight;
        if (pendingTransferredStarLight > 0)
        {
            AstralParty_VariantPersonShadowScionPendingTransferredStarLight = 0;
            Flash();
            await PowerCmd.Apply<EffectsStarlightPower>(Owner.Creature, pendingTransferredStarLight, Owner.Creature, null, false);
            MainFile.Logger.Info(
                $"[VariantPersonShadowScion] granted deferred transferred Starlight | owner={Owner.NetId} | amount={pendingTransferredStarLight}.");
        }

        var starlight = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        var bonus = starlight / EternalStarlightThreshold;
        if (bonus <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, bonus, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, bonus, Owner.Creature, null);
    }

    internal void AccumulatePendingTransferredStarlight(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_VariantPersonShadowScionPendingTransferredStarLight += amount;
        MainFile.Logger.Info(
            $"[VariantPersonShadowScion] queued transferred Starlight | owner={Owner?.NetId.ToString() ?? "<null>"} | amount={amount} | pending={AstralParty_VariantPersonShadowScionPendingTransferredStarLight}.");
    }

    internal async Task HandleObservedCardGain(Player recipient, AbstractModel? source)
    {
        if (Owner?.Creature == null || !CardGainAttribution.IsCausedBy(Owner))
            return;

        Flash();
        await PowerCmd.Apply<EffectsStarlightPower>(Owner.Creature, 1m, Owner.Creature, source as CardModel, false);
        MainFile.Logger.Info(
            $"[VariantPersonShadowScion] card gain observed; granted Starlight | owner={Owner.NetId} | recipient={recipient.NetId} | source={source?.GetType().Name ?? "<null>"}");
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (Owner.GetRelic<PersonalityDerivativeFallenHeavenlyKing>()
                ?.AstralParty_FallenHeavenlyKingDeckReplaced == true)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillPowerfulPityPlus>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
