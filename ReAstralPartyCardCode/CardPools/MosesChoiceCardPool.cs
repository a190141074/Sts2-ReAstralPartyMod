using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.CardPools;

[RegisterSharedCardPool]
public sealed class MosesChoiceCardPool : TypeListCardPoolModel
{
    public override string Title => "moses_choice_card_pool";
    public override string EnergyColorName => "colorless";
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
