using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Patches;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace AstralPartyMod.AstralPartyCardCode.Potions;

[Pool(typeof(SharedPotionPool))]
public class PersonChestChoose : AstralPartyPotionModel
{
    private const int RevealCount = 4;

    private static readonly RelicModel[] PersonaRelics =
    [
        // 人格：蒸蛋
        // ModelDb.Relic<PersonWeirdEgg>(),
        
        // 人格：太刀虾
        // ModelDb.Relic<PersonSamuraiPrawn>(),
        
        // 人格：史莱姆
        // ModelDb.Relic<PersonSlimeLulu>(),
        
        // 人格：绿油油
        ModelDb.Relic<PersonBionicJasmine>(),
        
        // 人格：老板娘
        // ModelDb.Relic<PersonProprietress>(),
        
        // 人格：鼠鼠
        ModelDb.Relic<PersonMousyLian>(),
        
        // 人格：大虎鲸
        ModelDb.Relic<PersonBlueWhale>(),
        
        // 人格：绿洲女王
        ModelDb.Relic<PersonOasisQueen>(),
        
        // 人格：小猎手
        ModelDb.Relic<PersonInkShadowHunter>(),
        
        // 人格：米米
        ModelDb.Relic<PersonMascotGirlMimi>(),
        
        // 人格：美甲师
        ModelDb.Relic<PersonSupermanMegas>(),
        
        // 人格：小雷
        // ModelDb.Relic<PersonXiaoLei>(),
        
        //人格：社恐修女
        // ModelDb.Relic<PersonSocialFearNun>(),
        
        // 人格：调酒师
        // ModelDb.Relic<PersonJillSteinle>(), 
    ];

    public PersonChestChoose() : base(true)
    {
    }

    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.Self;

    public override bool PassesCustomUsabilityCheck =>
        Owner != null
        && CombatManager.Instance?.IsInProgress != true
        && GetAvailablePersonaRelics(Owner).Count > 0;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null)
            return;

        var availableRelics = GetAvailablePersonaRelics(Owner);
        if (availableRelics.Count == 0)
            return;

        // Shuffle with the run RNG so the revealed relic set stays deterministic.
        var relicOptions = availableRelics.ToList();
        relicOptions.UnstableShuffle(Owner.RunState.Rng.Niche);
        relicOptions = relicOptions.Take(Math.Min(RevealCount, relicOptions.Count)).ToList();

        // Override the generic relic picker banner so the player sees a persona-specific title here.
        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("potions", $"{Id.Entry}.selectionScreenHeader").GetRawText());

        // Show canonical relics in the picker, then grant a mutable copy of the chosen relic.
        var selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(Owner, relicOptions);
        if (selectedRelic == null)
            return;

        await RelicCmd.Obtain(selectedRelic.ToMutable(), Owner);
    }

    private static IReadOnlyList<RelicModel> GetAvailablePersonaRelics(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        var ownedRelicIds = owner.Relics
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

        // Keep the source list canonical and still de-duplicate by Id as a safety net
        // so the picker cannot ever reveal duplicate persona relics.
        return PersonaRelics
            .DistinctBy(relic => relic.Id)
            .Where(relic => !ownedRelicIds.Contains(relic.Id))
            .ToList();
    }
}