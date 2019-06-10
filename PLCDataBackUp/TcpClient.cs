using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace PLCDataBackUp
{
    /// <summary>
    /// Socket非同期通信　クライアントクラス
    /// </summary>
    class TcpClient : IDisposable
    {
        /** プライベート変数 **/
        //Socket
        private Socket mySocket = null;

        //受信データ保存用
        private MemoryStream myMs;

        //ロック用
        private readonly object syncLock = new object();

        //送受信文字列エンコード
        private Encoding enc = Encoding.UTF8;


        /** イベント **/
        //データ受信イベント
        public delegate void ReceiveEventHandler(object sender, string e);
        public event ReceiveEventHandler OnReceiveData;

        //接続断イベント
        public delegate void DisconnectedEventHandler(object sender, EventArgs e);
        public event DisconnectedEventHandler OnDisconnected;

        //接続OKイベント
        public delegate void ConnectedEventHandler(EventArgs e);
        public event ConnectedEventHandler OnConnected;


        /** プロパティ **/
        /// <summary>
        /// ソケットが閉じているか
        /// </summary>
        public bool IsClosed
        {
            get { return (mySocket == null); }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            //Socketを閉じる
            Close();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TcpClient()
        {
            //Socket生成
            mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public TcpClient(Socket sc)
        {
            mySocket = sc;
        }

        /// <summary>
        /// SocketClose
        /// </summary>
        public void Close()
        {
            //Socketを無効
            mySocket.Shutdown(SocketShutdown.Both);
            //Socketを閉じる
            mySocket.Close();
            mySocket = null;

            //受信データStreamを閉じる
            if (myMs != null)
            {
                myMs.Close();
                myMs = null;
            }

            //接続断イベント発生
            OnDisconnected(this, new EventArgs());
        }

        /// <summary>
        /// Hostに接続
        /// </summary>
        /// <param name="host">接続先ホスト</param>
        /// <param name="port">ポート</param>
        public void Connect(string host, int port)
        {
            // ホスト名からIPアドレスを取得する IPv4
            IPAddress[] ServerIP = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress[] IPS = Dns.GetHostAddresses(host);

            foreach (IPAddress ip in IPS)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    ServerIP[0] = ip;
            }

            int portNo = port;
            IPEndPoint ipEnd = new IPEndPoint(ServerIP[0], portNo);
            
            //ホストに接続
            mySocket.Connect(ipEnd);

            //接続OKイベント発生
            OnConnected(new EventArgs());
            //データ受信開始
            StartReceive();
        }

        /// <summary>
        /// データ受信開始
        /// </summary>
        public void StartReceive()
        {
            //受信バッファ
            byte[] rcvBuff = new byte[1024];
            //受信データ初期化
            myMs = new MemoryStream();

            //非同期データ受信開始
            mySocket.BeginReceive(rcvBuff, 0, rcvBuff.Length, SocketFlags.None, new AsyncCallback(ReceiveDataCallback), rcvBuff);
        }

        /// <summary>
        /// 非同期データ受信
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataCallback(IAsyncResult ar)
        {
            int len = -1;
            lock (syncLock)
            {
                if (IsClosed)
                    return;

                //データ受信終了
                len = mySocket.EndReceive(ar);
            }

            //切断された
            if (len <= 0)
            {
                Close();
                return;
            }

            //受信データ取り出し
            byte[] rcvBuff = (byte[])ar.AsyncState;
            //受信データ保存
            myMs.Write(rcvBuff, 0, len);
             
           
            
           

            // string型文字列へ変換
            string hexData2 = BytesToHexString(rcvBuff, len);

            Console.WriteLine("受信完了!");
            //Console.WriteLine("{0}\n", receiveString);
            Console.WriteLine("{0}\n", hexData2);


            //受信データ初期化
            myMs.Close();
            myMs = new MemoryStream();

            //データ受信イベント発生
            OnReceiveData(this, hexData2);

            lock (syncLock)
            {
                //非同期受信を再開始
                if (!IsClosed)
                    mySocket.BeginReceive(rcvBuff, 0, rcvBuff.Length, SocketFlags.None, new AsyncCallback(ReceiveDataCallback), rcvBuff);
            }
        }

        /// <summary>
        /// メッセージを送信する
        /// </summary>
        /// <param name="str"></param>
        public void Send(string str)
        {
            if (!IsClosed)
            {
                // 「byte[]」配列へ変換 
                byte[] byteData = HexStringToBytes(str);
                
                lock (syncLock)
                {
                    //送信
                    mySocket.Send(byteData);
                }
            }
        }

        /// <summary>
        /// BYTEデータを送信する
        /// </summary>
        /// <param name="b"></param>
        public void SendByte(byte[] b)
        {
            if (!IsClosed)
            {
                lock (syncLock)
                {
                    //送信
                    mySocket.Send(b);
                }
            }
        }

        // バイト列を16進数表記の文字列に変換
        public static string BytesToHexString(byte[] bytes, int length)
        {
            StringBuilder sb = new StringBuilder();
            //for (int i = 0; i < bytes.Length; i++)
            for (int i = 0; i < length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        // 16進数表記の文字列をバイト列に変換
        public static byte[] HexStringToBytes(string byteString)
        {
            // 文字列の文字数(半角)が奇数の場合、頭に「0」を付ける

            int length = byteString.Length;
            if (length % 2 == 1)
            {
                byteString = "0" + byteString;
                length++;
            }

            List<byte> data = new List<byte>();

            for (int i = 0; i < length - 1; i = i + 2)
            {
                // 16進数表記の文字列かどうかをチェック
                string buf = byteString.Substring(i, 2);
                if (Regex.IsMatch(buf, @"^[0-9a-fA-F]{2}$"))
                {
                    data.Add(Convert.ToByte(buf, 16));
                }
                // // 16進数表記で無ければ「00」とする
                else
                {
                    data.Add(Convert.ToByte("00", 16));
                }
            }

            return data.ToArray();
        }

    }
}
