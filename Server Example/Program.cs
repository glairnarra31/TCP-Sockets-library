using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPSockets;

namespace Server_Example
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //instantiate socket
            Sockets sockets = new Sockets("192.168.1.208","23000");

            //socket server events
            sockets.ServerListenStarted += Sockets_ServerListenStarted;
            sockets.ServerListenStopped += Sockets_ServerListenStopped;
            sockets.SocketClosed += Sockets_SocketClosed;

            //client connectivity events
            sockets.ClientConnected += Sockets_ClientConnected;
            sockets.ClientDisconnected += Sockets_ClientDisconnected;

            //socket communication events
            sockets.DataReceived += Sockets_DataReceived;
            sockets.DataSent += Sockets_DataSent;


            //just a loop
            while(true)
            {
                //read command
                switch (Console.ReadLine().ToLower())
                {
                    case "start listen":
                        //start listening for clients
                        sockets.StartListen();
                        break;

                    case "stop listen":
                        //stop listening for clients
                        sockets.StopListen();
                        break;

                    case "close socket":
                        //dipose the socket
                        sockets.Close();
                        break;

                    case "send to":
                        //send to a particular client

                        Console.Write("Enter client remote endpoint: ");
                        string endpoint = Console.ReadLine();

                        Console.WriteLine();

                        Console.Write("Enter message: ");
                        string message = Console.ReadLine();

                        //write data on a particular client endpoint
                        sockets.WriteData(message, endpoint);
                        break;

                    case "broadcast":
                        //broadcast to all connected clients
                        sockets.BroadcastMessage(Console.ReadLine());
                        break;

                    case "exit":
                        return;

                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        private static void Sockets_DataSent(ClientNode node, string message)
        {
            Console.WriteLine($"Data sent to {node.tcpClient.Client.RemoteEndPoint}.");
        }

        private static void Sockets_DataReceived(ClientNode node, string message)
        {
            Console.WriteLine($"{node.tcpClient.Client.RemoteEndPoint} / {node.macAddress}: {message}");
        }

        private static void Sockets_ClientDisconnected(ClientNode node)
        {
            Console.WriteLine($"Client {node.tcpClient.Client.RemoteEndPoint} disconnected.");
        }

        private static void Sockets_ClientConnected(ClientNode node)
        {
            Console.WriteLine($"Client {node.tcpClient.Client.RemoteEndPoint} connected.");
        }

        private static void Sockets_SocketClosed(Sockets sockets)
        {
            Console.WriteLine("Socket Closed");
        }

        private static void Sockets_ServerListenStopped(Sockets sockets)
        {
            Console.WriteLine("Socket Listen Stopped");
        }

        private static void Sockets_ServerListenStarted(Sockets sockets)
        {
            Console.WriteLine("Socket Listen Started");
        }
    }
}
