//
// Created by 张一铭 on 2020/1/27.
// socket通讯的服务端代码
// 首先启动服务端再启动客户端

using System;
using System.Net;
using System.Net.Sockets;


namespace socket_service
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //Socket 创建socket套接字，指定了地址，套接字类型和协议
            Socket listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Bind 给套接字listenfd绑定ip和端口
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 6000);
            listenfd.Bind(ipEp);

            //Listen 开启监听 等待客户端连接
            listenfd.Listen(0);
            Console.WriteLine("服务器开启监听");

            while(true)
            {
                //Accept
                Socket connfd = listenfd.Accept();
                Console.WriteLine("服务器和客户端连接建立");
                //Rccv
                byte[] readBuff = new byte[1024];
                int count = connfd.Receive(readBuff, readBuff.Length,0);
                string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                Console.WriteLine("服务器接收信息：" + str);

                //Send
                str = System.DateTime.Now.ToString();
                byte[] bytes = System.Text.Encoding.Default.GetBytes("serv echo " + str);
                connfd.Send(bytes);
            }
        }
    }
}
