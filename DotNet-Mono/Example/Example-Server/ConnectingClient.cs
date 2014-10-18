﻿using System;
using System.Globalization;
using System.Net.Sockets;
using Sbatman.Networking;

namespace Example_Server
{
    class ConnectingClient : Sbatman.Networking.Server.ClientConnection
    {
        /// <summary>
        /// Created by the server when a new client is connecting
        /// </summary>
        /// <param name="client"></param>
        public ConnectingClient(TcpClient client)
            : base(client)
        {

        }

        protected override void OnConnect()
        {
            //The client has confirmed to have been connected by this point
            Program.Write("Client Connected");
        }

        protected override void OnDisconnect()
        {
            //The client has either activly disconnected or has timedout
            Program.Write("Client Disconnected");
        }

        /// <summary>
        /// This is run on a seperate thread at a given interval, its useful for handeling incoming packets
        /// </summary>
        protected override void ClientUpdateLogic()
        {
            foreach (Packet packet in GetOutStandingProcessingPackets())
            {
                switch (packet.Type)
                {
                    case 10:
                        Program.Write(((Int64)packet.GetObjects()[0]).ToString(CultureInfo.InvariantCulture));
                        Program.Write(((float)packet.GetObjects()[1]).ToString(CultureInfo.InvariantCulture));
                        SendPacket(new Packet(45));
                        break;
                    case 11:
                        Program.Write(((bool)packet.GetObjects()[0]).ToString(CultureInfo.InvariantCulture));
                        Program.Write(((string)packet.GetObjects()[1]).ToString(CultureInfo.InvariantCulture));
                        break;
                }
            }
        }

        protected override void HandelException(System.Exception e)
        {
            
        }
    }
}
