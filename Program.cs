using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        static Thread listenThread;//поток для прослушивания
        static void Main(string[] args)
        {
            Server server = new Server();
            listenThread = new Thread(new ThreadStart(server.Listen));
            listenThread.Start();
        }
    }

    class Server
    {
        public IPAddress iPAddress;
        public IPEndPoint iPEndPoint;
        public Socket listener;
        public static List<Client> clients = new List<Client>();//список подключений
        public string host;
        public Server() // конструктор класса Server
        {
            host = Dns.GetHostName();
            iPAddress = Dns.GetHostByName(host).AddressList[0];
            iPEndPoint = new IPEndPoint(iPAddress, 11000);
            listener = new Socket(iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(iPEndPoint);
            listener.Listen(10);
        }

        public void Listen()
        {
            while (true)
            {
                Console.WriteLine("Ожидаем соединение через порт {0}", iPEndPoint);
                Socket handler = listener.Accept();
                Client client = new Client
                {
                    socket = handler
                };
                clients.Add(client);
                IPEndPoint iP = (IPEndPoint)handler.RemoteEndPoint;
                Console.WriteLine(iP.Address);
                Thread clientThread = new Thread(new ThreadStart(client.process));
                clientThread.Start();
            }
        }   
    }

    class Client//Класс клиент
    {
        public Socket socket;
        string username;
        string id;

        public void process()
        {
            try
            {
                byte[] bytes = new byte[1024];
                int byteRec = socket.Receive(bytes);
                username = Encoding.UTF8.GetString(bytes, 0, byteRec);
                id = Guid.NewGuid().ToString();
                string reply = username.Trim() + " подключился.\n";
                Console.WriteLine(reply);
                byte[] msg = Encoding.UTF8.GetBytes(reply);
                foreach (var cl in Server.clients)
                {

                    cl.socket.Send(msg);
                }
                while (true)
                {
                    try
                    {
                        byte[] reciver = new byte[1024];
                        int size = socket.Receive(reciver);
                        string txt = Encoding.UTF8.GetString(reciver, 0, size);
                        foreach (var cl in Server.clients)
                        {
                            string message = username.Trim() + ": " + txt;
                            cl.socket.Send(Encoding.UTF8.GetBytes(message));
                        }
                    }
                    catch
                    {
                        reply = username.Trim() + " покинул чат.\n";
                        msg = Encoding.UTF8.GetBytes(reply);
                        foreach (var cl in Server.clients)
                        {
                            if (this.id != cl.id)
                            {
                                cl.socket.Send(msg);
                            }
                        }
                        socket.Shutdown(SocketShutdown.Both);
                        Server.clients.Remove(this);
                        break; 
                    }
                }
            }
            catch
            {
                Console.WriteLine("Ошибка");
            }
        }
    }  
}
