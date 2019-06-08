#region Usings

using Sbatman.Serialize;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Sbatman.Networking.Client
{
    /// <summary>
    ///     A Base class of a client connection. This can be used to connect to the specified server. All message handling is
    ///     performed ASynchronously
    /// </summary>
    public class BaseClient
    {
        /// <summary>
        ///     Queue containing un processed packets
        /// </summary>
        protected readonly Queue<Packet> _PacketsToProcess = new Queue<Packet>();

        /// <summary>
        ///     List of packets that have yet to be sent
        /// </summary>
        protected readonly List<Packet> _PacketsToSend = new List<Packet>();

        /// <summary>
        ///     The buffer size allocated to this client
        /// </summary>
        protected Int32 _BufferSize;

        /// <summary>
        ///     Buffer of bytes used to collect incoming packets and putt hem together
        /// </summary>
        protected Byte[] _ByteBuffer;

        /// <summary>
        ///     Current point in the byte buffer to use for new data
        /// </summary>
        protected Int32 _ByteBufferCount;

        /// <summary>
        ///     The TCP socket the client is connected on
        /// </summary>
        protected TcpClient _ClientSocket = new TcpClient();

        /// <summary>
        ///     Bool identifying if the client is currently connected or not
        /// </summary>
        protected Boolean _Connected;

        /// <summary>
        ///     Set true in the event of an error
        /// </summary>
        protected Boolean _Error;

        /// <summary>
        ///     Last internal error message
        /// </summary>
        protected String _ErrorMessage;

        /// <summary>
        ///     The interval in MS packets are checked for
        /// </summary>
        protected Int32 _PacketCheckInterval = 2;

        /// <summary>
        ///     The thread used for handling packets
        /// </summary>
        protected Thread _PacketHandel;

        protected Action<String> _LogFunction;

        /// <summary>
        /// Creates an instance of BaseClient, Call connect to establish a connection.
        /// </summary>
        /// <param name="logFunction">A function to which the code can log errors or warnings S(left null if not required)</param>
        public BaseClient(Action<String> logFunction = null)
        {
            _LogFunction = logFunction;
        }

        /// <summary>
        ///     Initialise a connection to the specified address and port
        /// </summary>
        /// <param name="serverAddress"> Address of server to attempt to connect to </param>
        /// <param name="port">The port over which to connect</param>
        /// <param name="bufferSizeInKb">The size in Kilobytes of the internal store for received but unprocessed packets, packets received close to or larger than this size may not be processed</param>
        public Boolean Connect(String serverAddress, Int32 port, Int32 bufferSizeInKb = 1024)
        {
            return Connect(new IPEndPoint(IPAddress.Parse(serverAddress), port), bufferSizeInKb);
        }

        /// <summary>
        ///     Initialise a connection to the specified endpoint
        /// </summary>
        /// <param name="ipEndPoint">The endpoint to which a connection should be attempted</param>
        /// <param name="bufferSizeInKb">The size in Kilobytes of the internal store for received but unprocessed packets, packets received close to or larger than this size may not be processed</param>
        public Boolean Connect(IPEndPoint ipEndPoint, Int32 bufferSizeInKb = 1024)
        {
            _BufferSize = bufferSizeInKb * 1024;
            _ErrorMessage = "";
            _Error = false;
            if (_ByteBuffer == null)
            {
                _ByteBuffer = new Byte[_BufferSize];
            }
            else
            {
                for (Int32 i = 0; i < _BufferSize; i++) _ByteBuffer[i] = 0;
            }
            try
            {
                if (_ClientSocket == null)
                {
                    _ClientSocket = new TcpClient(ipEndPoint);
                    _ClientSocket.NoDelay = true;
                }
                else
                {
                    _ClientSocket.Connect(ipEndPoint);
                }
                if (_ClientSocket.Connected)
                {
                    _Connected = true;
                    if (_PacketHandel != null)
                    {
                        _PacketHandel.Abort();
                        _PacketHandel = null;
                    }
                    _PacketHandel = new Thread(Update);
                    _PacketHandel.Start();
                    return true;
                }
            }
            catch
            {
                _LogFunction?.Invoke($"Sbatman:Networking - Failure to connect to {ipEndPoint.Address} on port {ipEndPoint.Port}");
            }

            return false;
        }


        /// <summary>
        ///     Changes the number of milliseconds between packet checks (this shouldn't be higher then 4ms for timely responses, or
        ///     lower then 1ms to prevent high cpu usage
        /// </summary>
        /// <param name="timeBetweenChecksInMs"> Number of ms between checks </param>
        public void SetPacketCheckInterval(Int32 timeBetweenChecksInMs)
        {
            if (timeBetweenChecksInMs <= 0) timeBetweenChecksInMs = 1;
            _PacketCheckInterval = timeBetweenChecksInMs;
        }

        /// <summary>
        /// Returns a bool representing whether no delay has been forced to true. This disables an underlying packet bunching logic
        /// decreasing available bandwidth in exchange for quicker response times.
        /// </summary>
        /// <returns></returns>
        public Boolean GetForceNoDelay()
        {
            return _ClientSocket.NoDelay;
        }

        /// <summary>
        /// sets whether no delay has been forced to true. This disables an underlying packet bunching logic
        /// decreasing available bandwidth in exchange for quicker response times. Default = true
        /// </summary>
        /// <param name="setting">True for speed over bandwidth, false for bandwidth over speed</param>
        public void SetForceNoDelay(Boolean setting)
        {
            _ClientSocket.NoDelay = setting;
        }

        /// <summary>
        ///     Returns a list of all the packs that have come in and need to be processed. This clears the list.
        /// </summary>
        /// <returns> </returns>
        public Packet[] GetPacketsToProcess()
        {
            Packet[] packets;
            lock (_PacketsToProcess)
            {
                Int32 count = _PacketsToProcess.Count;
                packets = new Packet[count];
                for (Int32 x = 0; x < count; x++)
                {
                    packets[x] = _PacketsToProcess.Dequeue();
                }
            }
            return packets;
        }

        /// <summary>
        ///     Injects packets into the process list as though they had been received over the network, great for debugging
        ///     and for local server/client combo's
        /// </summary>
        /// <param name="p">The packet to inject</param>
        public void InjectToPacketsToProcess(Packet p)
        {
            lock (_PacketsToProcess)
            {
                _PacketsToProcess.Enqueue(p);
            }
        }

        /// <summary>
        ///     Returns an int containing the number of waiting packets
        /// </summary>
        /// <returns> </returns>
        public Int32 GetPacketsToProcessCount()
        {
            lock (_PacketsToProcess)
            {
                return _PacketsToProcess.Count;
            }
        }

        /// <summary>
        ///     Returns an int containing the number of packets that have not yet been sent
        /// </summary>
        /// <returns> </returns>
        public Int32 GetPacketsToSendCount()
        {
            lock (_PacketsToSend)
            {
                return _PacketsToSend.Count;
            }
        }

        /// <summary>
        ///     Sends a packet to the connected server, and disposes the packet once sent.
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
        ///     Send multiple packets to the connected server
        /// </summary>
        /// <param name="packets"> List of packets to send</param>
        public virtual void SendPacket(IEnumerable<Packet> packets)
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
        public virtual Boolean Connected => _ClientSocket != null && _ClientSocket.Connected;

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
                    List<Packet> tempList = new List<Packet>();
                    lock (_PacketsToSend)
                    {
                        tempList.AddRange(_PacketsToSend);
                        _PacketsToSend.Clear();
                    }

                    if (tempList.Count > 0)
                    {
                        lock (_ClientSocket)
                        {
                            NetworkStream netStream = new NetworkStream(_ClientSocket.Client);
                            foreach (Packet packet in tempList)
                            {
                                Byte[] data = packet.ToByteArray();
                                netStream.Write(data, 0, data.Length);
                                packet.Dispose();
                            }
                            netStream.Close();
                        }
                    }
                    tempList.Clear();
                    lock (_ClientSocket)
                    {
                        if (_ClientSocket.Available > 0)
                        {
                            Byte[] dataPulled = new Byte[_ClientSocket.Available];
                            _ClientSocket.GetStream().Read(dataPulled, 0, dataPulled.Length);
                            Array.Copy(dataPulled, 0, _ByteBuffer, _ByteBufferCount, dataPulled.Length);
                            _ByteBufferCount += dataPulled.Length;
                        }
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
                                Packet p = Packet.FromByteArray(packet);
                                if (p != null) lock (_PacketsToProcess)
                                    {
                                        _PacketsToProcess.Enqueue(p);
                                    }
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
                    lock (_PacketsToProcess)
                    {
                        foreach (Packet p in tempList) _PacketsToProcess.Enqueue(p);
                    }
                    tempList.Clear();
                    Thread.Sleep(_PacketCheckInterval);
                }
            }
            catch (Exception e)
            {
                _Error = true;
                _ErrorMessage = e.Message;
            }

            if (_ClientSocket != null)
            {
                if (_ClientSocket.Connected) _ClientSocket.Close();
                _ClientSocket = null;
            }
            _Connected = false;
        }

        /// <summary>
        ///     Returns true if an internal error has occured. This can be retrieved with GetError.
        /// </summary>
        /// <returns></returns>
        public Boolean HasErrored()
        {
            return _Error;
        }

        /// <summary>
        ///     Returns the message string of the last error and resets the has error to false
        /// </summary>
        /// <returns></returns>
        public String GetError()
        {
            _Error = false;
            return _ErrorMessage;
        }

        /// <summary>
        ///     Returns the internal TCP socket used by this client. Lock the TCPClient when in use to maintain thread safe
        ///     operations
        /// </summary>
        /// <returns></returns>
        public TcpClient GetInternalSocket()
        {
            return _ClientSocket;
        }

        /// <summary>
        ///     Gets the size of the internal buffer array that stores incoming but unhandled packets.
        /// </summary>
        /// <returns>
        ///     The internal buffer size in bytes.
        /// </returns>
        public Int32 GetInternalBufferSize()
        {
            return _BufferSize;
        }
    }
}