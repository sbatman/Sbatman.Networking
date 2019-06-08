﻿using System;
using System.Net;
using System.Threading;
using Sbatman.Networking.Server;

namespace Example_Server
{
    class Program
    {
        private static readonly Object _LockingObject = new Object();
        static void Main()
        {
            BaseServer server = new BaseServer();//Create an instance of the server

            //Prepare to Listen for connections on port 6789 arriving on all addresses belonging to this machine,
            //If there is a connection accept it and create a new instance of ConnectedClient to pass it to.
            server.Init(new IPEndPoint(IPAddress.Any, 6789), typeof(ConnectingClient)); 

            server.StartListening();//Begin listening for connections

            while (true) Thread.Sleep(5);//Keep ticking over
        }

        /// <summary>
        /// ClientConnections are Threaded so calls from it to this function need to be thread safe
        /// </summary>
        /// <param name="s">The string to write to console</param>
        internal static void Write(String s)
        {
           lock (_LockingObject) Console.WriteLine(s);
        }
    }
}
