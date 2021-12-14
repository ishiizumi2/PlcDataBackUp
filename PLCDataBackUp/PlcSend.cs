//コメントのP***は Q対応Ethernet リファレンスマニュアル　コミュニケーションプロトコル.pdfのページ数
//伝文のフォーマット　4Eフレーム交信　バイナリコード
//ランダム読み出し　ランダム書き込みはDデバイスのみ対応
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
        TcpClient tClient = new TcpClient();//Socketクライアント
        protected const long MaxLength = (long)480;
        protected const int RandomReadMax = 192;//ランダム読み出し最大
        protected const int RandomWriteMax = 150;//ランダム書き込み最大点数　P114
        protected const int ArrayCount = 68; //32767/MaxLingthの数

        protected const string Common = "500000FFFF0300"; //P72 3Eフレーム
        protected const string CPUwatchtimer = "1000";
        protected const string ContinuityRead = "0104"; //PLC 一括読み出しコマンド 
        protected const string ContinuityWrite = "0114"; //PLC 一括書き込みコマンド
        protected const string RandomRead = "0304"; //PLC ランダム読み出しコマンド 
        protected const string RandomWrite = "0214"; //PLC ランダム書き込み
        protected const string Subcommand = "0000";  //サブコマンド
        protected const int data_start_position = 34; //ランダムデータ読み出し　送信データのデータ部分開始位置
        protected const int SendWordLength = 8; //送信デバイスメモリ1個分のバイト数
        protected const int ReciveWordLength = 4; //受信応答したデバイスメモリ1個分のLength P112

        protected List<ReceiveDataMemory> ReceiveDataMemorys = new List<ReceiveDataMemory>(); //受信したデータからcsvファイルを作成するためのList
        protected List<string> PlcSendBuffer = new List<string>(); //送信コマンド伝文用List
        protected int[] Device = { 0xA8, 0xAF, 0xB4 };//D,R,W
        protected string[] DevicdCode = { "D", "R", "W" };

        //PLCへの送信コマンドを作成
        private protected abstract string Commandcreate(int count,string senddata);//PLCへの送信コマンドを作成

        internal abstract List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas,List<string> PlcSendBuffer);

        internal abstract List<string> AddressSet(List<int> sraList);                  //ランダム読み出しデータ設定用
        internal abstract List<string> AddressSet(List<(string address, string data)> swaList); //ランダム書き込みデータ設定用 一括書き込みデータ設定用address, data
        internal abstract List<string> AddressSet();                                   //一括読み出しデータ設定用

        internal abstract string AddressSetiing(List<int> OneraList);　　　　　　　　　//ランダム読み出しデータ設定用
        internal abstract string AddressSetiing(List<(string address, string data)> OnewaList);　//ランダム書き込みデータ設定用 一括書き込みデータ設定用
        internal abstract string AddressSetiing(int devicecode, long StartAddress, long ReadLen);　//一括読み出しデータ設定用

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
        /// PLCへの送信コマンドを作成
        /// </summary>
        /// <param name="count">読み出し点数</param>
        /// <param name="senddata">要求データ部</param>
        /// <returns>送信コマンド</returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 4 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;
            return Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + RandomRead + Subcommand + count.ToString("X2") + "00" + senddata;
        }


        /// <summary>
        /// ランダム読み出し用
        /// Dデバイスのみ対応
        /// ReciveDatasからDアドレスの値を取り出してvalueListを作成　  
        /// PlcSendBufferから読み込み要求したアドレスを取り出してaddressListを作成 
        /// </summary>
        /// <param name="ReciveDatas"受信データ></param>
        /// <param name="PlcSendBuffer"送信データ></param>
        /// <returns>ReceiveDataMemorys</returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            List<string> addressList = new List<string>();
            List<int> valueList = new List<int>();
            foreach (var (value, index) in ReciveDatas.Select((value, index) => (value, index)))
            {
                string senddata = PlcSendBuffer.ElementAtOrDefault(index); 
                if (!string.IsNullOrEmpty(senddata))
                {
                    //DアドレスのList<string>を作成する
                    int Datacount = (senddata.Length - data_start_position)/SendWordLength;//要求したデータの個数を産出
                    for (int i=0; i< Datacount; i++)
                    {
                        //Q対応Ethernet リファレンスマニュアル　コミュニケーションプロトコル.pdf P171参照
                        //1word 8byte    
                        //デバイス指定形式   L  -  H  デバイスコード
                        //       　　　　　　12 34 56 78 
                        string address = senddata.Substring(data_start_position+i*SendWordLength, SendWordLength);
                        string DeviceKind = "D";
                        addressList.Add(DeviceKind + Convert.ToInt32((address.Substring(4, 2) + address.Substring(2, 2)
                                                   + address.Substring(0, 2)), 16).ToString("D4"));//要求したデバイスメモリのアドレスを10進数4桁に変換
                    }

                    //受信したデータの<List>valueを作成する
                    Boolean Dataend = true;
                    int rdatacount = 0;
                    do
                    {
                        try
                        {
                            if (value.Length > rdatacount * ReciveWordLength)
                            {
                                string Rdata = value.Substring(rdatacount * ReciveWordLength, ReciveWordLength);
                                if (!string.IsNullOrEmpty(Rdata))
                                {
                                    valueList.Add(Convert.ToInt32((Rdata.Substring(2, 2) + Rdata.Substring(0, 2)), 16)); //要求したデバイスメモリの値 10進数に変換
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

            //アドレスのList(addressList)と受信データのList(valueList)をMergeしてReceiveDataMemorysを作成する
            foreach (var adata in addressList.Zip(valueList, (address, value) => Tuple.Create(address, value)))//タプル
            {

                ReceiveDataMemorys.Add(new ReceiveDataMemory(adata.Item1, adata.Item2));
               
            } 
                      
             return ReceiveDataMemorys;
        }

        /// <summary>
        /// D_LISTから送信データList PlcSendBufferを作成
        /// </summary>
        /// <param name="D_LIST"></param>
        /// <returns>PlcSendBuffer</returns>送信データList
        internal override List<string>  AddressSet(List<int> D_LIST)
        {
            int i = 0;
            PlcSendBuffer.Clear();
            while (i * RandomReadMax < D_LIST.Count())
            {
                var OneraList = D_LIST.Skip(i * RandomReadMax).Take(RandomReadMax).ToList(); //1回読み込み分のデータを取り出す
                PlcSendBuffer.Add(Commandcreate(OneraList.Count(), AddressSetiing(OneraList)));
                i++;
            }

            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet(List<(string address, string data)> swaList)
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
        /// OneraListから送信用アドレスデータを作成
        /// </summary>
        /// <param name="OneraList">1回の送信分のデータ部分</param>
        /// <returns>senddata</returns>要求データ部
        internal override string AddressSetiing(List<int> OneraList)
        {
            string senddata = "";
            string deviceCode = "A8"; //Dアドレス
            foreach (var sdat in OneraList)
            {
                int addLo = sdat & 0xff;
                int addHi = sdat >> 8;
                //アドレスの指定　P171 デバイス(6桁)　デバイスコード D固定にしている
                //                      L H 0固定  D
                //                      000000     A8
                senddata = senddata + addLo.ToString("X2") + addHi.ToString("X2") + "00" +deviceCode;
            }
            return senddata;
        }

        //未使用
        internal override string AddressSetiing(List<(string address, string data)> OnewaList)
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
    /// ランダム書き込みデータ設定用　P157
    /// 
    /// /// </summary>
    class RandomWritePlcSend : PlcSend
    {
        /// <summary>
        /// PLCへの送信文を作成
        /// </summary>
        /// <param name="count">ワードアクセス点数</param>
        /// <param name="senddata">書き込みデータ</param>
        /// <returns>送信コマンド</returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 6 + 8;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            return Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + RandomWrite + Subcommand + count.ToString("X2") + "00" + senddata;
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


        //未使用
        internal override List<string> AddressSet(List<int> sraList)
        {

            return PlcSendBuffer;
        }

        /// <summary>
        /// A_D_LISTから送信データList PlcSendBufferを作成
        /// </summary>
        /// <param name="A_D_LIST">アドレス,データのタプル型の</param>
        /// <returns>PlcSendBufferr</returns> 送信データList
        internal override List<string> AddressSet(List<(string address, string data)> A_D_LIST)
        {
            int i = 0;
            PlcSendBuffer.Clear();
            while (i * RandomWriteMax < A_D_LIST.Count())
            {
                var OnewaList = A_D_LIST.Skip(i * RandomWriteMax).Take(RandomWriteMax).ToList();//1回の送信コマンド用List
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
        /// PlcSendBuffer用データを作成
        /// OneraListから送信用アドレスデータを作成
        /// 
        /// </summary>
        /// <param name="OnewaList"></param>
        /// <returns>senddata</returns>要求データ部　1回のコマンド送信用のデバイス・デバイスコード・書き込みデータ
        internal override string AddressSetiing(List<(string address, string data)> OnewaList)
        {
            string senddata = "";

            foreach (var sdat in OnewaList)
            {
                string ad = sdat.address.Substring(0, 1);
                int iaddress = int.Parse(sdat.address.Replace(ad, ""));
                string deviceCode = CodeChange(ad);
                int idata = int.Parse(sdat.data);
                int addLo = iaddress & 0xff;
                int addHi = iaddress >> 8;
                int dataLo = idata & 0xff;
                int dataHi = idata >> 8;
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
    /// 一括読出し用データ設定用
    /// </summary>
    class ContinuityReadPlcSend : PlcSend
    {
        List<SendDataClass> SendDatas = new List<SendDataClass>();
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
        /// PLCへの送信コマンドを作成
        /// </summary>
        /// <param name="count"></param>
        /// <param name="senddata"></param>
        /// <returns>送信コマンド</returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = 0x0C;//0C固定
            int lenLo = len & 0xff;
            int lenHi = len >> 8;
            return Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + ContinuityRead + Subcommand + senddata;
        }

        /// <summary>
        ///  一括読み出し用
        /// 要求したデータをlist<>ReceiveDataMemorysに設定
        ///  ReciveDatas と SendatasからデータをマージしてReceiveDataMemorysに代入する
        /// </summary>
        /// <param name="ReciveDatas"></param>受信データ
        /// <param name="PlcSendBuffer"></param>コマンド要求送信データ
        /// <returns>ReceiveDataMemorys</returns>
        internal override List<ReceiveDataMemory> RequestReceiveDataSet(List<string> ReciveDatas, List<string> PlcSendBuffer)
        {
            string DeviceKind = "";
            string receiveAddress = "";

            foreach (var ReceiveData in ReciveDatas.Select((data, index) => new { data, index }))
            {
                SendDataClass senddata = SendDatas.ElementAtOrDefault(ReceiveData.index);
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
                                            receiveAddress = DeviceKind + (senddata.SendStartAddress + Datacount).ToString();//10進数文字に変換
                                            break;
                                        case 0xAF:
                                            DeviceKind = "R";
                                            receiveAddress = DeviceKind + (senddata.SendStartAddress + Datacount).ToString();//10進数文字に変換
                                            break;
                                        case 0xB4:
                                            DeviceKind = "W";
                                            receiveAddress = DeviceKind + (senddata.SendStartAddress + Datacount).ToString("X");//16進数文字に変換
                                            break;
                                        default:
                                            break;
                                    }
                                    int receiveDataSet = Convert.ToInt32((Rdata.Substring(2, 2) + Rdata.Substring(0, 2)), 16);//16進数の文字列を整数に変換
                                    ReceiveDataMemorys.Add(new ReceiveDataMemory(receiveAddress, receiveDataSet));
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

       
        //  未使用
        internal override List<string> AddressSet(List<int> sraList)
        {
            return PlcSendBuffer;
        }

        //未使用
        internal override List<string> AddressSet(List<(string address, string data)> swaList)
        {
            return PlcSendBuffer;
        }

        /// <summary>
        /// ReadOutAddress[,,]から送信データList PlcSendBufferを作成
        /// </summary>
        /// <returns>PlcSendBuffer</returns>送信データList
        internal override List<string> AddressSet()
        {
            //int devicecode = 0;
            ReadOutStartAddressSet();
            foreach (var (device, index) in Device.Select((device, index) => (device, index)))//D:0,R:1,W:2の順でforecahで回す
            {
                for (int i = 0; i < ArrayCount; i++)
                {
                    if (ReadOutAddress[index, i, 0] != (long)-1)
                    {
                        SendDatas.Add(new SendDataClass(device, ReadOutAddress[index, i, 0], ReadOutAddress[index, i, 1]));//Buferに送信データを代入
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
        internal override string AddressSetiing(List<(string address, string data)> OnewaList)
        {
            string str = "";
            return str;
        }

        /// <summary>
        /// 一括読み出しデータ設定用  P151
        /// PlcSendBuffer用データを作成
        /// ReadOutAddressから送信用アドレスデータを作成
        /// </summary>
        /// <param name="devicecode">デバイスコード　D,R,W</param>
        /// <param name="StartAddress">先頭デバイス</param>
        /// <param name="ReadLength">デバイス点数</param>
        /// <returns>senddata</returns>要求データ部
        internal override string AddressSetiing(int devicecode, long StartAddress, long ReadLength)
        {
            string senddata = "";          
            int Address_Low = (int)StartAddress & 0xff; //先頭アドレスは6文字で表す L-H表記
            int Adress_Middle = (int)StartAddress >> 8;
            int Adress_High = 0;
            int ReadLength_Low = (int)ReadLength & 0xff;
            int ReadLength_High = (int)ReadLength >> 8;

            senddata = Address_Low.ToString("X2") + Adress_Middle.ToString("X2") + Adress_High.ToString("X2")  + devicecode.ToString("X2")
                     + ReadLength_Low.ToString("X2") + ReadLength_High.ToString("X2");

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
        /// <returns>送信コマンド</returns>
        private protected override string Commandcreate(int count, string senddata)
        {
            int len = count * 2 + 12;
            int lenLo = len & 0xff;
            int lenHi = len >> 8;

            return Common + lenLo.ToString("X2") + lenHi.ToString("X2") + CPUwatchtimer + ContinuityWrite + Subcommand + senddata;
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

        //未使用
        internal override List<string> AddressSet(List<int> sraList)
        {

            return PlcSendBuffer;
        }

        /// <summary>
        /// A_D_LISTから送信データList PlcSendBufferを作成
        /// </summary>
        /// <param name="A_D_LIST"></param>
        /// <returns>PlcSendBuffer</returns>送信データList
        internal override List<string> AddressSet(List<(string address, string data)> A_D_LIST)
        {
            PlcSendBuffer.Clear();
            foreach(var code in DevicdCode)
            {
                var dquery = A_D_LIST.Where(c => c.address.StartsWith(code)).ToList();//該当するdeviceのみ抜き出す
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

        /// <summary>
        /// PlcSendBuffer用データを作成
        /// OneraListから送信用アドレスデータを作成
        /// 
        /// </summary>
        /// <param name="OnewaList"></param>
        /// <returns>senddata</returns>要求データ部　1回のコマンド送信用のデバイス・デバイスコード・書き込みデータ
        internal override string AddressSetiing(List<(string address, string data)> OnewaList)
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
            //アドレスの指定　P156 先頭アドレス  デバイスコード  デバイス点数 
            //                      L -  H       D
            //                      000000       A8
            senddata = addLo.ToString("X2") + addHi.ToString("X2") + "00" + deviceCode + itemLo.ToString("X2") + itemHi.ToString("X2");
            foreach (var sdat in OnewaList)
            {
                int data = int.Parse(sdat.data);
                int dataLo = data & 0xff;
                int dataHi = data >> 8;
                // デバイス点数分のデータ
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
    /// 一括読み出しコマンドデータ送信用list用のクラス
    /// </summary>
    class SendDataClass
    {
        public int Senddevicecode { get; private set; } //デバイスコード
        public long SendStartAddress { get; private set; } //先頭アドレス
        public long SendReadLength { get; private set; } //デバイス点数

        public SendDataClass() { }
        public SendDataClass(int senddevicecode, long sendStartAddress, long sendReadLength)
        {
            Senddevicecode = senddevicecode;
            SendStartAddress = sendStartAddress;
            SendReadLength = sendReadLength;
        }
    }
}

