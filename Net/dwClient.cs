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

        //Initialization
        //==============
        public void initialize(PlayerData localPlayerData)
        {
            //Set up net connection
            peerConfig = new NetPeerConfiguration(WorldConstants.GAME_ID);
            netClient = new NetClient(peerConfig);
            _connectionStatus = NetConnectionStatus.Disconnected;

            //Finalise local player data
            localPlayer = localPlayerData;
            
            //Create player list
            clientWorld.addPlayer(localPlayer);
        }

        public void connect()
        {
            netClient.Start();
            NetOutgoingMessage loginMessage = getLoginMessage();
            netClient.Connect("127.0.0.1", WorldConstants.GAME_NET_PORT, loginMessage);

            Timer timer = new Timer(timerCallback, this, 0, 50);
        }

        public void disconnect()
        {
            netClient.Disconnect("Leaving");
        }

        //Mutator functions
        //=================
        private void setConnectionStatus(NetConnectionStatus newStatus)
        {
            _connectionStatus = newStatus;

            if (onNetStatusChanged != null)
                onNetStatusChanged(newStatus);
        }

        //Continual/Update functions
        //==========================
        private void timerCallback(Object stateInfo)
        {
            NetIncomingMessage inMsg = netClient.ReadMessage();

            if (inMsg != null)
            {
                Console.WriteLine("Received: " + inMsg.MessageType);

                switch (inMsg.MessageType)
                {
                    //App-specific data
                    case (NetIncomingMessageType.Data):
                        NetDataType msgDataType = (NetDataType)inMsg.ReadByte();
                        switch (msgDataType)
                        {
                            case(NetDataType.PlayerData):
                                readPlayerData(inMsg);
                                break;

                            case(NetDataType.StartGame):
                                startGame();
                                break;

                            default:
                                Console.WriteLine("Unknown data packet of size " + inMsg.LengthBytes + " bytes");
                                if(onNetDataReceived != null)
                                    onNetDataReceived(inMsg);
                                break;
                        }
                        break;

                    //The server (or this client)'s status changed (e.g connected/disconnected/connecting/disconnecting)
                    case (NetIncomingMessageType.StatusChanged):
                        setConnectionStatus(inMsg.SenderConnection.Status);
                        break;

                    default:
                        Console.WriteLine("Contents: " + inMsg.ReadString());
                        break;
                }
                Console.WriteLine();
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

            outMsg.WriteAllFields(localPlayer, BindingFlags.Instance | BindingFlags.Public);
            Console.WriteLine("Sending player data with name: " + localPlayer.name);

            return outMsg;
        }

        //Outgoing messages
        //=================

        /// <summary>
        /// Send a request to the server to start the game,
        /// this will (if successful) notify all players (including this one)
        /// that the game should start, and that they should do any necessary loading
        /// </summary>
        public void requestStartGame()
        {
            //
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.StartGame);

            netClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
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

        /// <summary>
        /// Send an update to the server that acknowledges that this client completed an update frame
        /// and informs it of any input that was given by this client
        /// </summary>
        internal void sendFrameUpdate(uint targetFrame, FrameInput input)
        {
            NetOutgoingMessage outMsg = netClient.CreateMessage();

            outMsg.Write((byte)NetDataType.FrameUpdate);
            outMsg.Write(targetFrame);
            outMsg.Write(input.orderList.Count);

            for (int i = 0; i < input.orderList.Count; i++)
            {
                outMsg.WriteAllFields(input.orderList[i], BindingFlags.Instance | BindingFlags.Public);
            }

            netClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        //Incoming messages
        //=================

        /// <summary>
        /// Reads in a packet containing all player-related data, and parses it
        /// into a PlayerData object
        /// </summary>
        /// <param name="inMsg"></param>
        private void readPlayerData(NetIncomingMessage inMsg)
        {
            PlayerData newPlayer = new PlayerData();

            inMsg.ReadAllFields(newPlayer, BindingFlags.Instance | BindingFlags.Public);

            clientWorld.addPlayer(newPlayer);
        }

    }
}
