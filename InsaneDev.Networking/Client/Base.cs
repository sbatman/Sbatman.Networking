#region Usings

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace InsaneDev.Networking.Client
{
    public class Base
    {
        protected byte[] _ByteBuffer;
        protected int _ByteBufferCOunt;
        protected int _ClientId = -1;
        protected TcpClient _ClientSocket = new TcpClient();
        protected bool _Connected;
        protected bool _Error;
        protected string _ErrorMessage;
        protected NetworkStream _NetStream;
        protected int _PacketCheckInterval = 6;
        protected Thread _PacketHandel;
        protected readonly Queue<Packet> PacketsToProcess = new Queue<Packet>();
        protected readonly List<Packet> PacketsToSend = new List<Packet>();

        /// <summary>
        ///     Initialise a connection to the speicified adress and port
        /// </summary>
        /// <param name="serverAddress"> Adress of server to attempt to connect to </param>
        /// <param name="port"> </param>
        public bool Connect(String serverAddress, int port)
        {
            _ErrorMessage = "";
            _Error = false;
            _ByteBuffer = new byte[100000];
            try
            {
                _ClientSocket = new TcpClient(serverAddress, port);
            }
            catch
            {
                Console.WriteLine("NerfCorev2:Networking - Failure to connect to " + serverAddress + " on port " + port);
            }
            if (_ClientSocket.Connected)
            {
                _Connected = true;
                _PacketHandel = new Thread(Update);
                _PacketHandel.Start();
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
            _PacketCheckInterval = timeBettweenChecksInMs;
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
            if (_ClientSocket != null)
            {
                return _ClientSocket.Connected;
            }
            return false;
        }

        /// <summary>
        ///     Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            _Connected = false;
        }

        private void Update()
        {
            try
            {
                while (_Connected)
                {
                    List<Packet> templist = new List<Packet>();
                    lock (PacketsToSend)
                    {
                        templist.AddRange(PacketsToSend);
                        PacketsToSend.Clear();
                    }

                    if (templist.Count > 0)
                    {
                        _NetStream = new NetworkStream(_ClientSocket.Client);
                        foreach (Packet p in templist)
                        {
                            byte[] packet = p.ToByteArray();
                            _NetStream.Write(packet, 0, packet.Length);                         
                        }
                        _NetStream.Close();
                    }
                    templist.Clear();

                    if (_ClientSocket.Available > 0)
                    {
                        byte[] datapulled = new byte[_ClientSocket.Available];
                        _ClientSocket.GetStream().Read(datapulled, 0, datapulled.Length);
                        Array.Copy(datapulled, 0, _ByteBuffer, _ByteBufferCOunt, datapulled.Length);
                        _ByteBufferCOunt += datapulled.Length;
                    }
                    bool finding = _ByteBufferCOunt > 11;
                    while (finding)
                    {
                        bool packerstartpresent = true;
                        for (int x = 0; x < 4; x++)
                        {
                            if (_ByteBuffer[x] == Packet.PacketStart[x]) continue;
                            packerstartpresent = false;
                            break;
                        }
                        if (packerstartpresent)
                        {
                            int size = BitConverter.ToInt32(_ByteBuffer, 6);
                            if (_ByteBufferCOunt >= size)
                            {
                                byte[] packet = new byte[size];
                                Array.Copy(_ByteBuffer, 0, packet, 0, size);
                                Array.Copy(_ByteBuffer, size, _ByteBuffer, 0, _ByteBufferCOunt - size);
                                _ByteBufferCOunt -= size;
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
                            for (int x = 0; x < _ByteBufferCOunt; x++)
                            {
                                if (_ByteBuffer[x] == Packet.PacketStart[x]) offset = x;
                            }
                            if (offset != -1)
                            {
                                Array.Copy(_ByteBuffer, offset, _ByteBuffer, 0, _ByteBufferCOunt - offset);
                                _ByteBufferCOunt -= offset;
                            }
                            else
                            {
                                _ByteBufferCOunt = 0;
                            }
                        }
                        if (_ByteBufferCOunt < 12) finding = false;
                    }
                    lock (PacketsToProcess)
                    {
                        foreach (Packet p in templist) PacketsToProcess.Enqueue(p);
                    }
                    templist.Clear();
                    Thread.Sleep(_PacketCheckInterval);
                }
            }
            catch (Exception e)
            {
                _Error = true;
                _ErrorMessage = e.Message;
                Console.WriteLine("Powerup:Networking - Networking layer failed " + e.Message);
            }

            if (_ClientSocket != null)
            {
                if (_ClientSocket.Connected) _ClientSocket.Close();
                _ClientSocket = null;
            }
            _Connected = false;
        }

        public bool HasErrored()
        {
            return _Error;
        }

        public string GetError()
        {
            return _ErrorMessage;
        }

        private void NetLayerMessage(Packet p)
        {
            switch (p.Type)
            {
                case 9999:
                    _ClientId = (int) p.GetObjects()[0];
                    break;
            }
        }
    }
}
