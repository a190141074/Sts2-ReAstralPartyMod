using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Interactions.RightClick;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillFortuneMischance : AstralPartyCardModel, IModRightClickableCard
{
    private const decimal BaseHeal = 2m;
    private const decimal AlternateDamage = 2m;
    private const string ActiveColor = "#ffffff";
    private const string InactiveColor = "#888888";

    [SavedProperty] private bool _damageMode { get; set; }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, CardKeyword.Retain, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override bool IsPlayable =>
        Owner != null
        && Owner.GetRelic<PersonalityDerivativeFortuneMischance>()
            ?.AstralParty_PersonalityDerivativeFortuneMischanceStacks >= 1;

    public override TargetType TargetType => _damageMode ? TargetType.AnyEnemy : TargetType.AnyPlayer;

    public override string Title => new LocString("cards", _damageMode
        ? "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_HUO.title"
        : "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_FU.title").GetRawText();

    public override string PortraitPath => _damageMode
        ? "res://ReAstralPartyMod/images/card_portraits/skill_fortune_mischance_huo.png"
        : "res://ReAstralPartyMod/images/card_portraits/skill_fortune_mischance_fu.png";

    protected override string DefaultPortraitPath => PortraitPath;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(BaseHeal),
        new DamageVar(AlternateDamage, ValueProp.Move),
        new StringVar("HealColor", ActiveColor),
        new StringVar("DamageColor", InactiveColor)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        BuildHoverTips();

    private IEnumerable<IHoverTip> BuildHoverTips()
    {
        yield return HoverTipFactory.FromPower<BaiZeBlessingPower>();
        yield return new HoverTip(
            new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_SWITCH_TIP.title"),
            new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_SWITCH_TIP.description"));

        var players = Owner?.Creature?.CombatState?.Players;
        if (players == null || Owner == null)
            yield break;

        if (players.Any(player => player != Owner && player.GetRelic<PersonFeng>() != null))
            yield return new HoverTip(
                new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_FENG_TIP.title"),
                new LocString("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FORTUNE_MISCHANCE_FENG_TIP.description"));
    }

    public SkillFortuneMischance() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyPlayer)
    {
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Model == this && context.Trigger.Source == ModRightClickSource.HandCard;
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        return context.Model == this && Owner != null && context.Trigger.Source == ModRightClickSource.HandCard;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        _damageMode = !_damageMode;
        UpdateModeDisplay();
        await Task.CompletedTask;
    }

    private void UpdateModeDisplay()
    {
        if (DynamicVars == null)
            return;

        if (DynamicVars["HealColor"] is StringVar healColor)
            healColor.StringValue = _damageMode ? InactiveColor : ActiveColor;
        if (DynamicVars["DamageColor"] is StringVar damageColor)
            damageColor.StringValue = _damageMode ? ActiveColor : InactiveColor;

        RefreshCardVisuals();
    }

    private void RefreshCardVisuals()
    {
        var nCard = NCard.FindOnTable(this);
        if (nCard == null)
            return;

        nCard.UpdateVisuals(Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);

        var reloadMethod = typeof(NCard).GetMethod("ReloadPortrait", BindingFlags.Public | BindingFlags.Instance);
        if (reloadMethod != null)
        {
            reloadMethod.Invoke(nCard, null);
            return;
        }

        var portraitNode = FindPortraitNode(nCard);
        if (portraitNode != null && ResourceLoader.Exists(PortraitPath))
            portraitNode.Texture = ResourceLoader.Load<Texture2D>(PortraitPath);
    }

    private static TextureRect? FindPortraitNode(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is TextureRect rect && (rect.Name == "Portrait" || rect.Name == "CardArt" || rect.Name == "Art"))
                return rect;

            var result = FindPortraitNode(child);
            if (result != null)
                return result;
        }

        return null;
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this && !IsClone)
            UpdateModeDisplay();
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == this
            ? (PileType.Hand, CardPilePosition.Top)
            : (pileType, position);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var target = cardPlay.Target ?? Owner?.Creature;
        var ownerCreature = Owner?.Creature;
        if (target == null || ownerCreature == null || !target.IsAlive)
            return;
        if (_damageMode && target.Side == ownerCreature.Side)
            return;
        if (!_damageMode && target.Player == null)
            return;
        if (Owner?.GetRelic<PersonalityDerivativeFortuneMischance>() is not { } derivativeRelic)
            return;
        if (!derivativeRelic.TryConsume(1))
            return;

        if (_damageMode)
        {
            await CreatureCmd.Damage(choiceContext, target, AlternateDamage, ValueProp.Move, ownerCreature, this);
        }
        else
        {
            var targetPlayer = target.Player;
            await CreatureCmd.Heal(target, BaseHeal, true);
            await PowerCmd.Apply<BaiZeBlessingPower>(target, 1m, ownerCreature, this, false);

            if (targetPlayer?.GetRelic<PersonFeng>() != null)
                await PowerCmd.Apply<GatheringStrengthPower>(target, 1m, ownerCreature, this, false);
        }
    }
}
