using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillSixDragonsAzure : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>()
    ];

    public SkillSixDragonsAzure() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        var drawPileCount = PileType.Draw.GetPile(Owner).Cards.Count;
        if (drawPileCount % 2 != 0)
        {
            foreach (var player in PersonMultiplayerEffectHelper.GetStableCombatPlayers(Owner))
            {
                if (player.Creature == null || !player.Creature.IsAlive)
                    continue;

                await PowerCmd.Apply<BufferPower>(player.Creature, 1m, Owner.Creature, this, false);
            }
        }
        else
        {
            await ResolveEvenDrawPileBenefit();
        }

        if (ManaAmplificationHelper.GetCurrent(Owner) >= 18 && Owner.Creature.CombatState != null)
        {
            var transcend = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTokenTranscendDimensions>(), Owner);
            await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(transcend, true, CardPilePosition.Top, this);
        }
    }

    private async Task ResolveEvenDrawPileBenefit()
    {
        if (Owner?.Creature == null)
            return;

        var totalPoints = ManaAmplificationHelper.GetCurrent(Owner) + 6;
        var summonPoints = 0;
        var vigorPoints = 0;
        var temporaryDexterityPoints = 0;

        for (var i = 0; i < totalPoints; i++)
        {
            var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                3,
                MainFile.ModId,
                ModelDb.GetId<VariantPersonWamdus>().Entry,
                Owner.NetId,
                Owner.Creature.CombatState?.RoundNumber ?? 0,
                Id.Entry,
                "six_dragons_azure_even_split",
                i);
            switch (roll)
            {
                case 0:
                    summonPoints++;
                    break;
                case 1:
                    vigorPoints++;
                    break;
                default:
                    temporaryDexterityPoints++;
                    break;
            }
        }

        if (summonPoints > 0)
            await OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, summonPoints, this);
        if (vigorPoints > 0)
            await PowerCmd.Apply<VigorPower>(Owner.Creature, vigorPoints, Owner.Creature, this, false);
        if (temporaryDexterityPoints > 0)
            await AstralTemporaryDexterityPower.Apply(Owner.Creature, temporaryDexterityPoints, this, Owner.Creature, this, false);
    }
}
