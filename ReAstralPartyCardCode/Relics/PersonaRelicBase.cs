using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Combat.HandSize;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

// Marker base for relics that should participate in persona-specific systems.
public abstract class PersonaRelicBase : AstralPartyRelicModel, IMaxHandSizeModifier
{
    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player != Owner)
            return currentMaxHandSize;

        return currentMaxHandSize + 1;
    }
}
