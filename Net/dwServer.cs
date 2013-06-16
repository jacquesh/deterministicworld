using System;
using System.Collections.Generic;

using Lidgren.Network;

using System.Reflection;

namespace DeterministicWorld.Net
{
    public enum NetDataType
    {
        PlayerData,
        FrameUpdate,
        StartGame,
    }

    class ServerPlayerData
    {
        public PlayerData playerData;
        public NetConnection connection;
    }

    public class dwServer
    {
        private NetPeerConfiguration peerConfig;
        private NetServer netServer;

        private List<ServerPlayerData> playerList;

        private dwWorld2D serverWorld;

        public dwServer(dwWorld2D world)
        {
            Console.WriteLine("Initializing netServer...");

            //Setup the connection between the server and the world
            serverWorld = world;
            serverWorld.onWorldUpdate += gameUpdate;

            //Create the player list
            playerList = new List<ServerPlayerData>();

            //Create the network configuration
            peerConfig = new NetPeerConfiguration(WorldConstants.GAME_ID);
            peerConfig.Port = WorldConstants.GAME_NET_PORT;
            peerConfig.MaximumConnections = 200;
            peerConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            //Create the server instance
            netServer = new NetServer(peerConfig);
        }

        public void start()
        {
            Console.WriteLine("Starting netServer...");
            netServer.Start();

            Console.WriteLine("Server running...");
            NetIncomingMessage inMsg;
            while (netServer.Status == NetPeerStatus.Starting || netServer.Status == NetPeerStatus.Running)
            {
                inMsg = netServer.ReadMessage();

                if (inMsg != null)
                {
                    Console.WriteLine("Received: " + inMsg.MessageType);

                    switch (inMsg.MessageType)
                    {
                        //Client attempting to create a connection
                        case (NetIncomingMessageType.ConnectionApproval):
                            //Accept request and set up new player
                            inMsg.SenderConnection.Approve();
                            setupNewPlayer(inMsg);
                            break;

                        //App-specific data
                        case (NetIncomingMessageType.Data):
                            NetDataType dataType = (NetDataType)inMsg.ReadByte();
                            switch (dataType)
                            {
                                case(NetDataType.StartGame):
                                    startGame();
                                    break;

                                case(NetDataType.FrameUpdate):
                                    uint targetFrame = inMsg.ReadUInt32();
                                    int orderCount = inMsg.ReadInt32();

                                    for (int i = 0; i < orderCount; i++)
                                    {
                                        //In wc3 (and indeed other RTS's) orders are all given an ID, so that they can be easily referenced and dereferenced (a simple lookup table would do)
                                        //I guess I should do something similar here with an order manager where you register orders and then get the type from a reference ID
                                        //Then again I should really implement serialization manually into all my networked objects
                                        //Order o = new Order();
                                        //inMsg.ReadAllFields(o);
                                        handleOrder(null, targetFrame);
                                    }

                                    break;
                            }
                            break;

                        //A client's status changed (e.g connected/disconnected/connecting/disconnecting)
                        case (NetIncomingMessageType.StatusChanged):
                            Console.WriteLine("Status changed to: " + inMsg.SenderConnection.Status);
                            break;

                        default:
                            Console.WriteLine("Contents: " + inMsg.ReadString());
                            break;
                    }
                    Console.WriteLine();
                }
            }
        }

        private void gameUpdate()
        {
            //Broadcast input
        }

        private void setupNewPlayer(NetIncomingMessage connectionMsg)
        {
            NetOutgoingMessage outMsg;

            //Create new player data
            ServerPlayerData newPlayer = new ServerPlayerData();
            newPlayer.connection = connectionMsg.SenderConnection;

            newPlayer.playerData = new PlayerData();
            newPlayer.playerData.deserialize(connectionMsg);

            //Tell the new player about all the players in this game
            for (int i = 0; i < playerList.Count; i++)
            {
                outMsg = netServer.CreateMessage();
                outMsg.Write((byte)NetDataType.PlayerData);
                playerList[i].playerData.serialize(outMsg);

                netServer.SendMessage(outMsg, newPlayer.connection, NetDeliveryMethod.ReliableOrdered);
            }

            //Tell all players in this game about the new player
            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerData);
            newPlayer.playerData.serialize(outMsg);

            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
            
            //Update netServer player list
            playerList.Add(newPlayer);
            Console.WriteLine(newPlayer.playerData.name+" has connected");
        }

        /// <summary>
        /// Initiate the start of the game
        /// </summary>
        private void startGame()
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.StartGame);

            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void handleOrder(Order order, uint targetFrame)
        {

        }

        /// <summary>
        /// Send a packet to all clients
        /// </summary>
        /// <param name="outMsg"></param>
        /// <param name="deliveryMethod"></param>
        private void sendToAll(NetOutgoingMessage outMsg, NetDeliveryMethod deliveryMethod)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                netServer.SendMessage(outMsg, playerList[i].connection, deliveryMethod);
            }
            
        }
    }
}
