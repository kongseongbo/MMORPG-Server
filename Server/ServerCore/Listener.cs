using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Listener
    {
        Socket listenSocket;
        Action<Socket> onAcceptHandler;

        public void Init(IPEndPoint _endPoint, Action<Socket> _onAcceptHandler)
        {
            listenSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            onAcceptHandler += _onAcceptHandler;

            // 문지기 교육
            // port 번호 입력
            listenSocket.Bind(_endPoint);

            // 영업 시작
            int backing = 10; // 최대 대기수
            listenSocket.Listen(backing);

            // 비동기 방식
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);

            // 매우 많은 수의 인원이 동시 다발적으로 접속해야한다고 가정을 하면
            // for문으로 묶어서 처리하면 RegisterAccept()의 혹시 모를 스택오버플로우를 예방할 수 있다.
            //for (int i = 0; i < backing; i++)
            //{
            //    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //    args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            //    RegisterAccept(args);
            //}
        }

        private void RegisterAccept(SocketAsyncEventArgs _args)
        {
            _args.AcceptSocket = null;

            // pending == false : Accept 성공 
            bool pending = listenSocket.AcceptAsync(_args);
            if (pending == false)
            {
                OnAcceptCompleted(null, _args);
            }
        }

        // 레드존
        // 항상 멀티쓰레드로 실행이될 수 있다는걸 생각해야 한다.
        private void OnAcceptCompleted(object _sender, SocketAsyncEventArgs _args)
        {
            if(_args.SocketError == SocketError.Success)
            {
                onAcceptHandler.Invoke(_args.AcceptSocket);
            }
            else
            {
                Console.WriteLine(_args.SocketError.ToString());
            }

            // 다음에 접속할 클라를 위해서 다시 등록
            RegisterAccept(_args);
        }
    }
}
