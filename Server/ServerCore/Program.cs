using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerCore
{
    class Program
    {
        static Listener listener = new Listener();

        static void OnAcceptHandler(Socket _clientSocket)
        {
            try
            {
                Session session = new Session();
                session.Start(_clientSocket);

                // 보낸다
                byte[] sendBuffer = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                session.Send(sendBuffer);

                Thread.Sleep(1000);

                session.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            // 도메인을 등록해서 ip 주소를 알아냄
            // www.naver.com -> 123.123.123.12
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint EndPoint = new IPEndPoint(ipAddr, 7777); // 최종 Ip 주소 

   
            listener.Init(EndPoint, OnAcceptHandler);
            Console.WriteLine("Listening...");

            while (true)
            {
             
            }
        }
    }
}
