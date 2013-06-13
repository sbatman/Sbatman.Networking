#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
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
        protected readonly Queue<Packet> _PacketsToProcess = new Queue<Packet>();
        protected readonly List<Packet> _PacketsToSend = new List<Packet>();
        protected int _BufferSize = 1000000;

        /// <summary>
        ///     Initialise a connection to the speicified adress and port
        /// </summary>
        /// <param name="serverAddress"> Adress of server to attempt to connect to </param>
        /// <param name="port"> </param>
        public bool Connect(String serverAddress, int port)
        {
            _ErrorMessage = "";
            _Error = false;
            _ByteBuffer = new byte[_BufferSize];
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
            lock (_PacketsToProcess)
            {
                int count = _PacketsToProcess.Count;
                packets = new Packet[count];
                for (int x = 0; x < count; x++)
                {
                    packets[x] = _PacketsToProcess.Dequeue();
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
            return _PacketsToProcess.Count;
        }

        /// <summary>
        ///     Retruns an int containing the number of packets that have not yet been sent
        /// </summary>
        /// <returns> </returns>
        public int GetPacketsToSendCount()
        {
            return _PacketsToSend.Count;
        }

        /// <summary>
        ///     Sends a packet to the connected server
        /// </summary>
        /// <param name="packet"> Packet to send </param>
        public virtual void SendPacket(Packet packet)
        {
            lock (_PacketsToSend)
            {
                _PacketsToSend.Add(packet);
            }
        }

        /// <summary>
        ///     Send multiplw packets to the connected server
        /// </summary>
        /// <param name="packets"> List of packets to send </param>
        public virtual void SendPackets(List<Packet> packets)
        {
            lock (_PacketsToSend)
            {
                _PacketsToSend.AddRange(packets);
            }
        }

        /// <summary>
        ///     Returns true of your connected to the server
        /// </summary>
        /// <returns> </returns>
        public virtual bool IsConnected()
        {
            return _ClientSocket != null && _ClientSocket.Connected;
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
                    lock (_PacketsToSend)
                    {
                        templist.AddRange(_PacketsToSend);
                        _PacketsToSend.Clear();
                    }

                    if (templist.Count > 0)
                    {
                        _NetStream = new NetworkStream(_ClientSocket.Client);
                        foreach (byte[] packet in templist.Select(p => p.ToByteArray()))
                        {
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
                                if (p != null) _PacketsToProcess.Enqueue(p);
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
                    lock (_PacketsToProcess)
                    {
                        foreach (Packet p in templist) _PacketsToProcess.Enqueue(p);
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
    }
}
