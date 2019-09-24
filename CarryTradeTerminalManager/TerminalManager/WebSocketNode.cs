using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace TerminalManager
{
    class SocketPacket
    {
        public string type { get; set; }
        public string command { get; set; }
        public object data { get; set; }
    }
    class LoginPacket
    {
        public string terminalid { get; set; }
    }
    public class MT4SettingPacket
    {
        public string Port1 { get; set; }
        public string Port2 { get; set; }
        public string AccountNo1 { get; set; }
        public string AccountNo2 { get; set; }
        public string Password1 { get; set; }
        public string Password2 { get; set; }
        public string MT4Server1 { get; set; }
        public string MT4Server2 { get; set; }
        public string CurrencyPair { get; set; }
        public string UsernameToRebalance { get; set; }
        public string PasswordToRebalance { get; set; }
        public string WithdrawRetryCount { get; set; }
        public string DepositeRetryCount { get; set; }
        public string SpreadToOpen { get; set; }
        public string MarginTargetPercent { get; set; }
        public string SpliteCount { get; set; }
        public string SpreadToClose { get; set; }
        public string LowLimitMarginPercent { get; set; }
        public string MagicNumbersToClose { get; set; }
        public string slippage1 { get; set; }
        public string slippage2 { get; set; }
    }

    class MT4CurrentState
    {
        public string Symbol1 { get; set; }
        public string Bid1 { get; set; }
        public string Ask1 { get; set; }
        public string ExpertHandle1 { get; set; }
        public string Balance1 { get; set; }
        public string Equity1 { get; set; }
        public string FreeMargin1 { get; set; }
        public string Margin1 { get; set; }
        public string Profit1 { get; set; }
        public string Swap_Buy1 { get; set; }
        public string Swap_Sell1 { get; set; }
        public string Account_No1 { get; set; }

        public string Symbol2 { get; set; }
        public string Bid2 { get; set; }
        public string Ask2 { get; set; }
        public string ExpertHandle2 { get; set; }
        public string Balance2 { get; set; }
        public string Equity2 { get; set; }
        public string FreeMargin2 { get; set; }
        public string Margin2 { get; set; }
        public string Profit2 { get; set; }
        public string Swap_Buy2 { get; set; }
        public string Swap_Sell2 { get; set; }
        public string Account_No2 { get; set; }
    }
    
    /// <summary>
    /// Main Socket Communication Class
    /// </summary>
    class WebSocketNode
    {
        WebSocket socket;
        public event UserlistChangedEvent UserListChanged;
        public event ReceiveMessageEvent MessageReceived;
        public event UserConnectEvent UserConnected;
        public event UserDisonnectEvent UserDisconnected;
        public event MT4SettingEvent MT4SettingReceived;
        public string strTerminalId { get; set; } = "ApolloTest1";
        public WebSocketNode()
        {
            socket = new WebSocket("ws://ec2-18-130-72-146.eu-west-2.compute.amazonaws.com:3000");

            socket.OnOpen += (sender, e) =>
            {
                LoginPacket LP = new LoginPacket();
                LP.terminalid = strTerminalId;
                Socket_Emit("joinTerminal", LP);
            };
            socket.OnMessage += (sender, e) =>
            {
                ProcessDataReceived(e.Data);
            };
        }
        public void Socket_Connect()
        {
            socket.Connect();
        }
        public async void Socket_Emit( string typeVal, object dataVal)
        {
            await Task.Run(() =>
            {
                SocketPacket SP = new SocketPacket();
                SP.type = typeVal;
                SP.data = dataVal;
                string strSendData = JsonConvert.SerializeObject(SP);
                socket.Send(strSendData);
            });
        }
        public async void SendDataToUser(string commandVal, object dataVal)
        {
            await Task.Run(() =>
            {
                SocketPacket SP = new SocketPacket();
                SP.type = "SendDataFromTerminalToUser";
                SP.command = commandVal;
                SP.data = dataVal;
                string strSendData = JsonConvert.SerializeObject(SP);
                if (socket.IsAlive)
                    socket.Send(strSendData);
            });
        }
        public void ProcessDataReceived(string RcvData)
        {
            SocketPacket SP = JsonConvert.DeserializeObject<SocketPacket>(RcvData);
            string typeVal = SP.type;
            object dataVal = SP.data;

            MT4SettingPacket MT4Setting = new MT4SettingPacket();

            switch (typeVal)
            {
                case "userid":
                    UserConnected(dataVal.ToString());
                    break;
                case "DisconnectedUser":
                    UserDisconnected();
                    break;



                case "TryLogin":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TryLogin", MT4Setting);
                    break;
                case "TryDisconnect":
                    MT4SettingReceived("TryDisconnect", null);
                    break;
                case "TrySelectCurrency":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TrySelectCurrency", MT4Setting);
                    break;
                case "TryReBalance":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TryReBalance", MT4Setting);
                    break;
                case "TryStartToOpen":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TryStartToOpen", MT4Setting);
                    break;
                case "TryStartToClose":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TryStartToClose", MT4Setting);
                    break;
                case "TrySaveSetting":
                    MT4Setting = JsonConvert.DeserializeObject<MT4SettingPacket>(dataVal.ToString());
                    MT4SettingReceived("TrySaveSetting", MT4Setting);
                    break;
            }
        }

    }
}
