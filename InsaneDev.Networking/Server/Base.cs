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
    public class Base
    {
        protected TcpListener _TcpListener;
        protected Thread _ListeningThread;
        protected Thread _UpdateThread;
        protected bool _Listening;
        protected bool _Running;
        protected List<ClientConnection> _CurrentlyConnectedClients;
        protected IPEndPoint _TCPLocalEndPoint;
        protected Type _ClientType;

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

        private void HandelNewConnection(TcpClient newSocket)
        {
            try
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
            catch (Exception e)
            {
                Console.WriteLine("Error creating new client, " + e.Message);
            }
        }

        public void SendToAll(Packet p)
        {
            List<ClientConnection> d = new List<ClientConnection>();
            lock (_CurrentlyConnectedClients) d.AddRange(_CurrentlyConnectedClients);
            foreach (ClientConnection c in d) c.SendPacket(p.Copy());

            d.Clear();
            p.Dispose();
        }

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
                    foreach (ClientConnection c in d.Where(i => i.Disposed)) _CurrentlyConnectedClients.Remove(c);
                    d.Clear();
                }
                Thread.Sleep(50);
            }
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