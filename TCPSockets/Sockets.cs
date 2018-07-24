using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TCPSockets
{
    public class Sockets
    {
        private readonly TcpListener tcpListener;

        private readonly List<ClientNode> clients = new List<ClientNode>();

        private bool listenerFlag = false;

        //events delegates
        public delegate void SocketStatusEventHandler(Sockets sockets);
        public delegate void TCPClientConnectivityEventHandler(ClientNode node);
        public delegate void TCPClientCommunicationEventHandler(ClientNode node, string message);

        //client events
        public event SocketStatusEventHandler ServerListenStarted;
        public event SocketStatusEventHandler ServerListenStopped;
        public event SocketStatusEventHandler SocketClosed;

        public event TCPClientConnectivityEventHandler ClientConnected;
        public event TCPClientConnectivityEventHandler ClientDisconnected;

        public event TCPClientCommunicationEventHandler DataReceived;
        public event TCPClientCommunicationEventHandler DataSent;

        public Sockets(string ipAddress = "127.0.0.1", string port = "23000")
        {
            if (int.TryParse(port, out int nport) && IPAddress.TryParse(ipAddress, out IPAddress iPAddress))
            {
                tcpListener = new TcpListener(iPAddress, nport);
            }
            else
            {
                throw new Exception("Unable to parse IP Address and/or Port");
            }
        }

        public void StartListen()
        {
            if (!listenerFlag)
                tcpListener.Start();
            else
                throw new Exception("Unable to start, server already started!");
            listenerFlag = true;
            tcpListener.BeginAcceptTcpClient(OnCompleteAcceptClient, tcpListener);
            OnServerListenStarted();
        }

        public void StopListen()
        {
            listenerFlag = false;
            tcpListener.Stop();
            OnServerListenStopped();
        }

        public void Close()
        {
            listenerFlag = false;
            foreach (var client in clients)
            {
                lock(clients)
                {
                    client.tcpClient.Dispose();
                }
                OnClientDisconnected(client);
            }
            tcpListener.Stop();
            OnServerListenStopped();
            OnSocketClosed();
        }

        private void OnCompleteAcceptClient(IAsyncResult asyncResult)
        {
            try
            {
                if (listenerFlag)
                {
                    ClientNode clientNode = new ClientNode(tcpListener.EndAcceptTcpClient(asyncResult));
                    lock (clients)
                    {
                        clients.Add(clientNode);
                    }
                    OnClientConnected(clientNode);

                    clientNode.tcpClient.GetStream().BeginRead(clientNode.RX, 0, clientNode.RX.Length, ReceiveData, clientNode.tcpClient);

                    tcpListener.BeginAcceptTcpClient(OnCompleteAcceptClient, tcpListener);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        private void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                ClientNode clientNode = clients.Find(x => x.tcpClient == (TcpClient)asyncResult.AsyncState);
                int toReadBytes = clientNode.tcpClient.GetStream().EndRead(asyncResult);

                if (toReadBytes == 0)
                {
                    lock(clients)
                    {
                        clientNode.tcpClient.Dispose();
                        clients.Remove(clientNode);
                    }
                    OnClientDisconnected(clientNode);
                }
                else
                {
                    OnDataReceived(clientNode,Encoding.ASCII.GetString(clientNode.RX, 0, toReadBytes).Trim());
                    clientNode.tcpClient.GetStream().BeginRead(clientNode.RX, 0, clientNode.RX.Length, ReceiveData, clientNode.tcpClient);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void RemoveClient(ClientNode clientNode)
        {
            try
            {
                ClientNode client = clients.Find(x => x == clientNode);
                if (clientNode != null)
                {
                    lock(clients)
                    {
                        clientNode.tcpClient.Dispose();
                        clients.Remove(clientNode);
                    }
                    OnClientDisconnected(clientNode);
                }
                else
                {
                    throw new Exception($"Client {clientNode.tcpClient.Client.RemoteEndPoint} not found.");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        public void RemoveClient(string endpoint)
        {
            try
            {
                ClientNode clientNode = clients.Find(x => x.tcpClient.Client.RemoteEndPoint.ToString() == endpoint);
                if (clientNode != null)
                {
                    lock(clients)
                    {
                        clientNode.tcpClient.Dispose();
                        clients.Remove(clientNode);
                    }
                    OnClientDisconnected(clientNode);
                }
                else
                {
                    throw new Exception($"Client {endpoint} not found.");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        public void WriteData(string message, ClientNode clientNode)
        {
            try
            {
                if (clientNode.tcpClient.Connected)
                {
                    clientNode.TX = Encoding.ASCII.GetBytes(message);
                    clientNode.tcpClient.GetStream().BeginWrite(clientNode.TX, 0, clientNode.TX.Length, OnCompleteWrite, clientNode.tcpClient);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        public void WriteData(string message, string endpoint)
        {
            try
            {
                ClientNode clientNode = clients.Find(x => x.tcpClient.Client.RemoteEndPoint.ToString() == endpoint);
                if (clientNode != null)
                {
                    if (clientNode.tcpClient.Connected)
                    {
                        clientNode.TX = Encoding.ASCII.GetBytes(message);
                        clientNode.tcpClient.GetStream().BeginWrite(clientNode.TX, 0, clientNode.TX.Length, OnCompleteWrite, clientNode.tcpClient);
                    }
                }
                else
                {
                    throw new Exception($"Client {endpoint} not found.");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        public void BroadcastMessage(string message)
        {
            try
            {
                foreach (ClientNode client in clients)
                {
                    if (client.tcpClient.Connected)
                    {
                        client.TX = Encoding.ASCII.GetBytes(message);
                        client.tcpClient.GetStream().BeginWrite(client.TX, 0, client.TX.Length, OnCompleteWrite, client.tcpClient);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        private void OnCompleteWrite(IAsyncResult ar)
        {
            try
            {
                TcpClient tcpClient = (TcpClient)ar.AsyncState;
                tcpClient.GetStream().EndWrite(ar);
                OnDataSent(clients.Find(x => x.tcpClient == tcpClient), string.Empty);
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }

        public List<ClientNode> GetClientNodes()
        {
            return this.clients;
        }

        //invokes
        protected virtual void OnServerListenStarted()
        {
            ServerListenStarted?.Invoke(this);
        }

        protected virtual void OnServerListenStopped()
        {
            ServerListenStopped?.Invoke(this);
        }

        protected virtual void OnSocketClosed()
        {
            SocketClosed?.Invoke(this);
        }

        protected virtual void OnClientConnected(ClientNode clientNode)
        {
            ClientConnected?.Invoke(clientNode);
        }

        protected virtual void OnClientDisconnected(ClientNode clientNode)
        {
            ClientDisconnected?.Invoke(clientNode);
        }

        protected virtual void OnDataReceived(ClientNode node, string message)
        {
            DataReceived?.Invoke(node, message);
        }

        protected virtual void OnDataSent(ClientNode node, string message)
        {
            DataSent?.Invoke(node, message);
        }
    }
}
