using System;
using System.Collections.Generic;

namespace DeterministicWorld
{
    public abstract partial class dwWorld2D
    {
        private int playerCountCached;
        private bool playerCountDirty;
        public int playerCount
        {
            get
            {
                if (!playerCountDirty)
                    return playerCountCached;

                int result = 0;

                for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
                {
                    if (playerList[i] != null)
                        result += 1;
                }
                result += unindexedPlayers.Count;

                playerCountCached = result;
                return result;
            }
        }

        private HashSet<dwPlayerData> unindexedPlayers;
        private dwPlayerData[] playerList;

        //Player list modifications
        public void addPlayer(dwPlayerData newPlayer)
        {
            unindexedPlayers.Add(newPlayer);
        }

        public void removePlayer(dwPlayerData player)
        {
            if (player == null)
                return;

            if (unindexedPlayers.Contains(player))
                unindexedPlayers.Remove(player);

            else
                playerList[player.index] = null;

            playerCountDirty = true;
        }

        internal void assignPlayerIndex(long playerUID, int newIndex)
        {
            dwPlayerData player = null;

            //Get the  player either form unindexed players or from the player list
            HashSet<dwPlayerData>.Enumerator unindexedEnumerator = unindexedPlayers.GetEnumerator();
            while (unindexedEnumerator.MoveNext())
            {
                dwPlayerData tempPlayer = unindexedEnumerator.Current;

                if (tempPlayer.uid == playerUID)
                {
                    player = tempPlayer;
                    break;
                }
            }
            if (player != null)
            {
                unindexedPlayers.Remove(player);
                playerCountDirty = true;
            }
            else
            {
                player = getPlayerByUID(playerUID);
            }

            dwLog.info("Attempt to assign index " + newIndex + " to " + player.name);
            if (player.index >= 0)
                playerList[player.index] = null;

            player.index = newIndex;

            if (newIndex >= 0)
                playerList[newIndex] = player;
        }

        //Data accessors
        public dwPlayerData getPlayerByUID(long uid)
        {
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (playerList[i] != null && playerList[i].uid == uid)
                    return playerList[i];
            }

            return null;
        }

        public dwPlayerData getPlayer(int index)
        {
            return playerList[index];
        }
    }
}
