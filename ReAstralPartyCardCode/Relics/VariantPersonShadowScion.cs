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
        HoverTipFactory.FromPower<StarLightPower>(),
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

        var starlight = Owner.GetRelic<TokenEternalStarlight>()?.GetStacks() ?? 0;
        var bonus = starlight / EternalStarlightThreshold;
        if (bonus <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, bonus, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, bonus, Owner.Creature, null);
    }

    internal async Task HandleObservedCardGain(Player recipient, AbstractModel? source)
    {
        if (Owner?.Creature == null || !CardGainAttribution.IsCausedBy(Owner))
            return;

        Flash();
        await PowerCmd.Apply<StarLightPower>(Owner.Creature, 1m, Owner.Creature, source as CardModel, false);
        MainFile.Logger.Info(
            $"[VariantPersonShadowScion] card gain observed; granted Starlight | owner={Owner.NetId} | recipient={recipient.NetId} | source={source?.GetType().Name ?? "<null>"}");
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillPowerfulPityPlus>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }
}
