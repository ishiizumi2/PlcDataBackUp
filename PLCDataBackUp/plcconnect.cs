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
        const long MaxLength = (long)480;//960;
        const int RandomReadMax = 192;//ランダム読み出し最大
        const int RandomWriteMax = 150;//ランダム書き込み最大点数　P114
        const int ArrayCount = 68; //32767/MaxLingthの数
        const int RDStratPosition = 22;
        const int LumpingRead = 0401; //PLC 一括読み出しコマンド
        const int LumpingWrite = 1401;//PLC 一括書き込みコマンド
        const int RandomRead = 0403; //PLC ランダム読み出しコマンド 
        const int RandomWrite = 1402; //PLC ランダム読み出しコマンド
        const string DeviceCode = "A8"; //Dアドレス
        const int MaxAddres = 11135; //Qシリーズで扱える最大のDアドレス

        //Socketクライアント
        TcpClient tClient = new TcpClient();
        RandomReadPlcSend randomReadPlcSend = new RandomReadPlcSend();
        RandomWritePlcSend randomWritePlcSend = new RandomWritePlcSend();
        long[,,] ReadOutAddress = new long[3, ArrayCount, 2];
        int SendCount = 0; //送信データカウント
        string StartTime;
        int SendCommand = 0;
        Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
        string[] lines;
        int RowCount = 0; //ランダム書き込み用の配列のカウント
       
        enum Device
        {
            D,R,W
        }
        List<SendData> SendDatas = new List<SendData>();
        List<WriteSendData> WriteSendDatas = new List<WriteSendData>();
        List<string> ReciveDataBufffer = new List<string>(); 
        List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>();
        List<string> ReciveDatas = new List<string>();
        List<string> RandomPlcSendBuffer = new List<string>();
       

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
            randomReadPlcSend.StartTime = StartTime;
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
            ReciveDataBufffer.Add(e);//受信した
            DebugText(e);
            PlcSend();//次のデータを送信する            
        }

        /** 終了処理 **/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!tClient.IsClosed)
                tClient.Close();

        }

        /// <summary>
        /// 読み出しコマンドのデバイスコード,先頭アドレス,データ数を設定する
        /// </summary>
        private Boolean ReadOutStartAddressSet()
        {
            long StartAddress=0, EndAddress=0;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < ArrayCount; j++)
                    for (int k = 0; k < 2;k++ )
                        ReadOutAddress[i, j, k] = (long)-1;
            
            for(int AddressCnt=0; AddressCnt<1; AddressCnt++)
            {
                //TextBoxをさがす。子コントロールも検索する。
                Control st = this.Controls["StartAdd"+(AddressCnt+1).ToString()];
                //TextBoxが見つかれば、Textの値を数値変換する
                if (st != null)
                {
                    if (!string.IsNullOrEmpty(((TextBox)st).Text))
                    {
                        if (AddressCnt < 2)
                            StartAddress = long.Parse(((TextBox)st).Text);//10進法
                        else 
                            StartAddress = Convert.ToInt64(((TextBox)st).Text, 16);//16進法
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
                Control en = this.Controls["EndAdd" + (AddressCnt+1).ToString()];
                //TextBoxが見つかれば、Textの値を数値変換する
                if (en != null)
                {
                    if (!string.IsNullOrEmpty(((TextBox)en).Text))
                    {
                        if (AddressCnt < 2)
                            EndAddress = long.Parse(((TextBox)en).Text);//10進法
                        else
                            EndAddress = Convert.ToInt64(((TextBox)en).Text, 16);//16進法
                    }
                    else
                    {
                        MessageBox.Show("アドレスが設定されていません",
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return false;
                    }
                    if (EndAddress< StartAddress)
                    {
                        MessageBox.Show("アドレスの設定が間違っています",
                                        "エラー",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return false;
                    }
                }

                for (long j = 0; j < ArrayCount; j++)
                {
                    ReadOutAddress[AddressCnt, j, 0] = (long)(StartAddress + MaxLength * j);//開始Address
                    if ((StartAddress + MaxLength * (j+1)) <= EndAddress)
                    {
                        ReadOutAddress[AddressCnt, j, 1] = (long)MaxLength;//読み出しワード数
                    }
                    else
                    {
                        ReadOutAddress[AddressCnt, j, 1] = (long)(EndAddress - (StartAddress + MaxLength * j) + 1);//最終読み出しワード数
                        break;
                    }
                }
            }
            return true;
        }

       
        /// <summary>
        /// PLCからのデータ一括読み込みD,R,W
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            int devicecode = 0;
            if (!ReadOutStartAddressSet())
                return;

            for(long devicecnt = 0; devicecnt <1 ; devicecnt++)
            {
                switch(devicecnt)
                {
                    case (int)Device.D :
                        devicecode = 0xA8;
                        break;
                    case (int)Device.R :
                        devicecode = 0xAF;
                        break;
                    case (int)Device.W :
                        devicecode = 0xB4;
                        break;
                    default :
                        break;
                }
                
                for (int i=0;i<ArrayCount;i++)
                {
                    if (ReadOutAddress[devicecnt, i, 0] != (long)-1)
                    {
                        SendDatas.Add(new SendData(devicecode, ReadOutAddress[devicecnt, i, 0], ReadOutAddress[devicecnt, i, 1]));//Buferに送信データを代入
                    }
                    else
                        break;
                }
            }
            dataGridView1.DataSource = SendDatas.ToList<SendData>();
            dataGridView1.Columns[0].DefaultCellStyle.Format = "X";

            //Bufferのデータを送信する
　          SendCount = 0;
            SendCommand = LumpingRead;//ワード単位の一括読出
            ReciveDataBufffer.Clear();
            PlcSend();//最初のデータ送信
        }

        /// <summary>
        /// PLCに送信バッファのデータを1個ずつ送信する
        /// </summary>
        private void PlcSend()
        {
            switch (SendCommand)
            {
                case LumpingRead:
                    if (SendCount < SendDatas.Count)
                    {
                        SendData SData = SendDatas.ElementAtOrDefault(SendCount);
                        if (SData != null)
                        {
                            RequestDataSend(SData.Senddevicecode, SData.SendStartAddress, SData.SendReadLen);
                            SendCount++;
                        }
                    }
                    else //受信完了
                    {
                        if (ReadReceiveDataCheck() == 0)
                        {
                            dataGridView2.DataSource = randomReadPlcSend.RequestReceiveDataSet(ReciveDatas, SendDatas);
                        }
                    }
                    break;
                case LumpingWrite:
                    if (SendCount < WriteSendDatas.Count)
                    {
                        WriteSendData SData = WriteSendDatas.ElementAtOrDefault(SendCount);
                        if (SData != null)
                        {
                            WriteDataSend(SData.Senddevicecode, SData.SendStartAddress, SData.SendReadLen, SData.SendDataStr);
                            SendCount++;
                        }
                    }
                    else //受信完了
                    {
                        WriteReciveDataCheck();
                    }
                    break;
                case RandomRead:
                    if (SendCount < RandomPlcSendBuffer.Count)
                    {
                        var SData = RandomPlcSendBuffer.ElementAtOrDefault(SendCount);
                        if (SData != null)
                        {
                            tClient.Send(SData);//ランダム読み出しコマンド送信
                            DebugText(SData);
                            SendCount++;
                        }
                    }
                    else //受信完了
                    {
                        if (ReadReceiveDataCheck() == 0)
                        {
                            ReceiveDataMemorys.Clear();
                            ReceiveDataMemorys = randomReadPlcSend.RequestReceiveDataSet(ReciveDatas, RandomPlcSendBuffer);
                            RandomReciveDataSave();
                        }
                        SendCount = 0;
                    }
                    break;
                case RandomWrite:
                    if (SendCount < RandomPlcSendBuffer.Count)
                    {
                        var SData = RandomPlcSendBuffer.ElementAtOrDefault(SendCount);
                        if (SData != null)
                        {
                            tClient.Send(SData);//ランダム書き込みコマンド送信
                            DebugText(SData);
                            SendCount++;
                        }
                    }
                    else //受信完了
                    {
                        WriteReciveDataCheck();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// PLCに一括読み出しコマンドを送信する関数
        /// </summary>
        /// <param name="devicecode"></param>デバイスコード D,R,W
        /// <param name="StartAddress"></param>先頭アドレス
        /// <param name="ReadLen"></param>読み出しワード数
        private void RequestDataSend(int devicecode,long StartAddress,long ReadLen)
        {
            string Buf1 = "500000FFFF03000C00100001040000";
            int addLo = (int)StartAddress & 0xff;
            int addHi = (int)StartAddress >> 8;
            int itemLo = (int)ReadLen & 0xff;
            int itemHi = (int)ReadLen >> 8;
            string aa = devicecode.ToString("X2");

            Buf1 = Buf1 + addLo.ToString("X2") + addHi.ToString("X2")+ "00"+ aa +itemLo.ToString("X2") + itemHi.ToString("X2");
            tClient.Send(Buf1);//一括データ読み出しコマンド送信
            DebugText(Buf1);
        }

        /// <summary>
        /// データ読み出しコマンドで受信したデータのチェック
        /// Endcode 0:正常 0以外:異常
        /// </summary>
        private int ReadReceiveDataCheck()
        {
            int Endcode = -1;

            foreach (var RRdata in ReciveDataBufffer.Select((data, index) => new { data, index }))
            {
                if (RRdata.data.Length != 0)
                { 
                   string DataLength = RRdata.data.Substring(14, 4);//応答データ長
                   if (Int32.TryParse(RRdata.data.Substring(18, 4), out Endcode))
                   {
                       if (Endcode == 0)
                       {
                           string Databuf = RRdata.data.Substring(RDStratPosition);//読み込みデータ
                           int Position = Convert.ToInt32((DataLength.Substring(2, 2) + DataLength.Substring(0, 2)), 16);
                           if ((Databuf.Length) / 2 + 2 == Position)
                           {
                               ReciveDatas.Add(Databuf);
                           }
                        }
                    }
                }
            }
            return Endcode;
        }
       

       /// <summary>
       /// 要求したデータをlist<>ReceiveDataMemorysに設定
       /// </summary>
        private void RequestReceiveDataSet()
        {
            string DeviceKind = "";
            string s = "";
           
            foreach (var RRdata in ReciveDatas.Select((data, index) => new { data, index }))
            {
                SendData senddata = SendDatas.ElementAtOrDefault(RRdata.index);
                if (senddata != null)
                {
                    int Datacount = 0;
                    Boolean Dataend = true;
                    do
                    {
                        try
                        {
                            if (RRdata.data.Length > Datacount * 4)
                            {
                                string Rdata = RRdata.data.Substring(Datacount * 4, 4);
                                if (!string.IsNullOrEmpty(Rdata))
                                {

                                    switch (senddata.Senddevicecode)
                                    {
                                        case 0xA8:
                                            DeviceKind = "D";
                                            s = DeviceKind + (senddata.SendStartAddress + Datacount).ToString();
                                            break;
                                        case 0xAF:
                                            DeviceKind = "R";
                                            s = DeviceKind + (senddata.SendStartAddress + Datacount).ToString();
                                            break;
                                        case 0xB4:
                                            DeviceKind = "W";
                                            s = DeviceKind + (senddata.SendStartAddress + Datacount).ToString("X"); //16進数文字に変換
                                            break;
                                        default:
                                            break;
                                    }
                                    int d = Convert.ToInt32((Rdata.Substring(2, 2) + Rdata.Substring(0, 2)), 16);
                                    ReceiveDataMemorys.Add(new ReceiveDataMemory(s, d));
                                }
                                Datacount++;
                            }else
                            {
                                Dataend = false;
                            }
                        }
                        catch
                        {
                            Dataend = false;
                        }
                    }
                    while (Dataend);
                }
            }
            dataGridView2.DataSource = ReceiveDataMemorys.ToList<ReceiveDataMemory>();
        }
      

       
        /// <summary>
        /// データのファイルへの保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button6_Click(object sender, EventArgs e)
        {
            //SaveFileDialogクラスのインスタンスを作成
            SaveFileDialog sfd = new SaveFileDialog();
            DateTime dt = DateTime.Now;
            string dt1 = dt.ToString("yyyy-MM-dd_HH-mm-ss");
            //はじめのファイル名を指定する
            sfd.FileName = "PLCDATA"+dt1+".csv";

            //はじめに表示されるフォルダを指定する
            sfd.InitialDirectory = @"C:\";
            //[ファイルの種類]に表示される選択肢を指定する
            sfd.Filter =
                "CSVファイル(*.csvl;*.csv)|*.csvl;*.csv|すべてのファイル(*.*)|*.*";
            //[ファイルの種類]ではじめに
            //「すべてのファイル」が選択されているようにする
            sfd.FilterIndex = 2;
            //タイトルを設定する
            sfd.Title = "保存先のファイルを選択してください";
            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            sfd.RestoreDirectory = true;
            //既に存在するファイル名を指定したとき警告する
            //デフォルトでTrueなので指定する必要はない
            sfd.OverwritePrompt = true;
            //存在しないパスが指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            sfd.CheckPathExists = true;

            //ダイアログを表示する
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき
                //選択されたファイル名を表示する
                //Console.WriteLine(sfd.FileName);
                // 保存用のファイルを開く
                using (StreamWriter writer = new StreamWriter(@sfd.FileName, false, Encoding.GetEncoding("shift_jis")))
                {
                    writer.WriteLine(dt1);
                    int rowCount = dataGridView2.Rows.Count;

                    // ユーザによる行追加が許可されている場合は、最後に新規入力用の
                    // 1行分を差し引く
                    if (dataGridView2.AllowUserToAddRows == true)
                    {
                        rowCount = rowCount - 1;
                    }

                    // 行
                    for (int i = 0; i < rowCount; i++)
                    {
                        // リストの初期化
                        List<String> strList = new List<String>();

                        // 列
                        for (int j = 0; j < dataGridView2.Columns.Count; j++)
                        {
                            strList.Add(dataGridView2[j, i].Value.ToString());
                        }
                        String[] strArray = strList.ToArray();  // 配列へ変換

                        // CSV 形式に変換
                        String strCsvData = String.Join(",", strArray);//文字列の配列を連結する

                        writer.WriteLine(strCsvData);
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
            string cDir = Directory.GetCurrentDirectory()+@"\WorkData\Debug\"+StartTime+".txt";
            DateTime now = DateTime.Now;
            string str = now.ToString("yyyy/MM/dd HH:mm:ss,");

            //ファイルを追記し、Shift JISで書き込む
            System.IO.StreamWriter sw = new System.IO.StreamWriter(
                @cDir,
                true,
                System.Text.Encoding.GetEncoding("shift_jis"));
            sw.WriteLine(str+line);
            //閉じる
            sw.Close();
        }

        /// <summary>
        /// データのファイルからの読み出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            string FileName = randomWritePlcSend.FileSelect();
            if (!string.IsNullOrWhiteSpace(FileName))
            {
                //前に表示されているデータを消去
                ReceiveDataMemorys.Clear();

                //OKボタンがクリックされたとき
                //選択されたファイル名を読み込んで表示する
                foreach (var sdata in File.ReadLines(FileName, Encoding.GetEncoding("Shift_JIS")).Skip(1).Select(line => line.Split(',')))
                {
                    ReceiveDataMemorys.Add(new ReceiveDataMemory(sdata[0], int.Parse(sdata[1])));
                }
                dataGridView2.DataSource = ReceiveDataMemorys.ToList<ReceiveDataMemory>();

            }
        }
        /// <summary>
        /// 読み出しデータをPLCに一括書き込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Write_Click(object sender, EventArgs e)
        {
            if (WriteDataSet())
            {
                //Bufferのデータを送信する
                SendCount = 0;
                SendCommand = LumpingWrite;//ワード単位の一括書き込み
                ReciveDataBufffer.Clear();
                PlcSend();
            }  
        }

        /// <summary>
        /// 読み出したデータをPLCに書き込みする為のデータをセット 
        /// </summary>
        private Boolean WriteDataSet()
        {
            string[] Addres = new string[6];
            ReceiveDataMemory[] RData = new ReceiveDataMemory[6];
            int devicecode = 0;
           
            if (ReceiveDataMemorys.Count != 0)
            {
                RData[0] = ReceiveDataMemorys.FirstOrDefault(c => c.ReceiveAddress.StartsWith("D"));
                RData[1] = ReceiveDataMemorys.LastOrDefault(c => c.ReceiveAddress.StartsWith("D"));
                RData[2] = ReceiveDataMemorys.FirstOrDefault(c => c.ReceiveAddress.StartsWith("R"));
                RData[3] = ReceiveDataMemorys.LastOrDefault(c => c.ReceiveAddress.StartsWith("R"));
                RData[4] = ReceiveDataMemorys.FirstOrDefault(c => c.ReceiveAddress.StartsWith("W"));
                RData[5] = ReceiveDataMemorys.LastOrDefault(c => c.ReceiveAddress.StartsWith("W"));
            }
            else
            {
                MessageBox.Show("データが空です");
                return(false);
            }
            SendDatas.Clear();
            for (int i = 0; i < 1; i++)
            {
                long StartAddres = long.Parse(RData[i * 2].ReceiveAddress.Substring(1));
                long EndAddress = long.Parse(RData[i * 2 + 1].ReceiveAddress.Substring(1));

                switch (i)
                {
                    case (int)Device.D:
                        devicecode = 0xA8;
                        break;
                    case (int)Device.R:
                        devicecode = 0xAF;
                        break;
                    case (int)Device.W:
                        devicecode = 0xB4;
                        break;
                    default:
                        break;
                }

                int Datacount = 0;
                Boolean Dataend = true;
                do
                {
                    string str;
                    if ((EndAddress+1) - (StartAddres + MaxLength * Datacount) > (long)MaxLength)
                    {
                        WriteDataSetting(devicecode, Datacount, (long)MaxLength, out str);
                        WriteSendDatas.Add(new WriteSendData(devicecode, StartAddres + MaxLength * Datacount, (long)MaxLength, str));//Buferに送信データを代入
                    }
                    else
                    {
                        long Len = (EndAddress+1) - (StartAddres + MaxLength * Datacount);
                        WriteDataSetting(devicecode, Datacount, Len, out str);
                        WriteSendDatas.Add(new WriteSendData(devicecode, StartAddres + MaxLength * Datacount, Len, str));//Buferに送信データを代入
                        Dataend = false;
                    }
                    Datacount++;
                } while (Dataend);
            }

            //dataGridView1.DataSource = WriteSendDatas.ToList<WriteSendData>();
           
            return (true);
        }

        /// <summary>
        /// PLCに一括書込コマンドを送信する関数
        /// </summary>
        /// <param name="devicecode"></param>デバイスコード D,R,W
        /// <param name="StartAddress"></param>先頭アドレス
        /// <param name="ReadLen"></param>読み出しワード数
        private void WriteDataSend(int devicecode, long StartAddress, long WriteLen, string Wdata)
        {
            
            //                                要求データ長 CPU監視タイマ  コマンド サブコマンド 先頭アドレス  デバイスコード  デバイス点数  デバイス点数分のデータ
            //string Buf1 = "500000FFFF0300　0E00　       1000           0114     0000         660000        A8              0100          9519";//D100 K6549を書き込み
            //string Buf2 = "500000FFFF03000E00100001140000660000A801009519";//D100 K6549を書き込み
           

            int len = (int)WriteLen*2 + 12;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;
            int addLo = (int)StartAddress & 0xff;
            int addHi = (int)StartAddress >> 8;
            int itemLo = (int)WriteLen & 0xff;
            int itemHi = (int)WriteLen >> 8;
            string dc = devicecode.ToString("X2");
            string Buf1 = "500000FFFF0300";
            Buf1 = Buf1 + lenLo.ToString("X2") + lenHi.ToString("X2") + "100001140000" + addLo.ToString("X2") + addHi.ToString("X2") + "00" + dc + itemLo.ToString("X2") + itemHi.ToString("X2") + Wdata;
            //string Buf2 = "500000FFFF03000E00100001140000640000A801009519";//D100 K6549を書き込み
                             
            tClient.Send(Buf1);//一括書込コマンド送信
            DebugText(Buf1);
        }

        /// <summary>
        /// 書き込み点数分のデータ設定
        /// </summary>
        /// <param name="devicecode"></param>デバイスコード D,R,W
        /// <param name="Count"></param>
        /// <param name="WriteLen"></param>デバイス点数
        /// <param name="str"></param>書き込み点数分のデータ　参照渡しで返す
        private void WriteDataSetting(int devicecode, int Count,long WriteLen,out string str)
        {
            int sdat ,Lo, Hi;
            str = "";
            string Dcode = "";
            switch (devicecode)
            {
                case 0xA8:
                    Dcode = "D";
                    break;
                case 0xAF:
                    Dcode = "R";
                    break;
                case 0xB4:
                    Dcode ="W";
                    break;
                default:
                    break;
            }
            foreach (var data in ReceiveDataMemorys.Where(c => c.ReceiveAddress.StartsWith(Dcode)).Skip((int)MaxLength * Count).Take((int)WriteLen))
            {
                 sdat = data.ReceiveDataSet;
                 Lo = sdat & 0xff;
                 Hi = sdat >> 8;
                 str = str + Lo.ToString("X2") + Hi.ToString("X2");
            }
        }

        /// <summary>
        /// 一括書き込みデータの応答の確認
        /// </summary>
        private void WriteReciveDataCheck()
        {
            //string DataLength = "0";

            foreach (var RRdata in ReciveDataBufffer.Select((data, index) => new { data, index }))
            {
                //DataLength = RRdata.data.Substring(14, 4);//応答データ長
                string Endcode = RRdata.data.Substring(18, 4);//終了コード
                if (int.Parse(Endcode) != 0)
                {
                    MessageBox.Show("書き込みデータの応答が異常終了です"); 
                }
            }
        }

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
            string Buf3 = "500000FFFF03000C001000030400000100010000A8";//D001 読み込み
            //Buf3 =        "500000FFFF0300 0E00 1000 0214 0000 01 00 E80300A8";
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

        /// <summary>
        /// アドレス設定ファイルを読み込む
        /// ReadAddressSetに作成したrslistを渡す        /// 
        /// </summary>
        private void RandomReadAddressData()
        {
            List<int> ReadAddressList = new List<int>();
            string FileName = Directory.GetCurrentDirectory() + @"\WorkData\Config\ReadAddressData.dat";
           
            try { 
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
                return;
            }
            var sraList = ReadAddressList.Distinct().OrderBy(t => t).ToList();//重複を消してソートする
            RandomReadAddressSet(sraList);
         }

        /// <summary>
        /// ralistから送信データList RandomPlcSendBuffer<>を作成
        /// </summary>
        /// <param name="ralist"></param>
        private void RandomReadAddressSet(List<int> sraList)
        {
            int i = 0;
            RandomPlcSendBuffer.Clear();
            while(i* RandomReadMax  < sraList.Count())
            {
                var OneraList = sraList.Skip(i * RandomReadMax).Take(RandomReadMax).ToList(); //1回読み込み分のデータを取り出す
                RandomPlcSendBuffer.Add(randomReadPlcSend.Commandcreate(OneraList.Count(), RandomReadAddressSetiing(OneraList)));
                i++;
            }
        }

        /// <summary>
        /// RandomPlcSendBuffer用データを作成
        /// //RAListから送信用アドレスデータを作成
        /// </summary>
        /// <param name="ralist3"></param>
        /// <returns></returns>
        private string RandomReadAddressSetiing(List<int> OneraList)
        {
            string str = "";
            foreach (var sdat in OneraList)
            {
                int addLo = sdat & 0xff;
                int addHi = sdat >> 8;
                //アドレスの指定　P171 デバイス　デバイスコード
                //                      L -  H     D
                //                      000000     A8
                str = str + addLo.ToString("X2") + addHi.ToString("X2") + "00"+ DeviceCode;
            }
            return str;
        }

        /// <summary>
        /// ランダム読み出しスタート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            RandomReadAddressData();
            SendCount = 0;
            SendCommand = RandomRead;//ランダム読み出しコマンド 
            Timer_Set();
            timer1.Start();// タイマーを開始
        }

        /// <summary>
        /// ReceiveDataMemorysのデータを1行にしてファイルに書き込む
        /// </summary>
        private void RandomReciveDataSave()
        {
            string cDir = Directory.GetCurrentDirectory() + @"\WorkData\PlcData\" + StartTime + ".csv";
            DateTime now = DateTime.Now;
            string str = now.ToString("yyyy/MM/dd HH:mm:ss,");

            foreach (var sdata in ReceiveDataMemorys)
            {
                str = str + sdata.ReceiveAddress + "," + sdata.ReceiveDataSet + ",";
            }
            //ファイルを追記し、Shift JISで書き込む
            System.IO.StreamWriter sw = new System.IO.StreamWriter(
                @cDir,
                true,
                System.Text.Encoding.GetEncoding("shift_jis"));
            sw.WriteLine(str);
            //閉じる
            sw.Close();
        }

        /// <summary>
        /// ランダム読み出し用タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
             ReciveDataBufffer.Clear();
             ReciveDatas.Clear();
             PlcSend();
        }

        /// <summary>
        /// ランダムデータ書き込みスタート
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click_1(object sender, EventArgs e)
        {
            RowCount = 0;
            SendCommand = RandomWrite;//ランダム書き込みコマンド 
            ReciveDataBufffer.Clear();
            string FileName = randomWritePlcSend.FileSelect();
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
        /// wdataからランダムデータ書き込み送信用データを作成
        /// </summary>
        /// <param name="wdata"></param>
        private void RandomWriteDataSet(List<(string x, string y)> swaList)
        {
　          int i = 0;
            RandomPlcSendBuffer.Clear();
            while (i * RandomWriteMax < swaList.Count())
            {
                var OnewaList = swaList.Skip(i * RandomWriteMax).Take(RandomWriteMax).ToList();
                RandomPlcSendBuffer.Add(randomWritePlcSend.Commandcreate(OnewaList.Count(), RandomWriteDataSetting(OnewaList)));
                i++;
            }
        }


        /// <summary>
        /// WWadatからランダム書き込み用のアドレス・データを作成
        /// </summary>
        /// <param name="wdata"></param>
        /// <returns></returns>str
        private string RandomWriteDataSetting(List<(string x,string y)> OnewaList)
        {
            string str = "";
            foreach (var sdat in OnewaList)
            {
                int address = int.Parse(sdat.x.Replace("D",""));
                int data = int.Parse(sdat.y);
                int addLo = address &  0xff;
                int addHi = address >> 8;
                int dataLo = data & 0xff;
                int dataHi = data >> 8;
                //アドレスの指定　P158 デバイス　デバイスコード  書き込みデータ
                //                      L -  H     D
                //                      000000     A8
                str = str + addLo.ToString("X2") + addHi.ToString("X2") + "00" + DeviceCode + dataLo.ToString("X2") + dataHi.ToString("X2");
            }
            return str;
        }

        /// <summary>
        /// ランダム書き込み用のタイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (RowCount >= lines.Count())//送信完了
            {
                timer2.Stop();
                textBox1.Text = "送信完了";
            }
            else
            {
                string[] arr = lines[RowCount].Split(',');
                if (arr.Length != 0)
                {
                    SendCount = 0;
                    ReciveDataBufffer.Clear();

                    List<string> alist = new List<string>();
                    alist.AddRange(arr); //string[]をlist<string>に変換するために行っている


                    var blist = alist.Skip(1).ToList();//時刻データを削除

                    var result2 = blist.Where((name, index) => index % 2 == 0).ToList();//addressを抽出
                    var result1 = blist.Where((name, index) => index % 2 == 1).ToList();//dataを抽出

                    var swaList = result2.Zip(result1, (address, data) => (address, data)).ToList();//addressとdataを1つのlistにマージする
                    RandomWriteDataSet(swaList);
                    PlcSend();
                    RowCount++;
                }
            }
        }

        /// <summary>
        /// ランダム読み出し通信停止
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
            if (int.TryParse(textBox2.Text ,out int i))
            {
                if ((i > 0)&&(i<100))
                {
                    timer1.Interval = i * 1000;
                    timer2.Interval = i * 1000;
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
        }
     }

    /// <summary>
    /// 読み込みデータ送信用list用のクラス
    /// </summary>
    class SendData
    {
        public int  Senddevicecode { get; private set; }
        public long SendStartAddress { get; private set; }
        public long SendReadLen { get; private set; }

        public SendData() { }
        public SendData(int senddevicecode, long sendStartAddress, long sendReadLen)
        {
            Senddevicecode   = senddevicecode;
            SendStartAddress = sendStartAddress;
            SendReadLen      = sendReadLen;
        }
    }

    /// <summary>
    /// 書き込みデータ送信用list用のクラス
    /// </summary>
    class WriteSendData
    {
        public int Senddevicecode { get; private set; }
        public long SendStartAddress { get; private set; }
        public long SendReadLen { get; private set; }
        public string SendDataStr { get; private set; }

        public WriteSendData() { }
        public WriteSendData(int senddevicecode, long sendStartAddress, long sendReadLen,string sendDataStr)
        {
            Senddevicecode = senddevicecode;
            SendStartAddress = sendStartAddress;
            SendReadLen = sendReadLen;
            SendDataStr = sendDataStr;
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
