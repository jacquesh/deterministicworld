using System;
using System.Collections.Generic;

using DeterministicWorld.Net;

using Lidgren.Network;

namespace DeterministicWorld
{
    public class PlayerData : dwISerializable
    {
        public static PlayerData getPlayer(int index)
        {
            return players[index];
        }
        private static PlayerData[] players;

        public string name;
        public int index;
        public string idString;

        static PlayerData()
        {
            players = new PlayerData[dwWorldConstants.GAME_MAX_PLAYERS];
        }

        public PlayerData() : this("")
        {
        }

        public PlayerData(string playerName)
        {
            index = -1;
            name = playerName;
        }

        internal void initializeAsLocal()
        {
            idString = NetUtility.GetMacAddress().ToString() + "_" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        }

        internal void assignIndex(int i)
        {
            if (players[i] != null)
            {
                throw new ArgumentException("Attempt to assign an index that is already in use (" + i + ")");
            }

            PlayerData.players[i] = this;
            index = i;
        }

        public void serialize(NetOutgoingMessage outMsg)
        {
            outMsg.Write(idString);
            outMsg.Write(name);
            outMsg.Write(index);
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            idString = inMsg.ReadString();
            name = inMsg.ReadString();

            int newIndex = inMsg.ReadInt32();

            //Only assign the deserialized index if we dont already have one
            //(so for example, a server can override this by assigning an index before deserializing)
            if(index == -1)
                assignIndex(newIndex);
        }

        public override bool Equals(object obj)
        {
            Console.WriteLine("Compare " + this + " to " + obj);

            if (obj.GetType() == typeof(PlayerData))
            {
                Console.WriteLine("Comparing ID " + this.idString + " to " + ((PlayerData)obj).idString);

                if (idString.Equals(((PlayerData)obj).idString))
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
