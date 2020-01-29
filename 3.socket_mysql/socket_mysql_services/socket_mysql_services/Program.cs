using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace socket_mysql_services
{
    //保存所有客户端的连接状态
    public class Conn
    {
        public const int BUFFER_SIZE = 1024;
        //socket
        public Socket socket;
        //是否使用的标志变量
        public bool isUse = false;
        //Buff
        public byte[] readBuff = new byte[BUFFER_SIZE];
        public int buffCount = 0;
        //构造函数
        public Conn()
        {
            readBuff = new byte[BUFFER_SIZE];
        }
        //初始化
        public void Init(Socket socket)
        {
            this.socket = socket;
            isUse = true;
            buffCount = 0;
        }
        //缓冲区剩余的字节数
        public int BuffRemain()
        {
            return BUFFER_SIZE - buffCount;
        }
        //获取客户端地址
        public string GetAdress()
        {
            if (!isUse)
            {
                return "无法获取地址";
            }
            return socket.RemoteEndPoint.ToString();

        }
        //断开连接
        public void Close()
        {
            if (!isUse)
            {
                return;
            }
            Console.WriteLine("[断开连接]" + GetAdress());
            socket.Close();
            isUse = false;
        }
    }

    public class Serv
    {
        //数据库
        MySqlConnection sqlConn;

        //监听套接字
        public Socket listenfd;
        //客户端连接
        public Conn[] conns;
        //最大连接数
        public int maxConn = 50;

        //获取连接池索引，返回负数表示获取失败
        public int NewIndex()
        {
            if (conns == null)
            {
                return -1;
            }
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (conns[i].isUse == false)
                {
                    return i;
                }
            }
            return -1;
        }

        //开启服务器
        public void Start(string host, int port)
        {
            //数据库 Database为在mysql中建立的数据库 
            string connStr = "Database=Notepad;Data Source=127.0.0.1;";
            connStr += "User Id=root;password=000000000a;port=3306";

            sqlConn = new MySqlConnection(connStr);

            try
            {
                sqlConn.Open();
                Console.WriteLine("[数据库]启动成功");
            }
            catch (Exception e)
            {
                Console.WriteLine("[数据库]启动失败"+e.Message);
                return;
            }

            //连接池
            conns = new Conn[maxConn];
            for (int i = 0; i < maxConn; i++)
            {
                conns[i] = new Conn();
            }

            //Socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Bind
            IPAddress iPAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(iPAdr, port);
            listenfd.Bind(ipEp);

            //Listne
            listenfd.Listen(maxConn);

            //Accept
            listenfd.BeginAccept(AcceptCb, null);
            Console.WriteLine("[服务器]启动成功");
        }

        //accept回调函数
        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                Socket socket = listenfd.EndAccept(ar);
                int index = NewIndex();

                if (index < 0)
                {
                    socket.Close();
                    Console.WriteLine("[警告]连接已满");
                }
                else
                {
                    Conn conn = conns[index];
                    conn.Init(socket);
                    string adr = conn.GetAdress();
                    Console.WriteLine("客户端连接[" + adr + "] conn池 ID: " + index);
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
                }
                listenfd.BeginAccept(AcceptCb, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("AcceptCb 失败" + e.Message);
            }
        }
        private void ReceiveCb(IAsyncResult ar)
        {
            Conn conn = (Conn)ar.AsyncState;
            try
            {
                int count = conn.socket.EndReceive(ar);

                //关闭连接
                if (count <= 0)
                {
                    Console.WriteLine("收到 [" + conn.GetAdress() + " ]断开连接");
                    conn.Close();
                    return;
                }
                //数据处理
                string str = System.Text.Encoding.UTF8.GetString(conn.readBuff, 0, count);
                Console.WriteLine("收到 [" + conn.GetAdress() + " ] 数据" + str);

                //数据库处理
                HandleMsg(conn, str);

                str = conn.GetAdress() + ":" + str;
                byte[] bytes = System.Text.Encoding.Default.GetBytes(str);

                //广播
                for (int i = 0; i < conns.Length; i++)
                {
                    if (conns[i] == null)
                    {
                        continue;
                    }
                    if (!conns[i].isUse)
                    {
                        continue;
                    }
                    Console.WriteLine("将消息传播给 " + conns[i].GetAdress());
                    Console.WriteLine("-----------------------------------");
                    //conns[i].socket.Send(bytes);
                }
                //继续接收
                conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
            }
            catch (Exception e)
            {
                Console.WriteLine("收到 【" + conn.GetAdress() + "] 断开连接" + e.Message);
                conn.Close();
            }
        }
        public void HandleMsg(Conn conn, string str)
        {
            if(str == "_SHOW_ME_MESSAGE")
            {
                string cmdStr = "select * from msg order by id desc limit 10;";
                MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
                try
                {
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    str = "";
                    while(dataReader.Read())
                    {
                        str += dataReader["name"] + ":" + dataReader["msg"] + "\n\r";
                    }
                    dataReader.Close();
                    byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
                    conn.socket.Send(bytes);
                }
                catch(Exception e)
                {
                    Console.WriteLine("【数据库】查询失败" + e.Message);
                }
            }
            else
            {
                string cmdStrFormat = "insert into msg set id=1 ,name ='{0}' ,msg ='{1}';";
                string cmdStr = string.Format(cmdStrFormat, conn.GetAdress(), str);
                MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception e)
                {
                    Console.WriteLine("【数据库】插入失败" + e.Message);
                }
            }
        }
    }


    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Serv serv = new Serv();
            serv.Start("127.0.0.1", 1234);

            while (true)
            {
                string str = Console.ReadLine();
                switch (str)
                {
                    case "quit":
                        return;
                }
            }
        }
    }
}
