#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace InsaneDev.Networking.Server
{
    public class BaseServer
    {
        /// <summary>
        ///     The type used to generate a clientconnection instance
        /// </summary>
        protected Type _ClientType;

        /// <summary>
        ///     A list of currently connected clients
        /// </summary>
        protected List<ClientConnection> _CurrentlyConnectedClients;

        /// <summary>
        ///     Bool representing whether the server is listening or not
        /// </summary>
        protected bool _Listening;

        /// <summary>
        ///     The thread that the connection listening logic is running on
        /// </summary>
        protected Thread _ListeningThread;

        /// <summary>
        ///     Bool representing whether the server is running or not
        /// </summary>
        protected bool _Running;

        /// <summary>
        ///     The endpoint onwhich the server is reacting to connection requests
        /// </summary>
        protected IPEndPoint _TCPLocalEndPoint;

        /// <summary>
        ///     The underlying TCP listener class
        /// </summary>
        protected TcpListener _TcpListener;

        /// <summary>
        ///     The thread that is used to keep the internal update of the server functioning
        /// </summary>
        protected Thread _UpdateThread;

        /// <summary>
        ///     Required to initalise the Server system
        /// </summary>
        /// <param name="tcpLocalEndPoint"> The local point ther server should listen for connections on </param>
        /// <param name="clientType"> A type of type Client that will be instantiated for each connection </param>
        public void Init(IPEndPoint tcpLocalEndPoint, Type clientType)
        {
            _ClientType = clientType;
            _CurrentlyConnectedClients = new List<ClientConnection>();
            _TCPLocalEndPoint = tcpLocalEndPoint;
        }

        /// <summary>
        ///     Begin the process of listening for incoming connections
        /// </summary>
        public void StartListening()
        {
            _ListeningThread = new Thread(ListenLoop);
            _ListeningThread.Start();
        }

        /// <summary>
        ///     Stop listening for incoming connections
        /// </summary>
        public void StopListening()
        {
            _Listening = false;
        }

        /// <summary>
        ///     When started this logic listens for and reacts to incoming connection requests
        /// </summary>
        private void ListenLoop()
        {
            _TcpListener = new TcpListener(_TCPLocalEndPoint);
            _TcpListener.Start();
            _Listening = true;

            while (_Listening)
            {
                while (_TcpListener.Pending()) HandelNewConnection(_TcpListener.AcceptTcpClient());
                Thread.Sleep(16);
            }
            _Listening = false;
        }

        /// <summary>
        ///     This Handels the new connections
        /// </summary>
        /// <param name="newSocket">The socket the connectionw as made on</param>
        private void HandelNewConnection(TcpClient newSocket)
        {
            newSocket.NoDelay = true;
            lock (_CurrentlyConnectedClients)
            {
                _CurrentlyConnectedClients.Add((ClientConnection) Activator.CreateInstance(_ClientType, new object[] {newSocket}));
                if (!_Running)
                {
                    _Running = true;
                    _UpdateThread = new Thread(UpdateLoop);
                    _UpdateThread.Start();
                }
            }
        }

        /// <summary>
        ///     Sends a packet to all connected clients and then disposes of the packet
        /// </summary>
        /// <param name="p">The packet to send, Withh dispose once sent</param>
        public void SendToAll(Packet p)
        {
            List<ClientConnection> d = new List<ClientConnection>();
            lock (_CurrentlyConnectedClients) d.AddRange(_CurrentlyConnectedClients);
            foreach (ClientConnection c in d) c.SendPacket(p.Copy());

            d.Clear();
            p.Dispose();
        }

        public void Dipose()
        {
            _Running = false;
        }

        /// <summary>
        ///     The internal update loop of the server
        /// </summary>
        private void UpdateLoop()
        {
            while (_Running)
            {
                List<ClientConnection> d = new List<ClientConnection>();
                lock (_CurrentlyConnectedClients)
                {
                    if (_CurrentlyConnectedClients.Count == 0)
                    {
                        _Running = false;
                        break;
                    }
                    d.AddRange(_CurrentlyConnectedClients);
                    foreach (ClientConnection c in d.Where(i => i.IsDisposed())) _CurrentlyConnectedClients.Remove(c);
                    d.Clear();
                }
                Thread.Sleep(50);
            }
            //Time to dispose
            lock (_CurrentlyConnectedClients)
            {
                foreach (ClientConnection client in _CurrentlyConnectedClients)
                {
                    client.Disconnect();
                    client.Dispose();
                }
            }
            _CurrentlyConnectedClients.Clear();
            _CurrentlyConnectedClients = null;
            _ListeningThread = null;
            _TcpListener.Stop();
            _TcpListener = null;
        }

        /// <summary>
        ///     Returns true if the the server is listening
        /// </summary>
        /// <returns> </returns>
        public bool IsListening()
        {
            return _Listening;
        }

        /// <summary>
        ///     Returns true if the server is running
        /// </summary>
        /// <returns> </returns>
        public bool IsRunning()
        {
            return _Running;
        }
    }
}