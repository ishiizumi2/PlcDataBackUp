using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Data;

namespace PLCDataBackUp
{
    /// <summary>
    /// 基底クラス
    /// </summary>
    abstract class PlcSend
    {
        //Socketクライアント
        TcpClient tClient = new TcpClient();
        protected const long MaxLength = (long)480;
        protected const int RandomReadMax = 192;//ランダム読み出し最大
        protected const int RandomWriteMax = 150;//ランダム書き込み最大点数　P114
        protected const int ArrayCount = 68; //32767/MaxLingthの数

        protected string Common = "500000FFFF0300";
        protected string CPUwatchtimer = "1000";
        protected string Sdata;
        protected List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>();
        protected List<string> PlcSendBuffer = new List<string>(); //コマンド伝文用List
        protected int[] Device = { 0xA8, 0xAF, 0xB4 };//D,R,W
        protected string[] DevicdCode = { "D", "R", "W" };

        internal string StartTime { get; set; }

        private protected abstract string Commandcreate(int count,string senddata);

        internal abstract List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas,List<string> PlcSendBuffer);
        internal abstract List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas);

        internal abstract List<string> AddressSet(List<int> sraList);
        internal abstract List<string> AddressSet(List<(string x, string y)> swaList);
        internal abstract List<string> AddressSet();

        internal abstract string AddressSetiing(List<int> OneraList);
        internal abstract string AddressSetiing(List<(string x, string y)> OnewaList);
        internal abstract string AddressSetiing(int devicecode, long StartAddress, long ReadLen);
        
        /// <summary>
        /// ファイルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal string FileSelect() 
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

        /// <summary>
        /// デバイスコードを送信用バイナリコードに変換
        /// </summary>
        /// <param name="ch"></param>デバイスコード
        /// <returns></returns>バイナリコード
        protected string CodeChange(string ch)
        {
            string devicecode;
            switch (ch)
            {
                case "D":
                    devicecode = "A8";
                    break;
                case "R":
                    devicecode = "AF";
                    break;
                case "W":
                    devicecode = "B4";
                    break;
                default:
                    devicecode = "";
                    break;
            }
            return devicecode;
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
        /// <param name="count">読み出し点数</param>
        /// <param name="senddata">要求データ部</param>
        /// <returns>sdata 通信伝文</returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 4 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            Sdata = Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "03040000" + count.ToString("X2") + "00" + senddata;
            return Sdata;
        }

        /// <summary>
        /// ランダム読み出し用
        /// 要求したデータをlist<>ReceiveDataMemorysに設定
        ///  ReciveDatas と SendatasからデータをマージしてReceiveDataMemorysに代入する
        /// </summary>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            string DeviceKind = "D";
            List<string> addressList = new List<string>();
            List<int> dataList = new List<int>();
            foreach (var (data, index) in ReciveDatas.Select((data, index) => (data, index)))
            {
                string senddata = PlcSendBuffer.ElementAtOrDefault(index); 
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
                            if (data.Length > rdatacount * 4)
                            {
                                string Rdata = data.Substring(rdatacount * 4, 4);
                              
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

                ReceiveDataMemorys.Add(new ReceiveDataMemory(adata.Item1, adata.Item2));
               
            } 
                      
             return ReceiveDataMemorys;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory>  RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

        /// <summary>
        /// ralistから送信データList PlcSendBuffer<>を作成
        /// </summary>
        /// <param name="ralist"></param>
        internal override List<string>  AddressSet(List<int> sraList)
        {
            int i = 0;
            PlcSendBuffer.Clear();
            while (i * RandomReadMax < sraList.Count())
            {
                var OneraList = sraList.Skip(i * RandomReadMax).Take(RandomReadMax).ToList(); //1回読み込み分のデータを取り出す
                PlcSendBuffer.Add(Commandcreate(OneraList.Count(), AddressSetiing(OneraList)));
                i++;
            }

            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            return PlcSendBuffer;
        }
        //未使用
        internal override List<string> AddressSet()
        {
            return PlcSendBuffer;
        }

        /// <summary>
        /// PlcSendBuffer用データを作成
        /// //RAListから送信用アドレスデータを作成
        /// </summary>
        /// <param name="ralist3"></param>
        /// <returns></returns>
        internal override string AddressSetiing(List<int> OneraList)
        {
            string senddata = "";
            string deviceCode = "A8"; //Dアドレス
            foreach (var sdat in OneraList)
            {
                int addLo = sdat & 0xff;
                int addHi = sdat >> 8;
                //アドレスの指定　P171 デバイス　デバイスコード
                //                      L -  H     D
                //                      000000     A8
                senddata = senddata + addLo.ToString("X2") + addHi.ToString("X2") + "00" +deviceCode;
            }
            return senddata;
        }

        //未使用
        internal override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string str="";
            return str;
        }
        //未使用
        internal override string AddressSetiing(int devicecode, long StartAddress, long ReadLen)
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
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 6 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            Sdata = Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "02140000" + count.ToString("X2") + "00" + senddata;
            return Sdata;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="PlcSendBuffer"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            return ReceiveDataMemorys;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

        //未使用
        internal override List<string> AddressSet(List<int> sraList)
        {

            return PlcSendBuffer;
        }


        internal override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            int i = 0;
            PlcSendBuffer.Clear();
            while (i * RandomWriteMax < swaList.Count())
            {
                var OnewaList = swaList.Skip(i * RandomWriteMax).Take(RandomWriteMax).ToList();
                PlcSendBuffer.Add(Commandcreate(OnewaList.Count(), AddressSetiing(OnewaList)));
                i++;
            }
            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet()
        {
            return PlcSendBuffer;
        }

        //未使用
        internal override string AddressSetiing(List<int> OneraList)
        {
            string str = "";
            return str;
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OnewaList"></param>
        /// <returns></returns>
        internal override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string senddata = "";

            foreach (var sdat in OnewaList)
            {
                string ad = sdat.x.Substring(0, 1);
                int address = int.Parse(sdat.x.Replace(ad, ""));
                string deviceCode = CodeChange(ad);
                int data = int.Parse(sdat.y);
                int addLo = address & 0xff;
                int addHi = address >> 8;
                int dataLo = data & 0xff;
                int dataHi = data >> 8;
                //アドレスの指定　P158 デバイス　デバイスコード  書き込みデータ
                //                      L -  H     D
                //                      000000     A8
                senddata = senddata + addLo.ToString("X2") + addHi.ToString("X2") + "00" + deviceCode + dataLo.ToString("X2") + dataHi.ToString("X2");
            }
            return senddata;
        }

        //未使用
        internal override string AddressSetiing(int devicecode, long StartAddress, long ReadLen)
        {
            string str = "";
            return str;
        }
    }


    /// <summary>
    /// 一括読み出しデータ設定用
    /// 
    /// /// </summary>
    class ContinuityReadPlcSend : PlcSend
    {
        List<SendData> SendDatas = new List<SendData>();
        long[,,] ReadOutAddress = new long[3, ArrayCount, 2]; //アドレス 0:D 1:R 2:W　,データのカウント,0:開始アドレス 1:読み出しワード数

        private long[] Svalues = new long[3];
        private long[] Evalues = new long[3];

        internal long[] PstartAddress //D,R,W StartAddress用
        {
            get
            {
                return Svalues;
            }
        }
        internal long[] PendAddress //D,R,W EndAddress用
        {
            get
            {
                return Evalues;
            }

        }

        /// <summary>
        /// PLCへの送信文を作成
        /// </summary>
        /// <param name="count"></param>
        /// <param name="senddata"></param>
        /// <returns></returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = 0x0C;//0C固定
            int lenLo = len & 0xff;
            int lenHi = len >> 8;
            Sdata = Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "01040000" + senddata;
            return Sdata;
        }

        /// <summary>
        /// 一括読み出し用
        /// 要求したデータをlist<>ReceiveDataMemorysに設定
        ///  ReciveDatas と SendatasからデータをマージしてReceiveDataMemorysに代入する
        /// </summary>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            string DeviceKind = "";
            string s = "";

            foreach (var ReceiveData in ReciveDatas.Select((data, index) => new { data, index }))
            {
                SendData senddata = SendDatas.ElementAtOrDefault(ReceiveData.index);
                if (senddata != null)
                {
                    int Datacount = 0;
                    Boolean Dataend = true;
                    do
                    {
                        try
                        {
                            if (ReceiveData.data.Length > Datacount * 4)
                            {
                                string Rdata = ReceiveData.data.Substring(Datacount * 4, 4);
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

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

       
        //  未使用
        internal override List<string> AddressSet(List<int> sraList)
        {
            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            return PlcSendBuffer;
        }

        //
        internal override List<string> AddressSet()
        {
            int devicecode = 0;
            ReadOutStartAddressSet();
            foreach (var (device, index) in Device.Select((device, index) => (device, index)))
            {
                devicecode = device;
                for (int i = 0; i < ArrayCount; i++)
                {
                    if (ReadOutAddress[index, i, 0] != (long)-1)
                    {
                        SendDatas.Add(new SendData(devicecode, ReadOutAddress[index, i, 0], ReadOutAddress[index, i, 1]));//Buferに送信データを代入
                    }
                    else
                        break;
                }
            }

            foreach(var SData in SendDatas)
            {
                PlcSendBuffer.Add(Commandcreate(0, AddressSetiing(SData.Senddevicecode, SData.SendStartAddress, SData.SendReadLength)));
            }

            return PlcSendBuffer;
        }

        /// <summary>
        /// 読み出しコマンドのデバイスコード,先頭アドレス,データ数を設定する
        /// </summary>
        private void ReadOutStartAddressSet()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < ArrayCount; j++)
                    for (int k = 0; k < 2; k++)
                        ReadOutAddress[i, j, k] = (long)-1;//初期化

            for (int i = 0; i < 3; i++)
            {
                if (PstartAddress[i] != PendAddress[i])
                {
                    for (long j = 0; j < ArrayCount; j++)
                    {
                        ReadOutAddress[i, j, 0] = (long)(PstartAddress[i] + MaxLength * j);//開始Address

                        if ((PstartAddress[i] + MaxLength * (j + 1)) <= PendAddress[i])
                        {
                            ReadOutAddress[i, j, 1] = (long)MaxLength;//読み出しワード数
                        }
                        else
                        {
                            ReadOutAddress[i, j, 1] = (long)(PendAddress[i] - (PstartAddress[i] + MaxLength * j) + 1);//最終読み出しワード数
                            break;
                        }
                    }
                }
            }
        }

        //未使用
        internal override string AddressSetiing(List<int> OneraList)
        {
            string str = "";
            return str;
        }

        //未使用
        internal override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string str = "";
            return str;
        }

        /// <summary>
        /// 一括読み出しデータ設定用
        /// </summary>
        /// <param name="devicecode">デバイスの種類D,R,W</param>
        /// <param name="StartAddress">先頭アドレス</param>
        /// <param name="ReadLength">読み出しデータ個数</param>
        /// <returns></returns>
        internal override string AddressSetiing(int devicecode, long StartAddress, long ReadLength)
        {
            string senddata = "";          
            int addLo = (int)StartAddress & 0xff;
            int addHi = (int)StartAddress >> 8;
            int itemLo = (int)ReadLength & 0xff;
            int itemHi = (int)ReadLength >> 8;
            string dc = devicecode.ToString("X2");

            senddata = addLo.ToString("X2") + addHi.ToString("X2") + "00" + dc + itemLo.ToString("X2") + itemHi.ToString("X2");

            return senddata;
        }
    } 

    /// <summary>
    /// 一括書き込みデータ設定用
    /// 
    /// /// </summary>
    class ContinuityWritePlcSend : PlcSend
    {
        /// <summary>
        /// PLCへの送信文を作成
        /// </summary>
        /// <param name="count"></param>
        /// <param name="senddata"></param>
        /// <returns></returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 2 + 12;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            Sdata = Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + "01140000" + senddata;
            return Sdata;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="PlcSendBuffer"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            return ReceiveDataMemorys;
        }

        /// <summary>
        /// 未使用
        /// </summary>
        /// <param name="ReciveDatas"></param>
        /// <param name="SendDatas"></param>
        /// <returns></returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<SendData> SendDatas)
        {
            return ReceiveDataMemorys;
        }

        //未使用
        internal override List<string> AddressSet(List<int> sraList)
        {

            return PlcSendBuffer;
        }

        internal override List<string> AddressSet(List<(string x, string y)> swaList)
        {
            PlcSendBuffer.Clear();
            foreach(var code in DevicdCode)
            {
                var dquery = swaList.Where(c => c.x.StartsWith(code)).ToList();//該当するdeviceのみ抜き出す
                int count = 0;
                while (count * RandomWriteMax < dquery.Count())
                {
                    var OnewaList = dquery.Skip(count * (int)MaxLength).Take((int)MaxLength).ToList();
                    PlcSendBuffer.Add(Commandcreate(OnewaList.Count(), AddressSetiing(OnewaList)));
                    count++;
                }
            }
            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet()
        {
            return PlcSendBuffer;
        }

        //未使用
        internal override string AddressSetiing(List<int> OneraList)
        {
            string str = "";
            return str;
        }

        //
        internal override string AddressSetiing(List<(string x, string y)> OnewaList)
        {
            string senddata = "";
            string code = "";
            int startaddress = 0;
            (string x, string y) saddress = OnewaList.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(saddress.x))
            {
                code = saddress.x.Substring(0, 1);
                startaddress = int.Parse(saddress.x.Replace(code, ""));
            }
            else
            {
                return senddata;
            }

            int addLo = startaddress & 0xff;
            int addHi = startaddress >> 8;
            int itemLo = (int)OnewaList.Count() & 0xff;
            int itemHi = (int)OnewaList.Count() >> 8;
            string deviceCode = CodeChange(code);
            senddata = addLo.ToString("X2") + addHi.ToString("X2") + "00" + deviceCode + itemLo.ToString("X2") + itemHi.ToString("X2");
            foreach (var sdat in OnewaList)
            {
                int data = int.Parse(sdat.y);
                int dataLo = data & 0xff;
                int dataHi = data >> 8;

                //アドレスの指定　P156 先頭アドレス  デバイスコード  デバイス点数  デバイス点数分のデータ
                //                      L -  H       D
                //                      000000       A8
                senddata = senddata +  dataLo.ToString("X2") + dataHi.ToString("X2");
            }
            return senddata;
        }

        //未使用
        internal override string AddressSetiing(int devicecode, long StartAddress, long ReadLen)
        {
            string str = "";
            return str;
        }
    }

    /// <summary>
    /// 読み込みデータ送信用list用のクラス
    /// </summary>
    class SendData
    {
        public int Senddevicecode { get; private set; }
        public long SendStartAddress { get; private set; }
        public long SendReadLength { get; private set; }

        public SendData() { }
        public SendData(int senddevicecode, long sendStartAddress, long sendReadLength)
        {
            Senddevicecode = senddevicecode;
            SendStartAddress = sendStartAddress;
            SendReadLength = sendReadLength;
        }
    }
}

