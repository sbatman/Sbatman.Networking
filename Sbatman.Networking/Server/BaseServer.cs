#region Usings

using Sbatman.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace Sbatman.Networking.Server
{
    public class BaseServer : IDisposable
    {
        /// <summary>
        ///     The type used to generate a clientConnection instance
        /// </summary>
        protected Type _ClientType;

        /// <summary>
        ///     A list of currently connected clients
        /// </summary>
        protected List<ClientConnection> _CurrentlyConnectedClients;

        /// <summary>
        ///     Bool representing whether the server is listening or not
        /// </summary>
        protected Boolean _Listening;

        /// <summary>
        ///     The thread that the connection listening logic is running on
        /// </summary>
        protected Thread _ListeningThread;

        /// <summary>
        ///     Bool representing whether the server is running or not
        /// </summary>
        protected Boolean _Running;

        /// <summary>
        ///     The endpoint on which the server is reacting to connection requests
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
        ///     Required to initialise the Server system
        /// </summary>
        /// <param name="tcpLocalEndPoint"> The local point the server should listen for connections on </param>
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
                if (_TcpListener == null) break;
                while (_TcpListener.Pending()) HandelNewConnection(_TcpListener.AcceptTcpClient());
                Thread.Sleep(16);
            }
            _Listening = false;
        }

        /// <summary>
        ///     This Handles the new connections
        /// </summary>
        /// <param name="newSocket">The socket the connection was made on</param>
        private void HandelNewConnection(TcpClient newSocket)
        {
            newSocket.NoDelay = true;
            lock (_CurrentlyConnectedClients)
            {
                newSocket.NoDelay = true;
                _CurrentlyConnectedClients.Add((ClientConnection)Activator.CreateInstance(_ClientType, this, newSocket));
                if (_Running) return;
                _Running = true;
                _UpdateThread = new Thread(UpdateLoop);
                _UpdateThread.Start();
            }
        }

        /// <summary>
        ///     Sends a packet to all connected clients and then disposes of the packet
        /// </summary>
        /// <param name="p">The packet to send, With dispose once sent</param>
        public void SendToAll(Packet p)
        {
	        if (_CurrentlyConnectedClients == null) return;
            List<ClientConnection> d = new List<ClientConnection>();

            lock (_CurrentlyConnectedClients)
            {
                d.AddRange(_CurrentlyConnectedClients);
            }

            foreach (ClientConnection c in d)
            {
                if (c == null || p == null) continue;
                c.SendPacket(p.Copy());
            }

            d?.Clear();
            p?.Dispose();
        }

        public void Dispose()
        {
            _Listening = false;
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
                    d.AddRange(_CurrentlyConnectedClients);
                    foreach (ClientConnection c in d.Where(i => i == null || i.Disposed)) _CurrentlyConnectedClients.Remove(c);
                    d.Clear();
                }
                Thread.Sleep(2);
            }

            Stop();
        }

		/// <summary>
        /// Stop ths server disconnecting all clients
        /// </summary>
        public void Stop()
        {
	        _Running = false;
	        _Listening = false;
            //Time to dispose
            if (_CurrentlyConnectedClients != null)
            {
	            lock (_CurrentlyConnectedClients)
	            {
		            foreach (ClientConnection client in _CurrentlyConnectedClients)
		            {
			            client.Disconnect();
			            client.Dispose();
		            }
	            }
	            _CurrentlyConnectedClients.Clear();
            }

            _CurrentlyConnectedClients = null;
	        _ListeningThread = null;
	        _TcpListener.Stop();
	        _TcpListener = null;
        }

        /// <summary>
        ///     Returns true if the the server is listening
        /// </summary>
        /// <returns> </returns>
        public Boolean IsListening()
        {
            return _Listening;
        }

        /// <summary>
        ///     Returns true if the server is running
        /// </summary>
        /// <returns> </returns>
        public Boolean IsRunning()
        {
            return _Running;
        }
    }
}