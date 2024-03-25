using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Session
    {
        Socket socket;
        int disconnected = 0;

        // 재사용을 위해 맴버변수로 선언 및 생성
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();

        List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();

        Queue<byte[]> sendQueue = new Queue<byte[]>();
        
        object _lock = new object();


        public void Start(Socket _socket)
        {
            socket = _socket;  
   
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            // 빈 버퍼를 연결만 해준 후 클라이언트가 데이터를 서버로 보내면 연결하둔 버퍼에 저장된다.
            recvArgs.SetBuffer(new byte[1024], 0, 1024);

            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(byte[] _sendBuff)
        {
            lock (_lock)
            {
                // send가 완료되기 전까지는 sendQueue에 저장 
                sendQueue.Enqueue(_sendBuff);
                if (pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            // 동시에 끊어서 생기는 오류 방지
            // Interlocked.Exchange(A, B) : A를 B로 바꾸고 원래의 A를 반환
            // 다른 쓰레드가 A값을 바꾼 상태에서 B값이 A를 덮어씌우는 경우를 막을 때 사용
            if (Interlocked.Exchange(ref disconnected, 1) == 1)
                return;

            // 쫓아낸다.
            socket.Shutdown(SocketShutdown.Both); // Send, Recv를 하지 않겠다는 것을 명시함
            socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            // sendQueue에 있는 모든 패킷을 한번에 보냄
            while (sendQueue.Count > 0)
            {
                // 빈 버퍼가 아니라 서버에서 클라이언트로 보낼 데이터가 있는 버퍼 및 버퍼의 길이를 넣어서 보낸다.
                byte[] buff = sendQueue.Dequeue();
                // c++ 과 달리 포인터를 사용할수없어 배열의 첫번째 주소만 알수있어서 몇번째부터 사용할것인지를 index로 따로 넘겨줘야한다.
                // sendArgs.BufferList.Add(new ArraySegment<byte>(buff, index, buff.Length))
                pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
            }
            // BufferList에 값을 넣을때는 List를 따로 만들어서 넣어줘야한다.
            sendArgs.BufferList = pendingList;

            bool _pending = socket.SendAsync(sendArgs);
            if (_pending == false)
            {
                OnSendCompleted(null, sendArgs);
            }
        }

        void OnSendCompleted(object _sender, SocketAsyncEventArgs _agrs)
        {
            lock (_lock)
            {
                if (_agrs.BytesTransferred > 0 && _agrs.SocketError == SocketError.Success)
                {
                    try
                    {
                        // 성공적으로 Send 했을 경우 다음 작업을 위해 초기화
                        sendArgs.BufferList = null;
                        pendingList.Clear();

                        Console.WriteLine($"Transferred bytes : {sendArgs.BytesTransferred}");

                        // sendQueue를 확인 후 보낼 정보가 있다면 처리
                        if (sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            bool _pending = socket.ReceiveAsync(recvArgs);
            if (_pending == false)
            {
                OnRecvCompleted(null, recvArgs);
            }
        }

        void OnRecvCompleted(object _sender, SocketAsyncEventArgs _agrs)
        {
            if(_agrs.BytesTransferred > 0 && _agrs.SocketError == SocketError.Success)
            {
                try
                {
                    string recvData = Encoding.UTF8.GetString(_agrs.Buffer, _agrs.Offset, _agrs.BytesTransferred);
                    Console.WriteLine($"[From Client] {recvData}");

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }
        #endregion
    }
}
