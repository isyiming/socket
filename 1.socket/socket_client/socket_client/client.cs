//
// Created by 张一铭 on 2020/1/27.
// socket通讯的客户端代码
// 首先启动服务端再启动客户端

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace socket_client
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //Socket
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Connect
            IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
            socket.Connect(ipAdr, 6000);
            Console.WriteLine("客户端启动!" + socket.LocalEndPoint.ToString());

            //send
            string str = "hellow service!";
            byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
            socket.Send(bytes);

            //recieve
            byte[] readBuff = new byte[1024];
            int count = socket.Receive(readBuff , readBuff.Length, 0);
            str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);

            Console.WriteLine(str);
            //close
            socket.Close();
        }
    }
}
