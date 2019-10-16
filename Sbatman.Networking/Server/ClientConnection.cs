#region Usings

using Sbatman.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Sbatman.Networking.Server
{
    public abstract class ClientConnection : IDisposable
    {
        /// <summary>
        ///     The id of the packet sent when connection is established
        /// </summary>
        public const Int32 CONNECT_PACKET = 9999;

        private static Int32 _LastClientIDAllocated;

        protected readonly TcpClient _AttachedSocket;
        protected readonly Int32 _ClientId;
        protected readonly List<Packet> _PacketsToProcess = new List<Packet>();
        protected readonly List<Packet> _PacketsToSend = new List<Packet>();
        private readonly List<Packet> _TempPacketList = new List<Packet>();
        protected Byte[] _ByteBuffer;
        protected Int32 _ByteBufferCount;
        protected TimeSpan _ClientUpdateInterval = new TimeSpan(0, 0, 0, 0, 5);
        protected Boolean _Connected;
        protected MemoryStream _CurrentDataStream;
        protected Boolean _Disposed;
        protected DateTime _LastClientUpdate = DateTime.Now;
        protected NetworkStream _NetStream;
        protected DateTime _TimeOfConnection;
        protected Thread _UpdateThread;
        protected BaseServer _Server;
        protected ReaderWriterLockSlim _Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        ///     An instance of a client connection on the server
        /// </summary>
        /// <param name="server">The Server</param>
        /// <param name="newSocket"> </param>
        /// <param name="bufferSizeInKb">The buffer size in kb for incoming packets</param>
        protected ClientConnection(BaseServer server, TcpClient newSocket, Int32 bufferSizeInKb = 1024)
        {
            _Server = server;
            _ByteBuffer = new Byte[bufferSizeInKb * 1024];
            _ClientId = GetNewClientId();
            _TimeOfConnection = DateTime.Now;
            _AttachedSocket = newSocket;
            _Connected = true;
            Packet p = new Packet(CONNECT_PACKET);
            p.Add(_ClientId);
            _PacketsToSend.Add(p);
            OnConnect();
            _UpdateThread = new Thread(Update);
            _UpdateThread.Start();
        }

        /// <summary>
        ///     Disposes of the client connection, this will cause the buffers to be cleared and any outstanding streams to be
        ///     flushed and closed
        /// </summary>
        public virtual void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            _Connected = false;
            _AttachedSocket?.Dispose();

            _ByteBuffer = null;

            _CurrentDataStream?.Dispose();
            _NetStream?.Dispose();
            _PacketsToProcess?.Clear();
            _PacketsToSend?.Clear();
            _UpdateThread = null;
        }

        /// <summary>
        ///     This function is called when the connection with the client is established
        /// </summary>
        protected abstract void OnConnect();

        /// <summary>
        ///     This function is called when the connection with the client is lost
        /// </summary>
        protected abstract void OnDisconnect();

        /// <summary>
        ///     This function is called at a regular interval to handel incoming packets
        /// </summary>
        protected abstract void ClientUpdateLogic();

        /// <summary>
        ///     This function is called whenever this class would throw and exception
        /// </summary>
        /// <param name="e">The exception thrown</param>
        protected abstract void HandelException(Exception e);

        /// <summary>
        ///     Send the attached client a packet
        /// </summary>
        /// <param name="packet"> Packet to send </param>
        public virtual void SendPacket(Packet packet)
        {
            _Lock.EnterWriteLock();
            _PacketsToSend.Add(packet);
            _Lock.ExitWriteLock();
        }

        /// <summary>
        ///     Send the attached client multiple packets
        /// </summary>
        /// <param name="packets"> List of packets that are to be sent </param>
        public virtual void SendPackets(List<Packet> packets)
        {
            _Lock.EnterWriteLock();
            _PacketsToSend.AddRange(packets);
            _Lock.ExitWriteLock();
        }

        /// <summary>
        ///     Returns all the currently outstanding packets that need processing (with a default maximum of 1000)
        ///     They are removed from the processing queue when this function returns
        /// </summary>
        /// <param name="maximum">The number upper limit of packets to get in one call, default is 1000</param>
        /// <returns>A list containing the packets that require processing</returns>
        protected virtual List<Packet> GetUnprocessedPackets(Int32 maximum = 1000)
        {
            List<Packet> newList = new List<Packet>();

            _Lock.EnterWriteLock();

            Int32 grabSize = Math.Min(_PacketsToProcess.Count, maximum);
            newList.AddRange(_PacketsToProcess.GetRange(0, grabSize));
            _PacketsToProcess.RemoveRange(0, grabSize);

            _Lock.ExitWriteLock();

            return newList;
        }

        /// <summary>
        ///     Returns true of the client is connected
        /// </summary>
        /// <returns> </returns>
        public virtual Boolean Connected => _Connected;

        /// <summary>
        ///     Disconnects from the connected client
        /// </summary>
        public virtual void Disconnect()
        {
            _Disposed = true;
        }

        /// <summary>
        ///     Returns a count of outstanding packets that need to be processed
        /// </summary>
        /// <returns> </returns>
        public virtual Int32 IncomingPacketsCount
        {
            get
            {
                _Lock.EnterReadLock();
                Int32 val = _PacketsToProcess.Count;
                _Lock.ExitReadLock();
                return val;
            }
        }

        /// <summary>
        ///     Returns a count of the packets that are pending to be sent
        /// </summary>
        /// <returns> </returns>
        public virtual Int32 OutgoingPacketsCount
        {
            get
            {
                _Lock.EnterReadLock();
                Int32 val = _PacketsToSend.Count;
                _Lock.ExitReadLock();
                return val;
            }
        }


        /// <summary>
        ///     Returns a DateTime outlining when the connection was made
        /// </summary>
        /// <returns> </returns>
        public virtual DateTime ConnectionStartTime => _TimeOfConnection;

        /// <summary>
        ///     Returns a timespan containing the duration of the connection
        /// </summary>
        /// <returns> </returns>
        public virtual TimeSpan ConnectionAge => DateTime.Now - _TimeOfConnection;

        private void Update()
        {
            while (!_Disposed&&_Connected)
            {
                try
                {
                    _Connected = _AttachedSocket.Client.Connected;
                    if (!_Connected) break;


                    if (_AttachedSocket.Available > 0)
                    {
                        Byte[] dataPulled = new Byte[_AttachedSocket.Available];
                        _AttachedSocket.GetStream().Read(dataPulled, 0, dataPulled.Length);
                        Array.Copy(dataPulled, 0, _ByteBuffer, _ByteBufferCount, dataPulled.Length);
                        _ByteBufferCount += dataPulled.Length;
                    }

                    Boolean finding = _ByteBufferCount > 11;
                    while (finding)
                    {
                        Boolean packetStartPresent = true;
                        for (Int32 x = 0; x < 4; x++)
                        {
                            if (_ByteBuffer[x] == Packet.PacketStart[x]) continue;
                            packetStartPresent = false;
                            break;
                        }

                        if (packetStartPresent)
                        {
                            Int32 size = BitConverter.ToInt32(_ByteBuffer, 6);
                            if (_ByteBufferCount >= size)
                            {
                                Byte[] packet = new Byte[size];
                                Array.Copy(_ByteBuffer, 0, packet, 0, size);
                                Array.Copy(_ByteBuffer, size, _ByteBuffer, 0, _ByteBufferCount - size);
                                _ByteBufferCount -= size;
                                _Lock.EnterWriteLock();
                                _PacketsToProcess.Add(Packet.FromByteArray(packet));
                                _Lock.ExitWriteLock();
                            }
                            else
                            {
                                finding = false;
                            }
                        }
                        else
                        {
                            Int32 offset = -1;
                            for (Int32 x = 0; x < _ByteBufferCount; x++)
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

                    _Lock.EnterWriteLock();
                    if (_PacketsToSend.Count > 0)
                    {
                        _TempPacketList.AddRange(_PacketsToSend);
                        _PacketsToSend.Clear();
                    }
                    _Lock.ExitWriteLock();

                    if (_TempPacketList.Count > 0)
                    {
                        _NetStream = new NetworkStream(_AttachedSocket.Client);
                        foreach (Byte[] data in _TempPacketList.Select(p => p.ToByteArray()))
                        {
                            _NetStream.Write(data, 0, data.Length);
                        }

                        _NetStream.Close();
                        _NetStream.Dispose();
                        _NetStream = null;
                    }

                    _TempPacketList.Clear();
                    if (DateTime.Now - _LastClientUpdate > _ClientUpdateInterval)
                    {
                        _LastClientUpdate += _ClientUpdateInterval;
                        ClientUpdateLogic();
                    }

                    Thread.Sleep(4);
                }
                catch (Exception e)
                {
                    HandelException(e);
                }
            }

            if (_AttachedSocket != null)
            {
                if (_AttachedSocket.Connected)
                {
                    _AttachedSocket.Close();
                    _AttachedSocket.Client.Dispose();
                }
                _AttachedSocket.Dispose();
            }
            _Connected = false;
            OnDisconnect();
            Dispose();
        }

        /// <summary>
        ///     Returns whether this Client Connection has been disposed
        /// </summary>
        /// <returns>True if disposed else false</returns>
        public virtual Boolean Disposed => _Disposed;

        /// <summary>
        ///     Returns the client ID of this client connection
        /// </summary>
        /// <returns>The id of the client connection</returns>
        private static Int32 GetNewClientId()
        {
            return _LastClientIDAllocated++;
        }
    }
}