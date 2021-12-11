using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PLCDataBackUp
{

    public partial class plcconnect : Form
    {

        const string ContinuityRead =  "0104"; //PLC 一括読み出しコマンド
        const string ContinuityWrite = "0114"; //PLC 一括書き込みコマンド
        const string RandomRead =      "0304"; //PLC ランダム読み出しコマンド 
        const string RandomWrite =     "0214"; //PLC ランダム書き込みコマンド
        const string DeviceCode = "A8"; //Dアドレス
        const int MaxAddres = 11135; //Qシリーズで扱える最大のDアドレス
        const int read_data_Position = 22;  //応答データ部位置　P68
        const int response_data_length_Position = 14; //応答データ長位置
        const int end_code_Position = 18; //終了コード位置


        TcpClient tClient = new TcpClient();//Socketクライアント

        RandomReadPlcSend randomReadPlcSend = new RandomReadPlcSend();
        RandomWritePlcSend randomWritePlcSend = new RandomWritePlcSend();
        ContinuityReadPlcSend continuityReadPlcSend = new ContinuityReadPlcSend();
        ContinuityWritePlcSend continuityWritePlcSend = new ContinuityWritePlcSend();
        int SendCount = 0; //送信データカウント
        string StartTime;
        string SendCommand = "";
        Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
        string[] lines; //書き込み用の読み込みデータ
        int lines_send_count = 0; //書き込み用送信のカウント
        DateTime dt1;
        Boolean elapsedTimeSet;//終了時間判定

        List<string> ReciveDataBufffer = new List<string>(); //受信データ用List
        List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>();//1回の読み出しコマンドで受信したアドレスとデータのList
        List<string> ReciveDatas = new List<string>();
        List<string> PlcSendBuffer = new List<string>(); //コマンド伝文用List

        public plcconnect()
        {
            InitializeComponent();

            //接続OKイベント
            tClient.OnConnected += new TcpClient.ConnectedEventHandler(tClient_OnConnected);
            //接続断イベント
            tClient.OnDisconnected += new TcpClient.DisconnectedEventHandler(tClient_OnDisconnected);
            //データ受信イベント
            tClient.OnReceiveData += new TcpClient.ReceiveEventHandler(tClient_OnReceiveData);
        }

        /** 接続断イベント **/
        void tClient_OnDisconnected(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke(new DisconnectedDelegate(Disconnected), new object[] { sender, e });
        }
        delegate void DisconnectedDelegate(object sender, EventArgs e);
        private void Disconnected(object sender, EventArgs e)
        {
            //接続断処理
        }


        /** 接続OKイベント **/
        void tClient_OnConnected(EventArgs e)
        {
            //接続OK処理
            textBox10.Text = "Com OK";
            //Debug用通信データ
            DateTime dt = DateTime.Now;
            StartTime = dt.ToString("yyyy-MM-dd_HH-mm-ss");
            DebugText(StartTime);
        }

        /** 接続ボタンを押して接続処理 **/
        private void btn_Connect_Click(object sender, EventArgs e)
        {
            if (txt_Host.Text.Length == 0 || txt_Port.Text.Length == 0)
                return;
            try
            {
                //接続先ホスト名
                string host = txt_Host.Text;
                //接続先ポート
                int port = int.Parse(txt_Port.Text);
                //接続処理
                tClient.Connect(host, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        /** データ受信イベント **/
        void tClient_OnReceiveData(object sender, string e)
        {
            //別スレッドからくるのでInvokeを使用
            if (this.InvokeRequired)
                this.Invoke(new ReceiveDelegate(ReceiveData), new object[] { sender, e });
        }
        delegate void ReceiveDelegate(object sender, string e);

        /// <summary>
        /// データ受信処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveData(object sender, string e)
        {
            ReciveDataBufffer.Add(e);//受信データのList作成
            DebugText(e);
            PlcDataSend();//次のデータを送信する            
        }

        /** 終了処理 **/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!tClient.IsClosed)
                tClient.Close();

        }

        /// <summary>
        /// 一括読み出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContinuityRead_Btn_Click(object sender, EventArgs e)
        {
            if (AdressCheck())
            {
                PlcSendBuffer = continuityReadPlcSend.AddressSet();
                SendCount = 0;
                SendCommand = ContinuityRead;//ワード単位の一括読出
                Timer_Set();
                timer1.Start();// タイマーを開始
            }
        }

        /// <summary>
        /// 一括書き込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContinuityWrite_Btn_Click(object sender, EventArgs e)
        {
            lines_send_count = 0;
            SendCommand = ContinuityWrite;//ワード単位の一括書き込みコマンド 
            File_read(continuityWritePlcSend.FileSelect());
        }

        /// <summary>
        /// ランダムデータ読み出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RandomRead_Btn_Click(object sender, EventArgs e)
        {
            if (RandomReadAddressData())
            {
                SendCount = 0;
                SendCommand = RandomRead;//ランダム読み出しコマンド 
                Timer_Set();
                timer1.Start();// タイマーを開始
            }
        }

        /// <summary>
        /// ランダムデータ書き込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RandomWrite_Btn_Click(object sender, EventArgs e)
        {
            lines_send_count = 0;
            SendCommand = RandomWrite;//ランダム書き込みコマンド 
            File_read(randomWritePlcSend.FileSelect());
        }

        /// <summary>
        /// ファイルを読んでlines[]に代入
        /// </summary>
        /// <param name="FileName"></param>
        private void  File_read(string FileName)
        {
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                lines = File.ReadAllLines(FileName, sjisEnc);//全ファイルを読み込み
                Timer_Set();
                timer2.Start(); // タイマーを開始
            }
            else
            {
                MessageBox.Show("ファイルが選択されていません",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 入力されたD,R,Wアドレスが正しいかチェックする
        /// </summary>
        /// <returns></returns>
        private Boolean AdressCheck()
        {
            long StartAddress = 0, EndAddress = 0;

            for (int i = 0; i < 3; i++)
            {
                //TextBoxをさがす。子コントロールも検索する。
                Control st = this.Controls["StartAdd" + (i + 1).ToString()];
                //TextBoxが見つかれば、Textの値を数値変換する
                if (st != null)
                {
                    if (!string.IsNullOrEmpty(((TextBox)st).Text))
                    {
                        if (i < 2)
                            StartAddress = long.Parse(((TextBox)st).Text);//10進法
                        else
                            StartAddress = Convert.ToInt64(((TextBox)st).Text, 16);//16進法
                        continuityReadPlcSend.PstartAddress[i] = StartAddress;
                    }
                    else
                    {
                        MessageBox.Show("アドレスが設定されていません",
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return false;
                    }
                }
                //TextBoxをさがす。子コントロールも検索する。
                Control en = this.Controls["EndAdd" + (i + 1).ToString()];
                //TextBoxが見つかれば、Textの値を数値変換する
                if (en != null)
                {
                    if (!string.IsNullOrEmpty(((TextBox)en).Text))
                    {
                        if (i < 2)
                            EndAddress = long.Parse(((TextBox)en).Text);//10進法
                        else
                            EndAddress = Convert.ToInt64(((TextBox)en).Text, 16);//16進法
                        continuityReadPlcSend.PendAddress[i] = EndAddress;
                    }
                    else
                    {
                        MessageBox.Show("アドレスが設定されていません",
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return false;
                    }
                    if (EndAddress < StartAddress)
                    {
                        MessageBox.Show("アドレスの設定が間違っています",
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// アドレス設定ファイルを読み込む
        /// ReadAddressSetに作成したrslistを渡す         
        /// </summary>
        private Boolean RandomReadAddressData()
        {
            List<int> ReadAddressList = new List<int>();
            string FileName = Directory.GetCurrentDirectory() + @"\WorkData\Config\ReadAddressData.dat";

            try
            {
                foreach (var sdata in File.ReadLines(FileName, sjisEnc).Skip(1))//1行目をとばす
                {
                    if (int.TryParse(sdata, out int data))
                    {
                        ReadAddressList.Add(data);
                    }
                }
            }
            catch
            {
                MessageBox.Show("読込用設定ファイルが有りません",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                return false;
            }
            var sraList = ReadAddressList.Distinct().OrderBy(t => t).ToList();//重複を消してソートする
            PlcSendBuffer = randomReadPlcSend.AddressSet(sraList);//ランダム読み出し　Dアドレスから送信データを作成
            return true;
        }

        /// <summary>
        /// PLCにPlcSendBufferデータを1個ずつ送信する
        /// </summary>
        private void PlcDataSend()
        {
            if (SendCount < PlcSendBuffer.Count)//送信中
            {
                var SData = PlcSendBuffer.ElementAtOrDefault(SendCount);
                if (SData != null)
                {
                    tClient.Send(SData);//コマンド送信
                    DebugText(SData);
                    SendCount++;
                }
            }
            else //送信完了　受信したデータをチェック OKならばReceiveDataMemorysを作成
            {
                switch (SendCommand){
                    case ContinuityRead:
                    case RandomRead:
                        if (ReadReceiveDataCheck() == 0)
                        {
                            ReceiveDataMemorys.Clear();
                            switch (SendCommand)
                            {
                                case ContinuityRead:
                                    ReceiveDataMemorys = continuityReadPlcSend.RequestReceiveDataSet(ReciveDatas, PlcSendBuffer);
                                    break;
                                case RandomRead:
                                    ReceiveDataMemorys = randomReadPlcSend.RequestReceiveDataSet(ReciveDatas, PlcSendBuffer);
                                    break;
                            }
                            ReciveDataSave(SendCommand);
                        }
                        SendCount = 0;
                        break;
                    case ContinuityWrite:
                    case RandomWrite:
                        WriteReciveDataCheck();
                        break;
                }
            }
        }

        /// <summary>
        /// データ読み出しコマンドで受信したデータのチェック　P68
        /// 1回の受信データのList ReciveDatasを作成
        /// Endcode 0:正常 0以外:異常
        /// </summary>
        private int ReadReceiveDataCheck()
        {
            int Endcode = -1;
           
            foreach (var ReceiveData in ReciveDataBufffer)
            {
                if (ReceiveData?.Length > 0)
                {
                    //3Eフレーム　データを読み出す場合　シーケンサCPU側　→　相手機器側（応答伝文）
                    //サブヘッダ　ネットワーク番号　PC番号 要求先ユニットI/O番号　要求先ユニット局番号　応答データ長　            終了コード　応答データ部
                    //0  2        4                 6      8                      12                    14L H(リトルエンディアン) 18          22
                    //D0 00       00                FF     FF 03                  00                    xxxx                      0000        xxxxxx
                    string DataLength = ReceiveData.Substring(response_data_length_Position, 4);//応答データ長
                    if (Int32.TryParse(ReceiveData.Substring(end_code_Position, 4), out Endcode))//終了コード
                    {
                        if (Endcode == 0)//応答OK
                        {
                            string Databuf = ReceiveData.Substring(read_data_Position);//読み込みデータ
                            if ((Databuf.Length) / 2 + 2 == Convert.ToInt32((DataLength.Substring(2, 2) + DataLength.Substring(0, 2)), 16))
                            {
                                ReciveDatas.Add(Databuf);//受信したデータを追加
                            }
                        }
                    }
                }
            }
            return Endcode;
        }

        /// <summary>
        /// 書き込みコマンドで受信したのデータ(ReciveDataBufffer)の応答の確認
        /// </summary>
        private void WriteReciveDataCheck()
        {
            if (ReciveDataBufffer?.Count > 0) { //null判定
            foreach (var ReceiveData in ReciveDataBufffer)
            {
                string Endcode = ReceiveData.Substring(end_code_Position, 4);//終了コード
                if (int.Parse(Endcode) != 0)
                {
                    MessageBox.Show("書き込みデータの応答が異常終了です");
                }
            }
        }
        }


        /// <summary>
        /// 送信データ,受信データの保存 Debug
        /// </summary>
        /// <param name="line"></param>Debug Text
        private void DebugText(string line)
        {
            textBox1.Text = line;
            if (!string.IsNullOrWhiteSpace(line))
            {
                
                string cDir = @Directory.GetCurrentDirectory() + @"\WorkData\Debug\" + StartTime + ".txt";
                DateTime now = DateTime.Now;
                string str = now.ToString("yyyy/MM/dd HH:mm:ss,");
                using (var writer = new System.IO.StreamWriter(cDir, true, sjisEnc))
                {
                    // 文字列を書き込む
                    writer.WriteLine(str + line);

                } // usingを抜けるときにファイルがクローズされる
            }
        }

        /*
        /// <summary>
        /// テスト用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click_1(object sender, EventArgs e)
        {
           
            //                                要求データ長 CPU監視タイマ  コマンド サブコマンド ワードアクセス点数　先頭アドレス  デバイスコード  書き込みデータ
            //                                                                                    ダブルワードアクセス点数
            //string Buf2 = "500000FFFF0300  0E00         1000           0214     0000         01　00                010000　　　　A8　　　　　　 0A00";//D001 H0Aを書き込み
            int devicecode = 0;
            long StartAddress = 0;
            int WriteLen =0;
            string Wdata ="";

            //string Buf2 = "500000FFFF03000E00 1000021400000100010000A80A00";//D001 H0Aを書き込み
            //                                要求データ長 CPU監視タイマ  ランダム読み出しコマンド  サブコマンド ワードアクセス点数　    アドレス  デバイスコード
            //                                                                                                     ダブルワードアクセス点数
            //string Buf3 = "500000FFFF0300   0C00         1000            0304 　　　　　　　　　  0000         01 00    　　　　　 010000        A8";//D001 読み込み
            //string Buf3 = "500000FFFF03000C001000030400000100010000A8";//D001 読み込み
            string Buf3 =   "54000100000000FFFF03000C001000030400000100010000A8";
            //Buf3 =        "54000100000000FFFF0300 0E00 1000 0214 0000 01 00 E80300A8";
            int len = (int)WriteLen * 2 + 12;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;
            int addLo = (int)StartAddress & 0xff;
            int addHi = (int)StartAddress >> 8;
            int itemLo = (int)WriteLen & 0xff;
            int itemHi = (int)WriteLen >> 8;
            string dc = devicecode.ToString("X2");
            string Buf1 = "500000FFFF0300";
            Buf1 = Buf1 + lenLo.ToString("X2") + lenHi.ToString("X2") + "1000"+"0214" + "0000" + addLo.ToString("X2") + addHi.ToString("X2") + "00" + dc + itemLo.ToString("X2") + itemHi.ToString("X2") + Wdata;
            tClient.Send(Buf3);//一括書込コマンド送信
            DebugText(Buf3);
            ReadReceiveDataCheck();
        }
        */
        

        /// <summary>
        /// ReceiveDataMemorysのデータを1行にしてファイルに書き込む
        /// </summary>
        /// <param name="type">dataの種類　1:連続　2:ﾗﾝﾀﾞﾑ</param>
        private void ReciveDataSave(string type)
        {
            string cDir="";
            string directory = "";
            switch(type)
            {
                case ContinuityRead :
                    directory = @"\連続データ\";
                    break;
                case RandomRead :
                    directory = @"\ランダムデータ\";
                    break;
            }
            cDir = Directory.GetCurrentDirectory() + @"\WorkData\PlcData"+ directory + StartTime + ".csv";
            DateTime now = DateTime.Now;
            string str = now.ToString("yyyy/MM/dd HH:mm:ss,");
            if (ReceiveDataMemorys?.Count > 0)
            {
                foreach (var sdata in ReceiveDataMemorys)
                {
                    str = str + sdata.ReceiveAddress + "," + sdata.ReceiveDataSet + ",";
                }
                using (var writer = new System.IO.StreamWriter(cDir, true, sjisEnc))
                {
                    // 文字列を書き込む
                    writer.WriteLine(str);

                } // usingを抜けるときにファイルがクローズされる
            }
        }

        /// <summary>
        /// 読み出しコマンド用タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            ReciveDataBufffer.Clear();
            ReciveDatas.Clear();
            PlcDataSend();
            if (elapsedTimeSet)
            {  
                // 現在時を取得
                DateTime datetime = DateTime.Now;
                textBox5.Text = datetime.ToLongTimeString();
                //現在の時間が設定の時間になった時の処理
                if (datetime >= dt1)
                {
                    timer1.Stop();
                    MessageBox.Show("経過時間が過ぎました",
                                       "時間",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Asterisk);
                }
            }
        }

        /// <summary>
        /// 書き込みコマンド用のタイマー
        /// 書き込み用のデータから送信コマンドListを作成して送信開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (lines_send_count >= lines.Count())//送信完了
            {
                timer2.Stop();
                textBox1.Text = "送信完了";
            }
            else
            {
                var LineList = lines[lines_send_count].Split(',').Skip(1).ToList();//','でデータを分割して1番目のデータを削除
                if (LineList?.Count > 0)
                {
                    SendCount = 0;
                    ReciveDataBufffer.Clear();
                    var AddressList = LineList.Where((name, index) => index % 2 == 0).ToList();//addressを抽出
                    var DataList = LineList.Where((name, index) => index % 2 == 1).ToList();//dataを抽出
                    var A_D_LIST = AddressList.Zip(DataList, (address, data) => (address, data)).ToList();//addressとdataを1つのlistにマージする　型はタプル型
                    switch(SendCommand)
                    {
                        case ContinuityWrite:
                            PlcSendBuffer = continuityWritePlcSend.AddressSet(A_D_LIST);
                            break;
                        case RandomWrite:
                            PlcSendBuffer = randomWritePlcSend.AddressSet(A_D_LIST);
                            break;
                    }
                    PlcDataSend();//送信開始
                    lines_send_count++;
                }
            }
        }

        /// <summary>
        /// 読み出し通信停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        /// <summary>
        /// タイマーインターバル設定
        /// </summary>
        private void Timer_Set()
        {
            Boolean TimerError = false;
            if (int.TryParse(textBox2.Text ,out int interval))
            {
                if ((interval > 0)&&(interval < 100))
                {
                    timer1.Interval = interval * 1000;
                    timer2.Interval = interval * 1000;
                }
                else
                {
                    TimerError = true;
                }
            }else
            {
                TimerError = true;
              
            }
            if (TimerError)
            {
                textBox2.Text = "1";
                timer1.Interval = 1000;
                timer2.Interval = 1000;
            }

            // 現在時を取得
            dt1 = DateTime.Now;
            //終了時刻を設定
            int elapsedTime = -1;
            int.TryParse(textBox3.Text, out elapsedTime);
            if (elapsedTime > 0)
            {
                TimeSpan ts1 = new TimeSpan(0, 0, 0, elapsedTime);
                dt1 += ts1;
                textBox4.Text = dt1.ToLongTimeString();
                elapsedTimeSet = true;
            }
            else
            {
                elapsedTimeSet = false;
            }
        }
    }

    /// <summary>
    /// データ受信list用のクラス
    /// </summary>
    public class ReceiveDataMemory
    {
        public string ReceiveAddress { get; private set; }
        public int ReceiveDataSet { get; private set; }

        public ReceiveDataMemory() { }
        public ReceiveDataMemory(string receiveAddress,int receiveDataSet)
        {
            ReceiveAddress = receiveAddress;
            ReceiveDataSet = receiveDataSet;
        }
    }
}
