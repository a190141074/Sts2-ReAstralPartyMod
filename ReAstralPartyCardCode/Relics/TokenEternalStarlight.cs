using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class TokenEternalStarlight : AstralPartyRelicModel
{
    private const int FloorsPerPayout = 3;
    private const int FirstTierMinStacks = 10;
    private const int SecondTierMinStacks = 25;
    private const int ThirdTierMinStacks = 40;
    private const int MembershipTierMinStacks = 56;

    private const int FirstTierGoldReward = 5;
    private const int SecondTierGoldReward = 15;
    private const int ThirdTierGoldReward = 25;
    private const int MembershipTierGoldReward = 25;

    [SavedProperty] public int AstralParty_TokenEternalStarlightStacks { get; set; }
    [SavedProperty] public int AstralParty_TokenEternalStarlightFloorProgress { get; set; }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_TokenEternalStarlightStacks;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("CurrentSetLine", GetCurrentSetLineText())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralEternalStarlightSetId),
        BuildCurrentSetHoverTip()
    ];

    protected override string IconBasePath => "res://ReAstralPartyMod/images/relic/token_Eternal_Starlight";

    public static IHoverTip BuildReferenceHoverTip()
    {
        var title = new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.title");
        var body = new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.description")
            .GetRawText()
            .Replace("\n{CurrentSetLine}", string.Empty);
        return new HoverTip(
            title,
            body,
            GD.Load<Texture2D>("res://ReAstralPartyMod/images/relic/token_Eternal_Starlight.png")
        );
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenEternalStarlightStacks = 0;
        AstralParty_TokenEternalStarlightFloorProgress = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null)
            return;

        AstralParty_TokenEternalStarlightFloorProgress++;
        var shouldPayoutFloorGold = AstralParty_TokenEternalStarlightFloorProgress >= FloorsPerPayout;
        if (shouldPayoutFloorGold)
            AstralParty_TokenEternalStarlightFloorProgress = 0;

        if (shouldPayoutFloorGold && AstralParty_TokenEternalStarlightStacks > 0)
        {
            Flash();
            await PlayerCmd.GainGold(AstralParty_TokenEternalStarlightStacks, Owner);
        }

        if (room.RoomType != RoomType.Shop)
            return;

        if (!TryGetCurrentSetBonus(AstralParty_TokenEternalStarlightStacks, out var goldReward,
                out var grantMembership))
            return;

        Flash();
        await PlayerCmd.GainGold(goldReward, Owner);

        if (grantMembership && Owner.GetRelic<MembershipCard>() == null)
        {
            await RelicCmd.Obtain(ModelDb.Relic<MembershipCard>().ToMutable(), Owner);
            MerchantUiRefreshHelper.TryRefreshCurrentMerchantUi(Owner.RunState);
        }
    }

    public int GetStacks()
    {
        return AstralParty_TokenEternalStarlightStacks;
    }

    public void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_TokenEternalStarlightStacks += amount;
        Flash();
        InvokeDisplayAmountChanged();
        Owner?.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
    }

    private static bool TryGetCurrentSetBonus(int stacks, out int goldReward, out bool grantMembership)
    {
        goldReward = 0;
        grantMembership = false;

        if (stacks >= MembershipTierMinStacks)
        {
            goldReward = MembershipTierGoldReward;
            grantMembership = true;
            return true;
        }

        if (stacks >= ThirdTierMinStacks)
        {
            goldReward = ThirdTierGoldReward;
            return true;
        }

        if (stacks >= SecondTierMinStacks)
        {
            goldReward = SecondTierGoldReward;
            return true;
        }

        if (stacks >= FirstTierMinStacks)
        {
            goldReward = FirstTierGoldReward;
            return true;
        }

        return false;
    }

    private HoverTip BuildCurrentSetHoverTip()
    {
        var title = new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_title");
        var body = GetCurrentSetLineText();
        return new HoverTip(title, body, GD.Load<Texture2D>(PackedIconPath));
    }

    private string GetCurrentSetLineText()
    {
        return GetCurrentSetLine()?.GetRawText()
               ?? new LocString("relics", "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_none")
                   .GetRawText();
    }

    private LocString? GetCurrentSetLine()
    {
        return AstralParty_TokenEternalStarlightStacks switch
        {
            >= MembershipTierMinStacks => new LocString("relics",
                "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_line_tier4"),
            >= ThirdTierMinStacks => new LocString("relics",
                "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_line_tier3"),
            >= SecondTierMinStacks => new LocString("relics",
                "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_line_tier2"),
            >= FirstTierMinStacks => new LocString("relics",
                "RE_ASTRAL_PARTY_MOD_RELIC_TOKEN_ETERNAL_STARLIGHT.current_set_line_tier1"),
            _ => null
        };
    }

    public static async Task<TokenEternalStarlight?> GrantStacks(Player owner, int amount)
    {
        if (amount <= 0)
            return owner.GetRelic<TokenEternalStarlight>();

        var relic = owner.GetRelic<TokenEternalStarlight>();
        if (relic == null)
        {
            await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(owner,
                ModelDb.Relic<TokenEternalStarlight>());
            relic = owner.GetRelic<TokenEternalStarlight>();
        }

        relic?.AddStacks(amount);
        return relic;
    }
}
