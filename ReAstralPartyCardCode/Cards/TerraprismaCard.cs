using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(TerraprismaCardPool), StableEntryStem = "terraprisma")]
public sealed class TerraprismaCard : AstralPartyCardModel
{
    private const string SovereignBladePortraitPath =
        "res://images/packed/card_portraits/token/sovereign_blade.png";

    private const string SovereignBladeBetaPortraitPath =
        "res://images/packed/card_portraits/token/beta/sovereign_blade.png";

    public TerraprismaCard()
        : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
    {
    }

    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath = SovereignBladePortraitPath,
        BetaPortraitPath = SovereignBladeBetaPortraitPath
    };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        this.TryGetCapability<TerraprismaLoadedBladeCapability>(out var capability)
            ? capability.GetSourceHoverTips()
            : [];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner == null || !TerraprismaHelper.TryGetLoadedDamage(this, out var damage))
            return;

        var target = cardPlay.Target is { IsAlive: true }
            ? cardPlay.Target
            : TerraprismaHelper.FindNearestLivingEnemy(Owner);
        if (target == null)
            return;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Unpowered()
            .Targeting(target)
            .BeforeDamage(() =>
            {
                var vfx = TerraprismaHelper.GetVfxNode(Owner, this);
                var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
                if (vfx != null && targetNode != null)
                    vfx.Attack(targetNode.VfxSpawnPosition);
                return Task.CompletedTask;
            })
            .WithHitVfxNode(MegaCrit.Sts2.Core.Nodes.Vfx.NBigSlashVfx.Create)
            .WithHitVfxNode(MegaCrit.Sts2.Core.Nodes.Vfx.NBigSlashImpactVfx.Create)
            .Execute(choiceContext);
    }
}
