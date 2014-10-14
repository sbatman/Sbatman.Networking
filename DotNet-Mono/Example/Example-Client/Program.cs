using System;
using System.Threading;
using InsaneDev.Networking;
using InsaneDev.Networking.Client;

namespace Example_Client
{
    class Program
    {
        static void Main()
        {
            BaseClient client = new BaseClient();//Create an instance of the client used to connect to the server
            client.Connect("127.0.0.1", 6789);//Connect to the server using the ip and port provided
            while (client.IsConnected())//Whilst we are connected to the server
            {
                Packet p1 = new Packet(10);//Create an empty packet of type 10
                p1.AddInt64(DateTime.Now.Ticks);//Add to the packet a long, in this case the current time in Ticks
                client.SendPacket(p1);//Send the packet over the connection (packet auto disposes when sent)

                Packet p2 = new Packet(11);//Create an empty packet of type 10
                p2.AddBool(true);//Add to the packet a bool
                p2.AddString("test cake");//Add to the packet a string
                client.SendPacket(p2);//Send the packet over the connection (packet auto disposes when sent)

                Thread.Sleep(20);//Wait for 20 ms before repeating
            }
        }
    }
}
