using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace PLCDataBackUp
{
    abstract class PlcSend
    {
        //Socketクライアント
        TcpClient tClient = new TcpClient();
        protected string Buf = "500000FFFF0300";
        protected string CPUwatchtimer = "1000";
        protected string Sdata;
        protected List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>(); 
        internal string StartTime { get; set; }
        
        public abstract string Commandcreate(int count,string senddata);

        public abstract List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas,List<string> RandomPlcSendBuffer);
        public abstract List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas);
        
        /// <summary>
        /// 送信データ,受信データの保存 Debug
        /// </summary>
        /// <param name="line"></param>Debug Text
        private void DebugText(string line)
        {
            string cDir = Directory.GetCurrentDirectory() + @"\WorkData\Debug\" + StartTime + ".txt";

            //ファイルを追記し、Shift JISで書き込む
            System.IO.StreamWriter sw = new System.IO.StreamWriter(
                @cDir,
                true,
                System.Text.Encoding.GetEncoding("shift_jis"));
            sw.WriteLine(line);
            //閉じる
            sw.Close();
        }

        /// <summary>
        /// ファイルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public string FileSelect() 
        {
            string FileName="";

            //OpenFileDialogクラスのインスタンスを作成
            OpenFileDialog ofd = new OpenFileDialog();

            //はじめのファイル名を指定する
            //はじめに「ファイル名」で表示される文字列を指定する
            ofd.FileName = "default.csv";
            //はじめに表示されるフォルダを指定する
            //指定しない（空の文字列）の時は、現在のディレクトリが表示される
            ofd.InitialDirectory = @"C:\";
            //[ファイルの種類]に表示される選択肢を指定する
            //指定しないとすべてのファイルが表示される
            ofd.Filter =
                "csvファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*";
            //[ファイルの種類]ではじめに
            //「すべてのファイル」が選択されているようにする
            ofd.FilterIndex = 2;
            //タイトルを設定する
            ofd.Title = "開くファイルを選択してください";
            //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
            ofd.RestoreDirectory = true;
            //存在しないファイルの名前が指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            ofd.CheckFileExists = true;
            //存在しないパスが指定されたとき警告を表示する
            //デフォルトでTrueなので指定する必要はない
            ofd.CheckPathExists = true;

            //ダイアログを表示する
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FileName = ofd.FileName;
            }
            return FileName;
        }
    }


    /// <summary>
    /// ランダム読み出しデータ設定用
    /// 
    /// /// </summary>
    class RandomReadPlcSend : PlcSend
    {
        /// <summary>
        /// PLCへの送信文を作成
        /// </summary>
        /// <param name="count"></param>
        /// <param name="senddata"></param>
        /// <returns></returns>
        public override string Commandcreate(int count, string senddata)
        {
            int len = count * 4 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            Sdata = Buf + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "03040000" + count.ToString("X2") + "00" + senddata;
            return Sdata;
        }
        /// <summary>
        /// ランダム読み出し用
        /// 要求したデータをlist<>ReceiveDataMemorysに設定
        ///  ReciveDatas と SendatasからデータをマージしてReceiveDataMemorysに代入する
        /// </summary>
        public override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> RandomPlcSendBuffer)
        {
            string DeviceKind = "D";
            List<string> addressList = new List<string>();
            List<int> dataList = new List<int>();

            foreach (var RRdata in ReciveDatas.Select((data, index) => new { data, index }))
            {
                string senddata = RandomPlcSendBuffer.ElementAtOrDefault(RRdata.index); 
                if (senddata != null)
                {
                    //DアドレスのList<string>を作成する
                    int Datacount = (senddata.Length - 33)/8;
                    for (int i=0; i< Datacount; i++)
                    {
                        //1word 8byte  P171  
                        //address   L  -  H  デバイスコード
                        //          12 34 56 78 
                        string address = senddata.Substring(34+i*8, 8);
                        addressList.Add(DeviceKind + Convert.ToInt32((address.Substring(2, 2) + address.Substring(0, 2)), 16).ToString("D4"));
                    }

                    //読み込んだデータのlist<int>を作成する
                    Boolean Dataend = true;
                    int rdatacount = 0;
                    do
                    {
                        try
                        {
                            if (RRdata.data.Length > rdatacount * 4)
                            {
                                string Rdata = RRdata.data.Substring(rdatacount * 4, 4);
                              
                                if (!string.IsNullOrEmpty(Rdata))
                                {
                                    dataList.Add(Convert.ToInt32((Rdata.Substring(2, 2) + Rdata.Substring(0, 2)), 16));
                                    
                                }
                                rdatacount++;
                            }
                            else
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
            ReceiveDataMemorys.Clear();

            foreach (var adata in addressList.Zip(dataList, (address, data) => Tuple.Create(address, data)))//タプル
            {
                ReceiveDataMemorys.Add(new ReceiveDataMemory(adata.Item1, adata.Item2));//本当は無駄　ReceiveDataMemorysを返すために行っている  
            } 

            return ReceiveDataMemorys;
        }

        //一括読み出し用
        public override List<ReceiveDataMemory>  RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
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
                            }
                            else
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

            return ReceiveDataMemorys;
        }

    }

    /// <summary>
    /// ランダム書き込みデータ設定用
    /// 
    /// /// </summary>
    class RandomWritePlcSend : PlcSend
    {
        /// <summary>
        /// PLCへの送信文を作成
        /// </summary>
        /// <param name="count"></param>
        /// <param name="senddata"></param>
        /// <returns></returns>
        public override string Commandcreate(int count, string senddata)
        {
            int len = count * 6 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            Sdata = Buf + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "02140000" + count.ToString("X2") + "00" + senddata;
            return Sdata;
        }
        /// <summary>
        /// ランダム読み出し用
        /// 要求したデータをlist<>ReceiveDataMemorysに設定
        ///  ReciveDatas と Sendatasからデータを合わせて(ZIP)からReceiveDataMemorysに代入する
        /// </summary>
        public override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> RandomPlcSendBuffer)
        {
            string DeviceKind = "D";
            List<string> addressList = new List<string>();
            List<int> dataList = new List<int>();

            foreach (var RRdata in ReciveDatas.Select((data, index) => new { data, index }))
            {
                string senddata = RandomPlcSendBuffer.ElementAtOrDefault(RRdata.index);
                if (senddata != null)
                {
                    //DアドレスのList<string>を作成する
                    int Datacount = (senddata.Length - 33) / 8;
                    for (int i = 0; i < Datacount; i++)
                    {
                        //1word 8byte  P171  
                        //address   L  -  H  デバイスコード
                        //          12 34 56 78 
                        string address = senddata.Substring(34 + i * 8, 8);
                        addressList.Add(DeviceKind + Convert.ToInt32((address.Substring(2, 2) + address.Substring(0, 2)), 16).ToString("D4"));
                    }

                    //読み込んだデータのlist<int>を作成する
                    Boolean Dataend = true;
                    int rdatacount = 0;
                    do
                    {
                        try
                        {
                            if (RRdata.data.Length > rdatacount * 4)
                            {
                                string Rdata = RRdata.data.Substring(rdatacount * 4, 4);

                                if (!string.IsNullOrEmpty(Rdata))
                                {
                                    dataList.Add(Convert.ToInt32((Rdata.Substring(2, 2) + Rdata.Substring(0, 2)), 16));

                                }
                                rdatacount++;
                            }
                            else
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
            ReceiveDataMemorys.Clear();

            foreach (var adata in addressList.Zip(dataList, (address, data) => Tuple.Create(address, data)))//タプル
            {
                ReceiveDataMemorys.Add(new ReceiveDataMemory(adata.Item1, adata.Item2));//本当は無駄　ReceiveDataMemorysを返すために行っている  
            }

            return ReceiveDataMemorys;
        }

        //一括読み出し用
        public override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
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
                            }
                            else
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

            return ReceiveDataMemorys;
        }

    }

}
