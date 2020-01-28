using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace socket_client_Asynchronous
{
    class Client
    {
        Socket socket;
        const int BUFFER_SIZE = 1024;
        public byte[] readBuff = new byte[BUFFER_SIZE];
        string recvText = "";
        string recvStr = "";

        //连接
        public void Connetion()
        {
            //清理text
            recvText = "";

            //socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //connect
            socket.Connect("127.0.0.1", 1234);

            //recv
            socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
        }

        //接收回调函数
        private void ReceiveCb(IAsyncResult ar)
        {
            try
            {
                //count
                int count = socket.EndReceive(ar);

                //数据处理
                string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                if (recvStr.Length > 300)
                {
                    recvStr = "";
                }
                recvStr += str + "\n";

                //继续接收
                socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
            }
            catch (Exception e)
            {
                recvText += "连接已经断开" + e.Message;
                socket.Close();
            }
        }

        //发送数据
        public void Send(string str)
        {
            byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
            try
            {
                socket.Send(bytes);
            }
            catch
            {

            }
        }
    }

    class MainClass
    {
 
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Client client = new Client();
            client.Connetion();
            //等待键盘输入字符串，发送给服务端
            string str = Console.ReadLine();
            client.Send(str);
        }
    }
}
