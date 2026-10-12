using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class LoadTerraprismaPower : AstralPartyPowerModel
{
    private CardModel? _neutralizedCard;
    private decimal _loadedBaseDamage;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override int DisplayAmount => 0;
    public override bool ShouldReceiveCombatHooks => true;

    internal bool TryBeginDirectLoad(CardModel card, bool isAutoPlay)
    {
        if (isAutoPlay
            || _neutralizedCard != null
            || Owner?.Player == null
            || card.Owner != Owner.Player
            || card.Type != CardType.Attack)
            return false;

        _neutralizedCard = card;
        _loadedBaseDamage = card.DynamicVars.TryGetValue("Damage", out var damage)
            ? damage.BaseValue
            : 0m;
        return true;
    }

    internal async Task<bool> DirectLoadAsync(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Owner?.Player == null || !ReferenceEquals(card, _neutralizedCard))
            return false;

        var source = _neutralizedCard;
        var baseDamage = _loadedBaseDamage;

        var loaded = await TerraprismaHelper.CreateLoadedBladeAsync(
            Owner.Player,
            source,
            baseDamage);
        if (!loaded)
            return false;

        _neutralizedCard = null;
        _loadedBaseDamage = 0m;
        try
        {
            await PowerCmd.Remove(this);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[Terraprisma] load power cleanup deferred after successful mount | error={ex}");
        }

        return loaded;
    }

    internal void CancelDirectLoad(CardModel card)
    {
        if (!ReferenceEquals(card, _neutralizedCard))
            return;

        _neutralizedCard = null;
        _loadedBaseDamage = 0m;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Player == null || side != Owner.Side)
            return;

        _neutralizedCard = null;
        _loadedBaseDamage = 0m;
        await PowerCmd.Remove(this);
    }

    internal bool IsNeutralizing(CardModel card)
    {
        return ReferenceEquals(card, _neutralizedCard);
    }
}
