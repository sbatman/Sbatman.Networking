#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace InsaneDev.Netwoking.Server
{
    public class Base
    {
        protected static TcpListener TcpListener;
        protected static Thread ListeningThread;
        protected static Thread UpdateThread;
        protected static bool Listening;
        protected static bool Running;
        protected static List<ClientConnection> CurrentlyConnectedClients;
        protected static IPEndPoint TCPLocalEndPoint;
        protected static Type ClientType;

        /// <summary>
        ///     Required to initalise the Server system
        /// </summary>
        /// <param name="tcpLocalEndPoint"> The local point ther server should listen for connections on </param>
        /// <param name="clientType"> A type of type Client that will be instantiated for each connection </param>
        public static void Init(IPEndPoint tcpLocalEndPoint, Type clientType)
        {
            ClientType = clientType;
            CurrentlyConnectedClients = new List<ClientConnection>();
            TCPLocalEndPoint = tcpLocalEndPoint;
        }

        /// <summary>
        ///     Begin the process of listening for incoming connections
        /// </summary>
        public static void StartListening()
        {
            ListeningThread = new Thread(ListenLoop);
            ListeningThread.Start();
        }

        /// <summary>
        ///     Stop listening for incoming connections
        /// </summary>
        public static void StopListening()
        {
            Listening = false;
        }

        private static void ListenLoop()
        {
            TcpListener = new TcpListener(TCPLocalEndPoint);
            TcpListener.Start();
            Listening = true;

            while (Listening)
            {
                while (TcpListener.Pending()) HandelNewConnection(TcpListener.AcceptTcpClient());
                Thread.Sleep(16);
            }
            Listening = false;
        }

        private static void HandelNewConnection(TcpClient newSocket)
        {
            try
            {
                newSocket.NoDelay = true;
                lock (CurrentlyConnectedClients)
                {
                    CurrentlyConnectedClients.Add((ClientConnection) Activator.CreateInstance(ClientType, new object[] {newSocket}));
                    if (!Running)
                    {
                        Running = true;
                        UpdateThread = new Thread(UpdateLoop);
                        UpdateThread.Start();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error creating new client, " + e.Message);
            }
        }

        public static void SendToAll(Packet p)
        {
            List<ClientConnection> d = new List<ClientConnection>();
            lock (CurrentlyConnectedClients) d.AddRange(CurrentlyConnectedClients);
            foreach (ClientConnection c in d) c.SendPacket(p.Copy());

            d.Clear();
            p.Dispose();
        }

        private static void UpdateLoop()
        {
            while (Running)
            {
                List<ClientConnection> d = new List<ClientConnection>();
                lock (CurrentlyConnectedClients)
                {
                    if (CurrentlyConnectedClients.Count == 0)
                    {
                        Running = false;
                        break;
                    }
                    d.AddRange(CurrentlyConnectedClients);
                    foreach (ClientConnection c in d.Where(i => i.Disposed)) CurrentlyConnectedClients.Remove(c);
                    d.Clear();
                }
                Thread.Sleep(50);
            }
        }

        /// <summary>
        ///     Returns true if the the server is listening
        /// </summary>
        /// <returns> </returns>
        public static bool IsListening()
        {
            return Listening;
        }

        /// <summary>
        ///     Returns true if the server is running
        /// </summary>
        /// <returns> </returns>
        public static bool IsRunning()
        {
            return Running;
        }
    }
}