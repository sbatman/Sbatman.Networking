#region Usings

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

#endregion

namespace InsaneDev.Networking.Client
{
    public class Base
    {
        protected byte[] ByteBuffer;
        protected int ByteBufferCOunt = 0;
        protected int ClientId = -1;
        protected TcpClient ClientSocket = new TcpClient();
        protected bool Connected = false;
        protected bool Error;
        protected string ErrorMessage;
        protected NetworkStream NetStream;
        protected int PacketCheckInterval = 6;
        protected Thread PacketHandel;
        protected Queue<Packet> PacketsToProcess = new Queue<Packet>();
        protected List<Packet> PacketsToSend = new List<Packet>();
        protected BinaryFormatter Serialiser = new BinaryFormatter();


        /// <summary>
        ///     Initialise a connection to the speicified adress and port
        /// </summary>
        /// <param name="serverAddress"> Adress of server to attempt to connect to </param>
        /// <param name="port"> </param>
        public bool Connect(String serverAddress, int port)
        {
            ErrorMessage = "";
            Error = false;
            ByteBuffer = new byte[100000];
            try
            {
                ClientSocket = new TcpClient(serverAddress, port);
            }
            catch
            {
                Console.WriteLine("NerfCorev2:Networking - Failure to connect to " + serverAddress + " on port " + port);
            }
            if (ClientSocket.Connected)
            {
                Connected = true;
                PacketHandel = new Thread(Update);
                PacketHandel.Start();
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Changes the number of milliseconds between packet checks (this shouldnt be higer then 8ms for timely responces, or lower then 3ms to repvent high cpu usage
        /// </summary>
        /// <param name="timeBettweenChecksInMs"> Number of ms between checks </param>
        public void SetPacketCheckInterval(int timeBettweenChecksInMs)
        {
            if (timeBettweenChecksInMs <= 0) timeBettweenChecksInMs = 1;
            PacketCheckInterval = timeBettweenChecksInMs;
        }

        /// <summary>
        ///     Retruns a list of all the packs that have come in and need to be processed. This clears the list.
        /// </summary>
        /// <returns> </returns>
        public Packet[] GetPacketsToProcess()
        {
            Packet[] packets;
            lock (PacketsToProcess)
            {
                int count = PacketsToProcess.Count;
                packets = new Packet[count];
                for (int x = 0; x < count; x++)
                {
                    packets[x] = PacketsToProcess.Dequeue();
                }
            }
            return packets;
        }

        /// <summary>
        ///     Retruns an int containing the number of waiting prackets
        /// </summary>
        /// <returns> </returns>
        public int GetPacketsToProcessCount()
        {
            return PacketsToProcess.Count;
        }

        /// <summary>
        ///     Retruns an int containing the number of packets that have not yet been sent
        /// </summary>
        /// <returns> </returns>
        public int GetPacketsToSendCount()
        {
            return PacketsToSend.Count;
        }

        /// <summary>
        ///     Sends a packet to the connected server
        /// </summary>
        /// <param name="packet"> Packet to send </param>
        public virtual void SendPacket(Packet packet)
        {
            lock (PacketsToSend)
            {
                PacketsToSend.Add(packet);
            }
        }

        /// <summary>
        ///     Send multiplw packets to the connected server
        /// </summary>
        /// <param name="packets"> List of packets to send </param>
        public virtual void SendPackets(List<Packet> packets)
        {
            lock (PacketsToSend)
            {
                PacketsToSend.AddRange(packets);
            }
        }

        /// <summary>
        ///     Returns true of your connected to the server
        /// </summary>
        /// <returns> </returns>
        public virtual bool IsConnected()
        {
            if (ClientSocket != null)
            {
                return ClientSocket.Connected;
            }
            return false;
        }

        /// <summary>
        ///     Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
        }

        private void Update()
        {
            try
            {
                while (Connected)
                {
                    List<Packet> templist = new List<Packet>();
                    lock (PacketsToSend)
                    {
                        templist.AddRange(PacketsToSend);
                        PacketsToSend.Clear();
                    }

                    if (templist.Count > 0)
                    {
                        NetStream = new NetworkStream(ClientSocket.Client);
                        foreach (Packet p in templist)
                        {
                            byte[] packet = p.ToByteArray();
                            NetStream.Write(packet, 0, packet.Length);                         
                        }
                        NetStream.Close();
                    }
                    templist.Clear();

                    if (ClientSocket.Available > 0)
                    {
                        byte[] datapulled = new byte[ClientSocket.Available];
                        ClientSocket.GetStream().Read(datapulled, 0, datapulled.Length);
                        Array.Copy(datapulled, 0, ByteBuffer, ByteBufferCOunt, datapulled.Length);
                        ByteBufferCOunt += datapulled.Length;
                    }
                    bool finding = ByteBufferCOunt > 11;
                    while (finding)
                    {
                        bool packerstartpresent = true;
                        for (int x = 0; x < 4; x++)
                        {
                            if (ByteBuffer[x] == Packet.PacketStart[x]) continue;
                            packerstartpresent = false;
                            break;
                        }
                        if (packerstartpresent)
                        {
                            int size = BitConverter.ToInt32(ByteBuffer, 6);
                            if (ByteBufferCOunt >= size)
                            {
                                byte[] packet = new byte[size];
                                Array.Copy(ByteBuffer, 0, packet, 0, size);
                                Array.Copy(ByteBuffer, size, ByteBuffer, 0, ByteBufferCOunt - size);
                                ByteBufferCOunt -= size;
                                Packet p = Packet.FromByteArray(packet);
                                if (p != null) PacketsToProcess.Enqueue(p);
                            }
                            else
                            {
                                finding = false;
                            }
                        }
                        else
                        {
                            int offset = -1;
                            for (int x = 0; x < ByteBufferCOunt; x++)
                            {
                                if (ByteBuffer[x] == Packet.PacketStart[x]) offset = x;
                            }
                            if (offset != -1)
                            {
                                Array.Copy(ByteBuffer, offset, ByteBuffer, 0, ByteBufferCOunt - offset);
                                ByteBufferCOunt -= offset;
                            }
                            else
                            {
                                ByteBufferCOunt = 0;
                            }
                        }
                        if (ByteBufferCOunt < 12) finding = false;
                    }
                    lock (PacketsToProcess)
                    {
                        foreach (Packet p in templist) PacketsToProcess.Enqueue(p);
                    }
                    templist.Clear();
                    Thread.Sleep(PacketCheckInterval);
                }
            }
            catch (Exception e)
            {
                Error = true;
                ErrorMessage = e.Message;
                Console.WriteLine("Powerup:Networking - Networking layer failed " + e.Message);
            }

            if (ClientSocket != null)
            {
                if (ClientSocket.Connected) ClientSocket.Close();
                ClientSocket = null;
            }
            Connected = false;
        }

        public bool HasErrored()
        {
            return Error;
        }

        public string GetError()
        {
            return ErrorMessage;
        }

        private void NetLayerMessage(Packet p)
        {
            switch (p.Type)
            {
                case 9999:
                    ClientId = (int) p.GetObjects()[0];
                    break;
            }
        }
    }
}
