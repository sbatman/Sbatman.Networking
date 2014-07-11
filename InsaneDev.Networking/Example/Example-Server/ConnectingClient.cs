using System.Net.Sockets;
using InsaneDev.Networking;

namespace Example_Server
{
    class ConnectingClient : InsaneDev.Networking.Server.ClientConnection
    {
        public ConnectingClient(TcpClient client)
            : base(client)
        {

        }

        protected override void OnConnect()
        {
            Program.Write("Client Connected");
        }

        protected override void OnDisconnect()
        {
            Program.Write("Client Disconnected");
        }

        protected override void ClientUpdateLogic()
        {
            foreach (Packet packet in GetOutStandingProcessingPackets())
            {
                switch (packet.Type)
                {
                    case 10:
                        Program.Write(((long)packet.GetObjects()[0]).ToString());
                        break;
                }

            }
        }
    }
}
