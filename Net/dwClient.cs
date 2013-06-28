using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld.Net
{
    public class dwClient
    {
        //Network event delegates
        public delegate void NetDataReceivedDelegate(NetIncomingMessage inMsg);
        public delegate void NetStatusChangedDelegate(NetConnectionStatus newStatus);
        
        //Network events
        public event Action onGameStart;
        public event NetDataReceivedDelegate onNetDataReceived;
        public event NetStatusChangedDelegate onNetStatusChanged;

        //Accessor Properties
        public NetConnectionStatus connectionStatus
        {
            get { return _connectionStatus; }
        }

        //Network connection and settings
        private NetPeerConfiguration peerConfig;
        private NetClient netClient;
        private NetConnectionStatus _connectionStatus;

        private Timer netUpdateTimer;
        
        //Network peer data
        private PlayerData localPlayer;

        //World data
        private dwWorld2D clientWorld;
        
		//Constructors
		//============
        public dwClient(dwWorld2D world)
        {
            clientWorld = world;
            clientWorld.onWorldUpdate += gameUpdate;
        }

        ~dwClient()
        {
            netUpdateTimer.Dispose();
        }

        //Initialization
        //==============
        public PlayerData initialize()
        {
            //Set up net connection
            peerConfig = new NetPeerConfiguration(dwWorldConstants.GAME_ID);
            netClient = new NetClient(peerConfig);
            _connectionStatus = NetConnectionStatus.Disconnected;

            netClient.Start();

            //Finalise local player data
            localPlayer = new PlayerData();
            localPlayer.uid = netClient.UniqueIdentifier;
            localPlayer.name = localPlayer.uid.ToString();
            dwLog.info("Creating player - " + localPlayer.uid);
            
            //Create player list
            clientWorld.addPlayer(localPlayer);

            return localPlayer;
        }

        public void connect()
        {
            NetOutgoingMessage loginMessage = getLoginMessage();
            netClient.Connect("127.0.0.1", dwWorldConstants.GAME_NET_PORT, loginMessage);

            netUpdateTimer = new Timer(timerCallback, this, 0, 50);
        }

        public void disconnect()
        {
            netClient.Disconnect("Leaving");
            netClient.Shutdown("Leaving");
        }

        public void shutdown()
        {
            disconnect();
            netClient.Shutdown("NetClient shutting down");
        }

        //Continual/Update functions
        //==========================
        private void timerCallback(Object stateInfo)
        {
            NetIncomingMessage inMsg = netClient.ReadMessage();

            if (inMsg != null)
            {
                switch (inMsg.MessageType)
                {
                    //App-specific data
                    case (NetIncomingMessageType.Data):
                        NetDataType msgDataType = (NetDataType)inMsg.ReadByte();
                        handleDataMessage(inMsg, msgDataType);
                        break;

                    //The server (or this client)'s status changed (e.g connected/disconnected/connecting/disconnecting)
                    case (NetIncomingMessageType.StatusChanged):
                        handleConnectionStatusUpdate(inMsg.SenderConnection.RemoteUniqueIdentifier, inMsg.SenderConnection.Status);
                        break;

                    case(NetIncomingMessageType.DebugMessage):
                        dwLog.debug(inMsg.ReadString());
                        break;

                    case(NetIncomingMessageType.WarningMessage):
                        dwLog.warn(inMsg.ReadString());
                        break;

                    default:
                        dwLog.info("Unhandled message type received: "+inMsg.MessageType);
                        break;
                }
            }
        }

        private void gameUpdate()
        {
            uint targetFrame = clientWorld.gameFrame + 0;
            
            //Get input from the world and send it
            FrameInput input = clientWorld.getInputData(targetFrame);

            sendFrameUpdate(targetFrame, input);
        }

        //==========================
        // Network control functions
        //==========================

        //Packet creation functions
        //=========================
        private NetOutgoingMessage getLoginMessage()
        {
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write(dwWorldConstants.GAME_ID);
            outMsg.Write(dwWorldConstants.GAME_VERSION);

            return outMsg;
        }

        private NetOutgoingMessage getConnectionMessage()
        {
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.PlayerConnect);
            localPlayer.serialize(outMsg);

            return outMsg;
        }

        //Outgoing messages
        //=================

        public void requestPlayerIndexUpdate(long playerID, int newIndex)
        {
            dwLog.info("Request player " + playerID + " index change to " + newIndex);
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.PlayerIndexUpdate);
            outMsg.Write(playerID);
            outMsg.Write(newIndex);

            netClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Send a request to the server to start the game,
        /// this will (if successful) notify all players (including this one)
        /// that the game should start, and that they should do any necessary loading
        /// </summary>
        public void requestStartGame()
        {
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.StartGame);

            netClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Send an update to the server that acknowledges that this client completed an update frame
        /// and informs it of any input that was given by this client
        /// </summary>
        internal void sendFrameUpdate(uint targetFrame, FrameInput input)
        {
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.FrameUpdate);
            input.serialize(outMsg);

            netClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);

            if(input.orderList.Count > 0)
                dwLog.info("Sent input with " + input.orderList.Count + " orders.");
        }

        //Incoming messages
        //=================

        private void handleDataMessage(NetIncomingMessage inMsg, NetDataType msgDataType)
        {
            switch (msgDataType)
            {
                case (NetDataType.FrameUpdate):
                    readFrameUpdateData(inMsg);
                    break;

                case (NetDataType.PlayerConnect):
                    readPlayerData(inMsg);
                    break;

                case (NetDataType.PlayerDisconnect):
                    long playerUID = inMsg.ReadInt64();
                    PlayerData dcPlayer = clientWorld.getPlayerByUID(playerUID);
                    handlePlayerDisconnect(dcPlayer);
                    break;

                case (NetDataType.StartGame):
                    startGame();
                    break;

                case (NetDataType.PlayerIndexUpdate):
                    readPlayerIndexUpdate(inMsg);
                    break;

                default:
                    dwLog.info("Unknown data packet of size " + inMsg.LengthBytes + " bytes");
                    if (onNetDataReceived != null)
                        onNetDataReceived(inMsg);
                    break;
            }
        }

        /// <summary>
        /// Reads in a packet containing all player-related data, and parses it
        /// into a PlayerData object
        /// </summary>
        /// <param name="inMsg"></param>
        private void readPlayerData(NetIncomingMessage inMsg)
        {
            PlayerData newPlayer = new PlayerData();

            newPlayer.deserialize(inMsg);

            clientWorld.addPlayer(newPlayer);
            dwLog.info("Received player data for " + newPlayer.name);
        }

        private void readPlayerIndexUpdate(NetIncomingMessage inMsg)
        {
            long playerID = inMsg.ReadInt64();
            int newIndex = inMsg.ReadInt32();
 
            clientWorld.assignPlayerIndex(playerID, newIndex);
        }

        /// <summary>
        /// Actually start the simulation and update the current status.
        /// Also run any onStartGame callbacks
        /// </summary>
        private void startGame()
        {
            clientWorld.startSimulation();

            if (onGameStart != null)
                onGameStart();
        }

        private void handleConnectionStatusUpdate(long connectionUID, NetConnectionStatus newStatus)
        {
            //The connection ID here for clients will always be the server...
            //but it indicates a status change for the local player (so to speak)

            //Send connection data if necessary
            if (newStatus == NetConnectionStatus.Connected)
            {
                NetOutgoingMessage connectedMessage = getConnectionMessage();
                netClient.SendMessage(connectedMessage, NetDeliveryMethod.ReliableOrdered);
            }

            //Update currently stored connection status
            _connectionStatus = newStatus;

            if (onNetStatusChanged != null)
                onNetStatusChanged(newStatus);
        }

        private void handlePlayerDisconnect(PlayerData dcPlayer)
        {
            clientWorld.removePlayer(dcPlayer);
        }

        
        private void readFrameUpdateData(NetIncomingMessage inMsg)
        {
            uint targetFrame = inMsg.ReadUInt32();
            int orderCount = inMsg.ReadInt32();

            if(orderCount > 0)
                dwLog.info("Received input with " + orderCount + " orders.");

            for (int i = 0; i < orderCount; i++)
            {
                int orderID = inMsg.ReadInt32();
                Order o = (Order)Activator.CreateInstance(OrderRegister.instance.idToOrder(orderID));
                o.deserialize(inMsg);
                
                clientWorld.issueOrder(o.owner, o);
            }
        }
    }
}
