using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.CardPools;

[RegisterSharedCardPool]
public sealed class TerraprismaCardPool : TypeListCardPoolModel
{
    public override string Title => "terraprisma_internal_card_pool";
    public override string EnergyColorName => "colorless";
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
