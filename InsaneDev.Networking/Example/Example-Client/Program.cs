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
            Base client = new Base();//Create an instance of the client used to connect to the server
            client.Connect("127.0.0.1", 6789);//Connect to the server using the ip and port provided
            while (client.IsConnected())//Whilst we are connected to the server
            {
                Packet p = new Packet(10);//Create an empty packet of type 10
                p.AddLong(DateTime.Now.Ticks);//Add to the packet a long, in this case the current time in Ticks
                client.SendPacket(p);//Send the packet over the connection (packet auto disposes when sent)
                Thread.Sleep(20);//Wait for 20 ms before repeating
            }
        }
    }
}
