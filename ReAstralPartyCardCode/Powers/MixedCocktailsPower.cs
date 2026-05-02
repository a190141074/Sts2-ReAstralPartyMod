using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class MixedCocktailsPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal TemporaryStrength;
        public decimal TemporaryDexterity;
        public decimal Dexterity;
        public decimal Heal;
        public decimal Energy;
        public decimal Draw;
        public decimal Block;
    }

    private static readonly Texture2D DetailsHoverTipIcon =
        GD.Load<Texture2D>(GenerateIconPath<MixedCocktailsPower>());

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 0;

    public override LocString Description
    {
        get
        {
            var data = GetInternalData<Data>() ?? new Data();
            var description = new LocString("powers", "RE_ASTRAL_PARTY_MOD_POWER_MIXED_COCKTAILS_POWER.description");
            description.Add("TemporaryStrength", data.TemporaryStrength);
            description.Add("TemporaryDexterity", data.TemporaryDexterity);
            description.Add("Dexterity", data.Dexterity);
            description.Add("Heal", data.Heal);
            description.Add("Energy", data.Energy);
            description.Add("Draw", data.Draw);
            description.Add("Block", data.Block);
            return description;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMixedId),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        var data = GetInternalData<Data>();
        if (data.TemporaryStrength > 0m)
            await PowerCmd.Apply<StrengthPower>(Owner, -data.TemporaryStrength, Owner, null, true);

        if (data.TemporaryDexterity > 0m)
            await PowerCmd.Apply<DexterityPower>(Owner, -data.TemporaryDexterity, Owner, null, true);

        await PowerCmd.Remove(this);
    }

    public async Task RecordTemporaryStrength(decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || amount <= 0m)
            return;

        GetInternalData<Data>().TemporaryStrength += amount;
        await PowerCmd.Apply<StrengthPower>(Owner, amount, applier, cardSource, true);
    }

    public async Task RecordTemporaryDexterity(decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || amount <= 0m)
            return;

        GetInternalData<Data>().TemporaryDexterity += amount;
        await PowerCmd.Apply<DexterityPower>(Owner, amount, applier, cardSource, true);
    }

    public Task RecordDexterity(decimal amount)
    {
        if (amount > 0m)
            GetInternalData<Data>().Dexterity += amount;

        return Task.CompletedTask;
    }

    public Task RecordHeal(decimal amount)
    {
        if (amount > 0m)
            GetInternalData<Data>().Heal += amount;

        return Task.CompletedTask;
    }

    public Task RecordEnergy(decimal amount)
    {
        if (amount > 0m)
            GetInternalData<Data>().Energy += amount;

        return Task.CompletedTask;
    }

    public Task RecordDraw(decimal amount)
    {
        if (amount > 0m)
            GetInternalData<Data>().Draw += amount;

        return Task.CompletedTask;
    }

    public Task RecordBlock(decimal amount)
    {
        if (amount > 0m)
            GetInternalData<Data>().Block += amount;

        return Task.CompletedTask;
    }

    public static async Task<MixedCocktailsPower> GetOrCreate(Creature target, Creature? applier, CardModel? cardSource)
    {
        var existingPower = target.GetPower<MixedCocktailsPower>();
        if (existingPower != null)
            return existingPower;

        await PowerCmd.Apply(
            ModelDb.Power<MixedCocktailsPower>().ToMutable(),
            target,
            1m,
            applier,
            cardSource,
            false);

        return target.GetPower<MixedCocktailsPower>()!;
    }
}