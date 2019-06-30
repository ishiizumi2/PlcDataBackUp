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
        protected const int RandomReadMax = 192;//ランダム読み出し最大
        protected const int RandomWriteMax = 150;//ランダム書き込み最大点数　P114
        protected const string DeviceCode = "A8"; //Dアドレス

        protected string Buf = "500000FFFF0300";
        protected string CPUwatchtimer = "1000";
        protected string Sdata;
        protected List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>();
        protected List<string> RandomPlcSendBuffer = new List<string>();

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
        public abstract List<string>  AddressSet(List<int> sraList);
        public abstract List<string> AddressSet(List<(string x, string y)> swaList);

        public abstract string AddressSetiing(List<int> OneraList);
        public abstract string AddressSetiing(List<(string x, string y)> OnewaList);

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

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        public override List<ReceiveDataMemory>  RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

        /// <summary>
        /// ralistから送信データList RandomPlcSendBuffer<>を作成
        /// </summary>
        /// <param name="ralist"></param>
        public override List<string>  AddressSet(List<int> sraList)
        {
            int i = 0;
            RandomPlcSendBuffer.Clear();
            while (i * RandomReadMax < sraList.Count())
            {
                var OneraList = sraList.Skip(i * RandomReadMax).Take(RandomReadMax).ToList(); //1回読み込み分のデータを取り出す
                RandomPlcSendBuffer.Add(Commandcreate(OneraList.Count(), AddressSetiing(OneraList)));
                i++;
            }

            return RandomPlcSendBuffer;
        }

        //未使用
        public override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            return RandomPlcSendBuffer; 
        }
        /// <summary>
        /// RandomPlcSendBuffer用データを作成
        /// //RAListから送信用アドレスデータを作成
        /// </summary>
        /// <param name="ralist3"></param>
        /// <returns></returns>
        public override string AddressSetiing(List<int> OneraList)
        {
            string str = "";
            foreach (var sdat in OneraList)
            {
                int addLo = sdat & 0xff;
                int addHi = sdat >> 8;
                //アドレスの指定　P171 デバイス　デバイスコード
                //                      L -  H     D
                //                      000000     A8
                str = str + addLo.ToString("X2") + addHi.ToString("X2") + "00" + DeviceCode;
            }
            return str;
        }

        //未使用
        public override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string str = "";
            return str;
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
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="RandomPlcSendBuffer"></param>
        /// <returns></returns>
        public override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> RandomPlcSendBuffer)
        {
            return ReceiveDataMemorys;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        public override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

        //未使用
        public override List<string> AddressSet(List<int> sraList)
        {

            return RandomPlcSendBuffer;
        }


        public override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            int i = 0;
            RandomPlcSendBuffer.Clear();
            while (i * RandomWriteMax < swaList.Count())
            {
                var OnewaList = swaList.Skip(i * RandomWriteMax).Take(RandomWriteMax).ToList();
                RandomPlcSendBuffer.Add(Commandcreate(OnewaList.Count(), AddressSetiing(OnewaList)));
                i++;
            }
            return RandomPlcSendBuffer;
        }

        //未使用
        public override string AddressSetiing(List<int> OneraList)
        {
            string str = "";
            return str;
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnewaList"></param>
        /// <returns></returns>
        public override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string str = "";
            foreach (var sdat in OnewaList)
            {
                int address = int.Parse(sdat.x.Replace("D", ""));
                int data = int.Parse(sdat.y);
                int addLo = address & 0xff;
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


    }

}
