using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class MultiplayerPlayerLimitPatch
{
    public const int ExpandedPlayerLimit = 12;

    public static void ClampLobbySize(ref int maxPlayers)
    {
        if (maxPlayers != 4)
            return;

        maxPlayers = ExpandedPlayerLimit;
        MainFile.Logger.Info($"Expanded StartRunLobby max players to {ExpandedPlayerLimit}.");
    }

    public static void ClampHostClientLimit(ref int maxClients)
    {
        if (maxClients != 4)
            return;

        maxClients = ExpandedPlayerLimit;
        MainFile.Logger.Info($"Expanded multiplayer host client limit to {ExpandedPlayerLimit}.");
    }
}
