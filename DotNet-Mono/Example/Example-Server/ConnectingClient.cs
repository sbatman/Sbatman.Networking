using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Sbatman.Networking;
using Sbatman.Serialize;
using System.Collections.Generic;

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
                        Program.Write(((Single)packet.GetObjects()[1]).ToString(CultureInfo.InvariantCulture));

                        Byte[] data = ((Byte[]) packet.GetObjects()[2]);
                        StringBuilder sb = new StringBuilder();
                        foreach (Byte b in data)
                        {
                            sb.Append(b);
                            sb.Append(',');
                        }
                        sb.AppendLine();

                        foreach (Double d in (List<Double>)packet.GetObjects()[3])
                        {
                            sb.Append(d);
                            sb.Append(',');
                        }


                            sb.AppendLine();

                            foreach (Single d in (List<Single>)packet.GetObjects()[4])
                        {
                            sb.Append(d);
                            sb.Append(',');
                        }

                        Program.Write(sb.ToString());

                        Packet response = new Packet(45);
                        response.Add(data);

                        SendPacket(response);
                        break;
                    case 11:
                        Program.Write(((Boolean)packet.GetObjects()[0]).ToString(CultureInfo.InvariantCulture));
                        Program.Write(((String)packet.GetObjects()[1]).ToString(CultureInfo.InvariantCulture));
                        break;
                }
            }
        }

        protected override void HandelException(System.Exception e)
        {
            
        }
    }
}
