#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace InsaneDev.Networking.Server
{
    public abstract class ClientConnection
    {
        private static int _LastClientIdAllocated;

        protected readonly TcpClient AttachedSocket;
        protected readonly int ClientId;
        protected readonly List<Packet> PacketsToProcess = new List<Packet>();
        protected readonly List<Packet> PacketsToSend = new List<Packet>();
        private readonly List<Packet> _tempPacketList = new List<Packet>();
        public bool Disposed;
        protected byte[] _ByteBuffer;
        protected int _ByteBufferCount;
        protected TimeSpan _ClientUpdateInterval = new TimeSpan(0, 0, 0, 0, 5);
        protected bool _Connected;
        protected MemoryStream _CurrentDataStream;
        protected DateTime _LastClientUpdate = DateTime.Now;
        protected NetworkStream _NetStream;
        protected DateTime _TimeOfConnection;
        protected Thread _UpdateThread;

        /// <summary>
        ///     An instance of a client connection on the server
        /// </summary>
        /// <param name="newSocket"> </param>
        public ClientConnection(TcpClient newSocket)
        {
            _ByteBuffer = new byte[100000];
            ClientId = GetNewClientId();
            _TimeOfConnection = DateTime.Now;
            AttachedSocket = newSocket;
            _Connected = true;
            Packet p = new Packet(9999);
            p.AddInt(ClientId);
            PacketsToSend.Add(p);
// ReSharper disable DoNotCallOverridableMethodsInConstructor
            OnConnect();
// ReSharper restore DoNotCallOverridableMethodsInConstructor
            _UpdateThread = new Thread(Update);
            _UpdateThread.Start();
        }

        protected abstract void OnConnect();
        protected abstract void OnDisconnect();
        protected abstract void ClientUpdateLogic();

        /// <summary>
        ///     Send the attached client a packet
        /// </summary>
        /// <param name="packet"> Packet to send </param>
        public virtual void SendPacket(Packet packet)
        {
            lock (PacketsToSend) PacketsToSend.Add(packet);
        }

        /// <summary>
        ///     Send the attached client multiple packets
        /// </summary>
        /// <param name="packets"> List of packets that are to be sent </param>
        public virtual void SendPackets(List<Packet> packets)
        {
            lock (PacketsToSend) PacketsToSend.AddRange(packets);
            packets.Clear();
        }

        protected virtual List<Packet> GetOutStandingProcessingPackets()
        {
            List<Packet> newList = new List<Packet>();
            lock (PacketsToProcess)
            {
                int grabSize = Math.Min(PacketsToProcess.Count, 1000);
                newList.AddRange(PacketsToProcess.GetRange(0, grabSize));
                PacketsToProcess.RemoveRange(0, grabSize);
            }
            return newList;
        }

        /// <summary>
        ///     Returns true of the client is connected
        /// </summary>
        /// <returns> </returns>
        public virtual bool IsConnected()
        {
            return _Connected;
        }

        /// <summary>
        ///     Disconnects from the connected client
        /// </summary>
        public virtual void Disconnect()
        {
            _Connected = false;
        }

        /// <summary>
        ///     Returns a count of outstanding packets that need to be processed
        /// </summary>
        /// <returns> </returns>
        public virtual int GetOutStandingProcessingPacketsCount()
        {
            return PacketsToProcess.Count;
        }

        /// <summary>
        ///     Returns a count of the packets that are pending to be sent
        /// </summary>
        /// <returns> </returns>
        public virtual int GetOutStandingSendPacketsCount()
        {
            return PacketsToSend.Count;
        }

        /// <summary>
        ///     Returns a DateTime outligning when
        /// </summary>
        /// <returns> </returns>
        public virtual DateTime GetTimeOfConnection()
        {
            return _TimeOfConnection;
        }

        /// <summary>
        ///     Returns a timespan containing the duration of the connection
        /// </summary>
        /// <returns> </returns>
        public virtual TimeSpan GetDurationOfConnection()
        {
            return DateTime.Now - _TimeOfConnection;
        }

        private void Update()
        {
            try
            {
                while (_Connected)
                {
                    _Connected = AttachedSocket.Client.Connected;
                    lock (PacketsToProcess)
                    {
                        if (AttachedSocket.Available > 0)
                        {
                            byte[] datapulled = new byte[AttachedSocket.Available];
                            AttachedSocket.GetStream().Read(datapulled, 0, datapulled.Length);
                            Array.Copy(datapulled, 0, _ByteBuffer, _ByteBufferCount, datapulled.Length);
                            _ByteBufferCount += datapulled.Length;
                        }
                        bool finding = _ByteBufferCount > 11;
                        while (finding)
                        {
                            bool packetStartPresent = true;
                            for (int x = 0; x < 4; x++)
                            {
                                if (_ByteBuffer[x] == Packet.PacketStart[x]) continue;
                                packetStartPresent = false;
                                break;
                            }
                            if (packetStartPresent)
                            {
                                int size = BitConverter.ToInt32(_ByteBuffer, 6);
                                if (_ByteBufferCount >= size)
                                {
                                    byte[] packet = new byte[size];
                                    Array.Copy(_ByteBuffer, 0, packet, 0, size);
                                    Array.Copy(_ByteBuffer, size, _ByteBuffer, 0, _ByteBufferCount - size);
                                    _ByteBufferCount -= size;
                                    PacketsToProcess.Add(Packet.FromByteArray(packet));
                                }
                                else
                                {
                                    finding = false;
                                }
                            }
                            else
                            {
                                int offset = -1;
                                for (int x = 0; x < _ByteBufferCount; x++)
                                {
                                    if (_ByteBuffer[x] == Packet.PacketStart[x]) offset = x;
                                }
                                if (offset != -1)
                                {
                                    Array.Copy(_ByteBuffer, offset, _ByteBuffer, 0, _ByteBufferCount - offset);
                                    _ByteBufferCount -= offset;
                                }
                                else
                                {
                                    _ByteBufferCount = 0;
                                }
                            }
                            if (_ByteBufferCount < 12) finding = false;
                        }
                    }

                    lock (PacketsToSend)
                    {
                        if (PacketsToSend.Count > 0)
                        {
                            _tempPacketList.AddRange(PacketsToSend);
                            PacketsToSend.Clear();
                        }
                    }
                    if (_tempPacketList.Count > 0)
                    {
                        _NetStream = new NetworkStream(AttachedSocket.Client);
                        foreach (byte[] data in _tempPacketList.Select(p => p.ToByteArray()))
                        {
                            _NetStream.Write(data, 0, data.Length);
                        }
                        _NetStream.Close();
                        _NetStream.Dispose();
                        _NetStream = null;
                        foreach (Packet p in _tempPacketList) p.Dispose();
                    }
                    _tempPacketList.Clear();
                    if (DateTime.Now - _LastClientUpdate > _ClientUpdateInterval)
                    {
                        _LastClientUpdate += _ClientUpdateInterval;
                        ClientUpdateLogic();
                    }
                    Thread.Sleep(4);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (AttachedSocket != null)
            {
                if (AttachedSocket.Connected)
                {
                    AttachedSocket.Close();
                    AttachedSocket.Client.Dispose();
                }
            }
            _Connected = false;
            OnDisconnect();
            Dispose();
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            _Connected = false;
            if (AttachedSocket != null && AttachedSocket.Connected) AttachedSocket.Close();

            _ByteBuffer = null;

            if (_CurrentDataStream != null)
            {
                _CurrentDataStream.Close();
                _CurrentDataStream.Dispose();
                _CurrentDataStream = null;
            }
            if (_NetStream != null)
            {
                _NetStream.Close();
                _NetStream.Dispose();
                _NetStream = null;
            }
            PacketsToProcess.Clear();
            PacketsToSend.Clear();
            if (_UpdateThread != null)
            {
                _UpdateThread.Abort();
                _UpdateThread = null;
            }
        }

        private static int GetNewClientId()
        {
            _LastClientIdAllocated++;
            return _LastClientIdAllocated;
        }
    }
}