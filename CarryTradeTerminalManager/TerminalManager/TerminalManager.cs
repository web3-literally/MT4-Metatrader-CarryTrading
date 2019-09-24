using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MtApi.Monitors;
using MtApi;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace TerminalManager
{
    //WebSocket EventHandler
    delegate void UserlistChangedEvent(List<string> listUsers);
    delegate void ReceiveMessageEvent(string strFrom, string strText);
    delegate void UserConnectEvent(string strUserId);
    delegate void UserDisonnectEvent();
    delegate void MT4SettingEvent(string strCommand, MT4SettingPacket MT4SettingPck);

    //MT4 EventHandler
    delegate void apiClient_QuoteEvent(MtQuote quote = null);
    public partial class TerminalManager : Form
    {
        WebSocketNode WSNode;
        MT4Controller MT4Ctrl1, MT4Ctrl2;

        private volatile bool _isUiQuoteUpdateReady = true;

        
        private string strCurrentSymbol;
        private int nSlippage1, nSlippage2;
        private double dVolumn1, dVolumn2;
        private int nCurSpread1 = 0, nCurSpread2 = 0;

        private int nSpreadOpenThreshold = 1, nSpreadCloseThreshold = 1;
        private double dMarginTarget = 100, dLowLimitMargin = 150;
        private int nSplitCnt = 1;

        private int nWithrawRetryCount, nDepositeRetryCount;
        private bool bWorking = false;
        private bool bCloseOrderWorking = false;
        private List<int> listMagicNumberToClose = new List<int>();
        private int nPort1, nPort2;
        string strlogin1;
        string strPassword1;
        string strMT4Server1;
        string strlogin2;
        string strPassword2;
        string strMT4Server2;
        string UAGTRADE_Username, UAGTRADE_Password, accountFrom, accountTo;



        public TerminalManager()
        {
            InitializeComponent();
            WSNode = new WebSocketNode();
            WSNode.UserConnected += OnSocketUserConnected;
            WSNode.UserDisconnected += OnSocketUserDisconnected;
            WSNode.MT4SettingReceived += OnMT4Setting;

            MT4Ctrl1 = new MT4Controller();
            MT4Ctrl2 = new MT4Controller();
            MT4Ctrl1.evtApiClientConnected += On_Terminal1_Connected;
            MT4Ctrl2.evtApiClientConnected += On_Terminal2_Connected;
            MT4Ctrl1.evtApiClientDisonnected += On_Terminal1_Disconnected;
            MT4Ctrl2.evtApiClientDisonnected += On_Terminal2_Disconnected;
            MT4Ctrl1.evtApiClientQuoteUpdate += On_Terminal1_QuoteUpdate;
            MT4Ctrl2.evtApiClientQuoteUpdate += On_Terminal2_QuoteUpdate;
        }

        private Task<TResult> Execute<TResult>(Func<TResult> func)
        {
            return Task.Factory.StartNew(() =>
            {
                var result = default(TResult);
                try
                {
                    result = func();
                }
                catch (MtConnectionException ex)
                {
                    //PrintLog("MtExecutionException: " + ex.Message);
                }
                catch (MtExecutionException ex)
                {
                    //PrintLog("MtExecutionException: " + ex.Message + "; ErrorCode = " + ex.ErrorCode);
                }

                return result;
            });
        }
        private Task Execute(Action action)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    action();
                }
                catch (MtConnectionException ex)
                {
                    //PrintLog("MtExecutionException: " + ex.Message);
                }
                catch (MtExecutionException ex)
                {
                    //PrintLog("MtExecutionException: " + ex.Message + "; ErrorCode = " + ex.ErrorCode);
                }
            });
        }
        private void RunOnUiThread(Action action)
        {
            if (!IsDisposed)
            {
                BeginInvoke(action);
            }
        }
        
        /// <summary>
        /// Functions From Socket!!!
        /// </summary>
        /// <param name="strUserId"></param>

        private void OnSocketUserConnected(string strUserId)
        {
            RunOnUiThread(()=> {
                m_lblSocketState.Text = "User Connected: (ID: " + strUserId + ")";
            });
        }
        private void OnSocketUserDisconnected()
        {
            RunOnUiThread(() => {
                m_lblSocketState.Text = "User Disonnected!";
            });
        }
        private void OnMT4Setting(string strCommand, MT4SettingPacket MT4SettingPck)
        {
            if (strCommand == "TryLogin")
            {
                int.TryParse(m_txtPortNo1.Text, out nPort1);
                int.TryParse(m_txtPortNo2.Text, out nPort2);

                strlogin1 = MT4SettingPck.AccountNo1.ToString();
                strPassword1 = MT4SettingPck.Password1;
                strMT4Server1 = MT4SettingPck.MT4Server1;
                strlogin2 = MT4SettingPck.AccountNo2.ToString();
                strPassword2 = MT4SettingPck.Password2;
                strMT4Server2 = MT4SettingPck.MT4Server2;

                TryLogin();
            }
            else if (strCommand == "TryDisconnect")
            {
                TryDisconnect();
            }
            else if (strCommand == "TrySelectCurrency")
            {
                strCurrentSymbol = MT4SettingPck.CurrencyPair;
                TrySelectCurrency();
            }
            else if (strCommand == "TryReBalance")
            {
                UAGTRADE_Username = MT4SettingPck.UsernameToRebalance;
                UAGTRADE_Password = MT4SettingPck.PasswordToRebalance;
                nWithrawRetryCount = int.Parse(MT4SettingPck.WithdrawRetryCount);
                nDepositeRetryCount = int.Parse(MT4SettingPck.DepositeRetryCount);
                TryReBalance();
            }
            else if (strCommand == "TryStartToOpen")
            {
                nSlippage1 = int.Parse(MT4SettingPck.slippage1);
                nSlippage2 = int.Parse(MT4SettingPck.slippage2);
                nSpreadOpenThreshold = int.Parse(MT4SettingPck.SpreadToOpen);
                dMarginTarget = double.Parse(MT4SettingPck.MarginTargetPercent);
                nSplitCnt = int.Parse(MT4SettingPck.SpliteCount);

                string strSymbol1 = MT4Ctrl1.currentQuote.Instrument;
                string strSymbol2 = MT4Ctrl2.currentQuote.Instrument;

                if (strSymbol1 == strSymbol2 && strSymbol1 != "")
                {
                    strCurrentSymbol = strSymbol1;
                    bWorking = true;
                }
            }
            else if (strCommand == "TryStartToClose")
            {
                dLowLimitMargin = double.Parse(MT4SettingPck.LowLimitMarginPercent);
                nSpreadCloseThreshold = (int)m_numSpreadCloseThreshold.Value;
                m_txtMagicNumbersToClose.Text = MT4SettingPck.MagicNumbersToClose;

                listMagicNumberToClose.Clear();
                foreach (string strMagicNumber in m_txtMagicNumbersToClose.Text.Split(','))
                {
                    listMagicNumberToClose.Add(int.Parse(strMagicNumber));
                }

                bCloseOrderWorking = true;
            }
            else if (strCommand == "TrySaveSetting")
            {
                LoadSetting(MT4SettingPck);
                SaveSetting();
            }
        }


        /// <summary>
        /// Functions From UI!!!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void TerminalManager_Load(object sender, EventArgs e)
        {
            MT4SettingPacket SettingPck = ReadFromXmlFile<MT4SettingPacket>("SettingData.xml");
            LoadSetting(SettingPck);
        }
        private void TerminalManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            TryDisconnect();
            //m_btnSaveSettings_Click(null, null);
        }
        private void m_btnClose_Click(object sender, EventArgs e)
        {
            TryDisconnect();
            Close();
        }
        private void m_btnLogin_Click(object sender, EventArgs e)
        {
            int.TryParse(m_txtPortNo1.Text, out nPort1);
            int.TryParse(m_txtPortNo2.Text, out nPort2);

            strlogin1 = m_txtLoginID1.Text;
            strPassword1 = m_txtPassword1.Text;
            strMT4Server1 = m_txtMT4Server1.Text;
            strlogin2 = m_txtLoginID2.Text;
            strPassword2 = m_txtPassword2.Text;
            strMT4Server2 = m_txtMT4Server2.Text;

            TryLogin();
            m_timerUpdateQuote.Enabled = true;
            WSNode.Socket_Connect();
        }
        private void m_btnDisconnect_Click(object sender, EventArgs e)
        {
            TryDisconnect();
        }
        private void m_btnSelectCurrency_Click(object sender, EventArgs e)
        {
            var selectedItem = m_comboCurrencyPairs.SelectedItem;
            if (selectedItem == null) return;

            strCurrentSymbol = selectedItem.ToString();
            TrySelectCurrency();
        }
        private void m_btnStartTrade_Click(object sender, EventArgs e)
        {
            if (MT4Ctrl1.listOrders.Count() != 0 || MT4Ctrl2.listOrders.Count() != 0)
            {
                if (MessageBox.Show("Hello! There are already open orders!\nDo you want trade continously?",
                    "Warning!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                    return;
            }
            nSlippage1 = (int)m_numSloppage1.Value;
            nSlippage2 = (int)m_numSloppage2.Value;
            nSpreadOpenThreshold = (int)m_numSpreadOpenThreshold.Value;
            nSpreadCloseThreshold = (int)m_numSpreadCloseThreshold.Value;
            dMarginTarget = double.Parse(m_txtRemainMarginTarget.Text);
            dLowLimitMargin = double.Parse(m_txtLowLimitPercent.Text);
            nSplitCnt = (int)m_numSplitCnt.Value;

            string strSymbol1 = MT4Ctrl1.currentQuote.Instrument;
            string strSymbol2 = MT4Ctrl2.currentQuote.Instrument;

            if (strSymbol1 == strSymbol2 && strSymbol1 != "")
            {
                strCurrentSymbol = strSymbol1;
                bWorking = true;
            }
        }
        private void m_btnReBalance_Click(object sender, EventArgs e)
        {
            UAGTRADE_Username = m_txtUAGTRADE_Username.Text;
            UAGTRADE_Password = m_txtUAGTRADE_Password.Text;
            nWithrawRetryCount = (int)m_numWithrawRetryCount.Value;
            nDepositeRetryCount = (int)m_numDepositeRetryCount.Value;

            TryReBalance();
        }
        private void m_btnCloseAllPosition_Click(object sender, EventArgs e)
        {
            listMagicNumberToClose.Clear();
            foreach (string strMagicNumber in m_txtMagicNumbersToClose.Text.Split(','))
            {
                listMagicNumberToClose.Add(int.Parse(strMagicNumber));
            }
            bCloseOrderWorking = true;
        }
        private void m_timerUpdateQuote_Tick(object sender, EventArgs e)
        {
            MT4Ctrl1.GetAccountInfomation();
            MT4Ctrl1.GetOpenOrders();
            MT4Ctrl2.GetAccountInfomation();
            MT4Ctrl2.GetOpenOrders();
            UpdatedCurrentQuote();
        }
        public void On_Terminal1_Connected(MtQuote quote)
        {
            m_picConnectState1.Image = global::TerminalManager.Properties.Resources._checked;
            DoUpdateSymbols();
            DoChangeAccount();
            WSNode.SendDataToUser("MT4TerminalConnected1", null);
        }
        public void On_Terminal2_Connected(MtQuote quote)
        {
            m_picConnectState2.Image = global::TerminalManager.Properties.Resources._checked;
            DoUpdateSymbols();
            DoChangeAccount();
            WSNode.SendDataToUser("MT4TerminalConnected2", null);
        }
        public void On_Terminal1_Disconnected(MtQuote quote)
        {
            m_picConnectState1.Image = global::TerminalManager.Properties.Resources.cross;
            //DoUpdateSymbols();
            UpdatedCurrentQuote();
            WSNode.SendDataToUser("MT4TerminalDisconnected1", null);
        }
        public void On_Terminal2_Disconnected(MtQuote quote)
        {
            m_picConnectState2.Image = global::TerminalManager.Properties.Resources.cross;
            //DoUpdateSymbols();
            UpdatedCurrentQuote();
            WSNode.SendDataToUser("MT4TerminalDisconnected2", null);
        }
        public void On_Terminal1_QuoteUpdate(MtQuote quote)
        {
            if (_isUiQuoteUpdateReady)
            {
                _isUiQuoteUpdateReady = false;
                RunOnUiThread(() =>
                {
                    m_txtSymbol1.Text = MT4Ctrl1.currentQuote.Instrument;
                    m_txtBid1.Text = MT4Ctrl1.currentQuote.Bid.ToString(CultureInfo.CurrentCulture);
                    m_txtAsk1.Text = MT4Ctrl1.currentQuote.Ask.ToString(CultureInfo.CurrentCulture);
                    m_txtExpertHandle1.Text = MT4Ctrl1.currentQuote.ExpertHandle.ToString();

                    MT4CurrentState CurrentQuoteState = new MT4CurrentState();
                    CurrentQuoteState.Symbol1 = m_txtSymbol1.Text;
                    CurrentQuoteState.Bid1 = m_txtBid1.Text;
                    CurrentQuoteState.Ask1 = m_txtAsk1.Text;
                    CurrentQuoteState.ExpertHandle1 = m_txtExpertHandle1.Text;

                    WSNode.SendDataToUser("TerminalUpdate1", CurrentQuoteState);
                });
                _isUiQuoteUpdateReady = true;
            }
            nCurSpread1 = (int)MT4Ctrl1.GetSymbolSpread(quote.Instrument);
            if (bWorking)
            {
                if (CanRebalance()) TryReBalance();
                //nCurSpread1 = (int)(quote.Ask * 100000) - (int)(quote.Bid * 100000);
                if (nCurSpread1 == nCurSpread2 && nCurSpread1 < nSpreadOpenThreshold)
                    DoOpenOrder();
            }
            if (bCloseOrderWorking)
            {
                if (nCurSpread1 == nCurSpread2 && nCurSpread1 < nSpreadCloseThreshold)
                    DoCloseOrder();
            }
        }
        public void On_Terminal2_QuoteUpdate(MtQuote quote)
        {
            if (_isUiQuoteUpdateReady)
            {
                _isUiQuoteUpdateReady = false;
                RunOnUiThread(() =>
                {
                    m_txtSymbol2.Text = MT4Ctrl1.currentQuote.Instrument;
                    m_txtBid2.Text = MT4Ctrl1.currentQuote.Bid.ToString(CultureInfo.CurrentCulture);
                    m_txtAsk2.Text = MT4Ctrl1.currentQuote.Ask.ToString(CultureInfo.CurrentCulture);
                    m_txtExpertHandle2.Text = MT4Ctrl1.currentQuote.ExpertHandle.ToString();

                    MT4CurrentState CurrentQuoteState = new MT4CurrentState();
                    CurrentQuoteState.Symbol2 = m_txtSymbol2.Text;
                    CurrentQuoteState.Bid2 = m_txtBid2.Text;
                    CurrentQuoteState.Ask2 = m_txtAsk2.Text;
                    CurrentQuoteState.ExpertHandle2 = m_txtExpertHandle2.Text;

                    WSNode.SendDataToUser("TerminalUpdate2", CurrentQuoteState);
                });
                _isUiQuoteUpdateReady = true;
            }

            nCurSpread2 = (int)MT4Ctrl2.GetSymbolSpread(quote.Instrument);
            if (bWorking)
            {
                //nCurSpread2 = (int)(quote.Ask * 100000) - (int)(quote.Bid * 100000);
                if (nCurSpread1 == nCurSpread2 && nCurSpread1 < nSpreadOpenThreshold)
                    DoOpenOrder();
            }
            if (bCloseOrderWorking)
            {
                if (nCurSpread1 == nCurSpread2 && nCurSpread1 < nSpreadCloseThreshold)
                    DoCloseOrder();
            }

        }
        private void m_btnSaveSettings_Click(object sender, EventArgs e)
        {
            MT4SettingPacket SettingPck = SaveSetting();
            WSNode.SendDataToUser("MT4Settings", SettingPck);
        }
        
        public void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        public T ReadFromXmlFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        
        
        private void UpdatedCurrentQuote()
        {
            if (_isUiQuoteUpdateReady)
            {
                _isUiQuoteUpdateReady = false;
                RunOnUiThread(() => {
                    MT4CurrentState CurrentState = new MT4CurrentState();

                    m_txtSymbol1.Text = MT4Ctrl1.currentQuote.Instrument;
                    m_txtBid1.Text = MT4Ctrl1.currentQuote.Bid.ToString(CultureInfo.CurrentCulture);
                    m_txtAsk1.Text = MT4Ctrl1.currentQuote.Ask.ToString(CultureInfo.CurrentCulture);
                    m_txtExpertHandle1.Text = MT4Ctrl1.currentQuote.ExpertHandle.ToString();

                    CurrentState.Symbol1 = m_txtSymbol1.Text;
                    CurrentState.Bid1 = m_txtBid1.Text;
                    CurrentState.Ask1 = m_txtAsk1.Text;
                    CurrentState.ExpertHandle1 = m_txtExpertHandle1.Text;

                    if (MT4Ctrl1.bAvailable)
                    {
                        if (mtOrderBindingSource.DataSource != MT4Ctrl1.listOrders)
                        {
                            mtOrderBindingSource.DataSource = MT4Ctrl1.listOrders;
                            WSNode.SendDataToUser("TerminalOrders1", MT4Ctrl1.listOrders);
                        }
                    }
                    else
                        m_dataGridOpenOrders1.DataSource = null;

                    m_txtBalance1.Text = MT4Ctrl1.AccountBalance.ToString();
                    m_txtEquity1.Text = MT4Ctrl1.AccountEquity.ToString();
                    m_txtFreeMargin1.Text = MT4Ctrl1.AccountFreeMargin.ToString();
                    m_txtMargin1.Text = MT4Ctrl1.AccountMargin.ToString();
                    m_txtProfit1.Text = MT4Ctrl1.AccountProfit.ToString();
                    m_txtSwapBuy1.Text = MT4Ctrl1.dSwapBuy.ToString();
                    m_txtSwapSell1.Text = MT4Ctrl1.dSwapSell.ToString();
                    m_txtAccountNo1.Text = MT4Ctrl1.AccountNumber.ToString();

                    CurrentState.Balance1 = m_txtBalance1.Text;
                    CurrentState.Equity1 = m_txtEquity1.Text;
                    CurrentState.FreeMargin1 = m_txtFreeMargin1.Text;
                    CurrentState.Margin1 = m_txtMargin1.Text;
                    CurrentState.Profit1 = m_txtProfit1.Text;
                    CurrentState.Swap_Buy1 = m_txtSwapBuy1.Text;
                    CurrentState.Swap_Sell1 = m_txtSwapSell1.Text;
                    CurrentState.Account_No1 = m_txtAccountNo1.Text;
                    

                    m_txtSymbol2.Text = MT4Ctrl2.currentQuote.Instrument;
                    m_txtBid2.Text = MT4Ctrl2.currentQuote.Bid.ToString(CultureInfo.CurrentCulture);
                    m_txtAsk2.Text = MT4Ctrl2.currentQuote.Ask.ToString(CultureInfo.CurrentCulture);
                    m_txtExpertHandle2.Text = MT4Ctrl2.currentQuote.ExpertHandle.ToString();

                    CurrentState.Symbol2 = m_txtSymbol2.Text;
                    CurrentState.Bid2 = m_txtBid2.Text;
                    CurrentState.Ask2 = m_txtAsk2.Text;
                    CurrentState.ExpertHandle2 = m_txtExpertHandle2.Text;

                    if (MT4Ctrl2.bAvailable)
                    {
                        if (mtOrderBindingSource1.DataSource != MT4Ctrl2.listOrders)
                        {
                            mtOrderBindingSource1.DataSource = MT4Ctrl2.listOrders;
                            WSNode.SendDataToUser("TerminalOrders2", MT4Ctrl2.listOrders);
                        }
                    }
                    else
                        m_dataGridOpenOrders2.DataSource = null;

                    m_txtBalance2.Text = MT4Ctrl2.AccountBalance.ToString();
                    m_txtEquity2.Text = MT4Ctrl2.AccountEquity.ToString();
                    m_txtFreeMargin2.Text = MT4Ctrl2.AccountFreeMargin.ToString();
                    m_txtMargin2.Text = MT4Ctrl2.AccountMargin.ToString();
                    m_txtProfit2.Text = MT4Ctrl2.AccountProfit.ToString();
                    m_txtSwapBuy2.Text = MT4Ctrl2.dSwapBuy.ToString();
                    m_txtSwapSell2.Text = MT4Ctrl2.dSwapSell.ToString();
                    m_txtAccountNo2.Text = MT4Ctrl2.AccountNumber.ToString();

                    CurrentState.Balance2 = m_txtBalance2.Text;
                    CurrentState.Equity2 = m_txtEquity2.Text;
                    CurrentState.FreeMargin2 = m_txtFreeMargin2.Text;
                    CurrentState.Margin2 = m_txtMargin2.Text;
                    CurrentState.Profit2 = m_txtProfit2.Text;
                    CurrentState.Swap_Buy2 = m_txtSwapBuy2.Text;
                    CurrentState.Swap_Sell2 = m_txtSwapSell2.Text;
                    CurrentState.Account_No2 = m_txtAccountNo2.Text;

                    if (MT4Ctrl1.bAvailable)
                    {
                        if (MT4Ctrl1.bIsWithSwap)
                        {
                            m_lblSwapState1.ForeColor = Color.Yellow;
                            m_lblSwapState1.Text = "This Account get Swap Mode!";
                        }
                        else
                        {
                            m_lblSwapState1.ForeColor = Color.Blue;
                            m_lblSwapState1.Text = "This Account don't get Swap Mode!";
                        }
                    }
                    else
                    {
                        m_lblSwapState1.Text = "";
                    }

                    if (MT4Ctrl2.bAvailable)
                    {
                        if (MT4Ctrl2.bIsWithSwap)
                        {
                            m_lblSwapState2.ForeColor = Color.Yellow;
                            m_lblSwapState2.Text = "This Account get Swap Mode!";
                        }
                        else
                        {
                            m_lblSwapState2.ForeColor = Color.Blue;
                            m_lblSwapState2.Text = "This Account don't get Swap Mode!";
                        }
                    }
                    else
                    {
                        m_lblSwapState2.Text = "";
                    }

                    if (MT4Ctrl1.bAvailable == false)
                        m_dataGridOpenOrders1.DataSource = null;
                    if (MT4Ctrl2.bAvailable == false)
                        m_dataGridOpenOrders2.DataSource = null;
                    if (MT4Ctrl1.bAvailable == false && MT4Ctrl2.bAvailable == false)
                        m_comboCurrencyPairs.Items.Clear();

                    //SendData To Socket!!!
                    WSNode.SendDataToUser("MT4CurrentState", CurrentState);
                });
                _isUiQuoteUpdateReady = true;
            }
        }
        private void DoChangeAccount()
        {
            if (!MT4Ctrl1.bAvailable || !MT4Ctrl2.bAvailable)
                return;

            if (!string.IsNullOrEmpty(strlogin1) && !string.IsNullOrEmpty(strPassword1) && !string.IsNullOrEmpty(strMT4Server1))
                MT4Ctrl1.ChangeAccount(strlogin1, strPassword1, strMT4Server1);
            if (!string.IsNullOrEmpty(strlogin2) && !string.IsNullOrEmpty(strPassword2) && !string.IsNullOrEmpty(strMT4Server2))
                MT4Ctrl2.ChangeAccount(strlogin2, strPassword2, strMT4Server2);

            if (_isUiQuoteUpdateReady)
            {
                _isUiQuoteUpdateReady = false;
                RunOnUiThread(() =>
                {
                    if (MT4Ctrl1.bIsWithSwap)
                    {
                        m_lblSwapState1.ForeColor = Color.Yellow;
                        m_lblSwapState1.Text = "This Account get Swap Mode!";
                    }
                    else
                    {
                        m_lblSwapState1.ForeColor = Color.Blue;
                        m_lblSwapState1.Text = "This Account don't get Swap Mode!";
                    }

                    if (MT4Ctrl2.bIsWithSwap)
                    {
                        m_lblSwapState2.ForeColor = Color.Yellow;
                        m_lblSwapState2.Text = "This Account get Swap Mode!";
                    }
                    else
                    {
                        m_lblSwapState2.ForeColor = Color.Blue;
                        m_lblSwapState2.Text = "This Account don't get Swap Mode!";
                    }
                });
                _isUiQuoteUpdateReady = true;
            }
        }
        private void DoOpenOrder()
        {
            MT4Ctrl1.GetAccountInfomation();
            MT4Ctrl2.GetAccountInfomation();
            int nAccountLeverage1 = MT4Ctrl1.GetAccountLeverage();
            int nAccountLeverage2 = MT4Ctrl2.GetAccountLeverage();
            double dSymbolTradeContractSize1 = MT4Ctrl1.GetSymbolTradeContractSize(strCurrentSymbol);
            double dSymbolTradeContractSize2 = MT4Ctrl2.GetSymbolTradeContractSize(strCurrentSymbol);

            if (nAccountLeverage1 == 0 || nAccountLeverage2 == 0 || dSymbolTradeContractSize1 == 0 || dSymbolTradeContractSize2 == 0)
                return;

            double dMarginPerLot1 = dSymbolTradeContractSize1 / (double)nAccountLeverage1 + MT4Ctrl1.dCommissionPerLot;
            double dMarginPerLot2 = dSymbolTradeContractSize2 / (double)nAccountLeverage2 + MT4Ctrl2.dCommissionPerLot;

            double dAvailableMarginToOpen1, dAvailableMarginToOpen2;
            //Get AvailableMarginToOpen of MT4-1 
            if (MT4Ctrl1.AccountMargin == 0)
            {
                dAvailableMarginToOpen1 = MT4Ctrl1.AccountEquity / dMarginTarget * 100;
            }
            else
            {
                double dMarginPercent1 = MT4Ctrl1.AccountEquity / MT4Ctrl1.AccountMargin * 100;
                if (dMarginPercent1 <= dMarginTarget) return;

                double dAvailableMarginToOpenPercent1 = dMarginPercent1 - dMarginTarget;
                dAvailableMarginToOpen1 = MT4Ctrl1.AccountEquity * dAvailableMarginToOpenPercent1;
            }
            //Get AvailableMarginToOpen of MT4-2
            if (MT4Ctrl2.AccountMargin == 0)
            {
                dAvailableMarginToOpen2 = MT4Ctrl2.AccountEquity / dMarginTarget * 100;
            }
            else
            {
                double dMarginPercent2 = MT4Ctrl2.AccountEquity / MT4Ctrl2.AccountMargin * 100;
                if (dMarginPercent2 <= dMarginTarget) return;

                double dAvailableMarginToOpenPercent2 = dMarginPercent2 - dMarginTarget;
                dAvailableMarginToOpen2 = MT4Ctrl1.AccountEquity * dAvailableMarginToOpenPercent2;
            }

            double dLotToOpen1 = dAvailableMarginToOpen1 / dMarginPerLot1;
            double dLotToOpen2 = dAvailableMarginToOpen2 / dMarginPerLot2;
            double dLotToOpen = 0;

            if (dLotToOpen1 < dLotToOpen2)
                dLotToOpen = dLotToOpen1 / nSplitCnt;
            else
                dLotToOpen = dLotToOpen2 / nSplitCnt;

            dLotToOpen = (double)Math.Round(dLotToOpen * 100f) / 100f;
            

            //Open Order
            if ((MT4Ctrl1.bIsWithSwap && MT4Ctrl2.bIsWithSwap) || (MT4Ctrl1.bIsWithSwap && MT4Ctrl2.bIsWithSwap))
            {
                m_lblState.Text = "Two Account are not Matched!";
                return;
            }

            if (MT4Ctrl1.bIsWithSwap && !MT4Ctrl2.bIsWithSwap)
            {
                if (MT4Ctrl1.dSwapBuy > 0 && MT4Ctrl1.dSwapSell < 0)
                {
                    for (int i = 0; i < nSplitCnt; i++)
                    {
                        int nMagicCode = DateTime.Now.Millisecond;
                        MT4Ctrl1.OpenOrder(strCurrentSymbol, TradeOperation.OP_BUY, dLotToOpen, nSlippage1, nMagicCode);
                        MT4Ctrl2.OpenOrder(strCurrentSymbol, TradeOperation.OP_SELL, dLotToOpen, nSlippage2, nMagicCode);
                    }
                }
                else
                {
                    for (int i = 0; i < nSplitCnt; i++)
                    {
                        int nMagicCode = DateTime.Now.Millisecond;
                        MT4Ctrl1.OpenOrder(strCurrentSymbol, TradeOperation.OP_SELL, dLotToOpen, nSlippage1, nMagicCode);
                        MT4Ctrl2.OpenOrder(strCurrentSymbol, TradeOperation.OP_BUY, dLotToOpen, nSlippage2, nMagicCode);
                    }
                }
            }

            if (!MT4Ctrl1.bIsWithSwap && MT4Ctrl2.bIsWithSwap)
            {
                if (MT4Ctrl2.dSwapBuy > 0 && MT4Ctrl2.dSwapSell < 0)
                {
                    for (int i = 0; i < nSplitCnt; i++)
                    {
                        int nMagicCode = DateTime.Now.Millisecond;
                        MT4Ctrl1.OpenOrder(strCurrentSymbol, TradeOperation.OP_SELL, dLotToOpen, nSlippage1, nMagicCode);
                        MT4Ctrl2.OpenOrder(strCurrentSymbol, TradeOperation.OP_BUY, dLotToOpen, nSlippage2, nMagicCode);
                    }
                }
                else
                {
                    for (int i = 0; i < nSplitCnt; i++)
                    {
                        int nMagicCode = DateTime.Now.Millisecond;
                        MT4Ctrl1.OpenOrder(strCurrentSymbol, TradeOperation.OP_BUY, dLotToOpen, nSlippage1, nMagicCode);
                        MT4Ctrl2.OpenOrder(strCurrentSymbol, TradeOperation.OP_SELL, dLotToOpen, nSlippage2, nMagicCode);
                    }
                }
            }

        }
        private void DoCloseOrder()
        {
            List<MtOrder> listOrders1 = MT4Ctrl1.listOrders;
            List<MtOrder> listOrders2 = MT4Ctrl2.listOrders;
            foreach(MtOrder order1 in listOrders1)
            {
                int nTicket1 = 0, nTicket2 = 0;
                nTicket1 = order1.Ticket;
                int nMagic = order1.MagicNumber;

                if (listMagicNumberToClose.IndexOf(nMagic) == -1)
                    break;

                foreach (MtOrder order2 in listOrders1)
                {
                    if (order2.MagicNumber == nMagic)
                    {
                        nTicket2 = order2.Ticket;
                        break;
                    }
                }
                if (nTicket1 != 0 && nTicket2 != 0)
                {
                    MT4Ctrl1.CloseOrder(nTicket1, nSlippage1);
                    MT4Ctrl2.CloseOrder(nTicket2, nSlippage2);
                }
            }
        }
        private void DoUpdateSymbols()
        {
            if (!MT4Ctrl1.bAvailable || !MT4Ctrl1.bAvailable) return;
            if (_isUiQuoteUpdateReady)
            {
                _isUiQuoteUpdateReady = false;
                RunOnUiThread(() =>
                {
                    List<string> listSymbols = new List<string>();
                    listSymbols = MT4Ctrl1.listSymbols;
                    if (listSymbols.Count() == 0)
                        listSymbols = MT4Ctrl2.listSymbols;
                    m_comboCurrencyPairs.Items.Clear();
                    foreach (string strSymbol in listSymbols)
                    {
                        m_comboCurrencyPairs.Items.Add(strSymbol);
                    }
                    WSNode.SendDataToUser("MT4Symbols", listSymbols);
                });
                _isUiQuoteUpdateReady = true;
            }
        }
        private bool CanRebalance()
        {
            MT4Ctrl1.GetAccountInfomation();
            MT4Ctrl2.GetAccountInfomation();

            if (MT4Ctrl1.AccountMargin == 0 && MT4Ctrl2.AccountMargin == 0)
            {
                if (MT4Ctrl1.AccountEquity != MT4Ctrl2.AccountEquity)
                    return true;
                else
                    return false;
            }

            double dMarginPercent1 = MT4Ctrl1.AccountEquity / MT4Ctrl1.AccountMargin * 100;
            double dMarginPercent2 = MT4Ctrl2.AccountEquity / MT4Ctrl2.AccountMargin * 100;

            if (dMarginPercent1 <= dLowLimitMargin || dMarginPercent2 <= dLowLimitMargin)
                return true;
            
            return false;
        }

        private MT4SettingPacket SaveSetting()
        {
            MT4SettingPacket SettingPck = new MT4SettingPacket();
            SettingPck.Port1 = m_txtPortNo1.Text;
            SettingPck.AccountNo1 = m_txtLoginID1.Text;
            SettingPck.Password1 = m_txtPassword1.Text;
            SettingPck.MT4Server1 = m_txtMT4Server1.Text;
            SettingPck.slippage1 = m_numSloppage1.Value.ToString();

            SettingPck.Port2 = m_txtPortNo2.Text;
            SettingPck.AccountNo2 = m_txtLoginID2.Text;
            SettingPck.Password2 = m_txtPassword2.Text;
            SettingPck.MT4Server2 = m_txtMT4Server2.Text;
            SettingPck.slippage2 = m_numSloppage2.Value.ToString();

            SettingPck.SpreadToOpen = m_numSpreadOpenThreshold.Value.ToString();
            SettingPck.SpreadToClose = m_numSpreadCloseThreshold.Value.ToString();
            SettingPck.CurrencyPair = m_comboCurrencyPairs.Text;
            SettingPck.MarginTargetPercent = m_txtRemainMarginTarget.Text;
            SettingPck.LowLimitMarginPercent = m_txtLowLimitPercent.Text;
            SettingPck.SpliteCount = m_numSplitCnt.Value.ToString();
            SettingPck.UsernameToRebalance = m_txtUAGTRADE_Username.Text;
            SettingPck.PasswordToRebalance = m_txtUAGTRADE_Password.Text;
            SettingPck.WithdrawRetryCount = m_numWithrawRetryCount.Text;
            SettingPck.DepositeRetryCount = m_numDepositeRetryCount.Text;
            SettingPck.MagicNumbersToClose = m_txtMagicNumbersToClose.Text;

            WriteToXmlFile("SettingData.xml", SettingPck);

            return SettingPck;
        }
        private void LoadSetting(MT4SettingPacket SettingPck)
        {
            RunOnUiThread(() =>
            {
                if (SettingPck.Port1 != null || SettingPck.Port1 != "")
                    m_txtPortNo1.Text = SettingPck.Port1;

                m_txtLoginID1.Text = SettingPck.AccountNo1;
                m_txtPassword1.Text = SettingPck.Password1;
                m_txtMT4Server1.Text = SettingPck.MT4Server1;
                m_numSloppage1.Value = decimal.Parse(SettingPck.slippage1);

                if (SettingPck.Port2 != null || SettingPck.Port2 != "")
                    m_txtPortNo2.Text = SettingPck.Port2;

                m_txtLoginID2.Text = SettingPck.AccountNo2;
                m_txtPassword2.Text = SettingPck.Password2;
                m_txtMT4Server2.Text = SettingPck.MT4Server2;
                m_numSloppage2.Value = decimal.Parse(SettingPck.slippage2);

                m_numSpreadOpenThreshold.Value = decimal.Parse(SettingPck.SpreadToOpen);
                m_numSpreadCloseThreshold.Value = decimal.Parse(SettingPck.SpreadToClose);
                m_comboCurrencyPairs.Text = SettingPck.CurrencyPair;
                m_txtRemainMarginTarget.Text = SettingPck.MarginTargetPercent;
                m_txtLowLimitPercent.Text = SettingPck.LowLimitMarginPercent;
                m_numSplitCnt.Value = decimal.Parse(SettingPck.SpliteCount);
                m_txtUAGTRADE_Username.Text = SettingPck.UsernameToRebalance;
                m_txtUAGTRADE_Password.Text = SettingPck.PasswordToRebalance;
                m_numWithrawRetryCount.Value = decimal.Parse(SettingPck.WithdrawRetryCount);
                m_numDepositeRetryCount.Value = decimal.Parse(SettingPck.DepositeRetryCount);
                m_txtMagicNumbersToClose.Text = SettingPck.MagicNumbersToClose;
            });    
        }

        private void TryLogin()
        {
            if (nPort1 != 0)
                MT4Ctrl1.BeginConnect(nPort1);
            if (nPort2 != 0)
                MT4Ctrl2.BeginConnect(nPort2);

            RunOnUiThread(() =>
            {
                m_timerUpdateQuote.Enabled = true;
            });
        }
        private void TryDisconnect()
        {
            RunOnUiThread(() =>
            {
                m_timerUpdateQuote.Enabled = false;
            });

            MT4Ctrl1.BeginDisconnect();
            MT4Ctrl2.BeginDisconnect();

            bWorking = false;
            MT4CurrentState CurrentState = new MT4CurrentState();
            WSNode.SendDataToUser("MT4CurrentState", CurrentState);
        }
        private void TrySelectCurrency()
        {
            MT4Ctrl1.SetCurrencyPair(strCurrentSymbol);
            MT4Ctrl2.SetCurrencyPair(strCurrentSymbol);
            UpdatedCurrentQuote();
        }
        private async void TryReBalance()
        {
            if (!MT4Ctrl1.bAvailable || !MT4Ctrl2.bAvailable || MT4Ctrl1.bDemoAccount || MT4Ctrl2.bDemoAccount)
                return;

            MT4Ctrl1.GetAccountInfomation();
            MT4Ctrl2.GetAccountInfomation();

            double dNewBalance = (MT4Ctrl1.AccountEquity + MT4Ctrl2.AccountEquity) / 2;
            double dAmount = MT4Ctrl1.AccountEquity - dNewBalance;
            if (dAmount > 0)
            {
                accountFrom = MT4Ctrl1.AccountNumber.ToString();
                accountTo = MT4Ctrl2.AccountNumber.ToString();
            }
            else
            {
                accountFrom = MT4Ctrl2.AccountNumber.ToString();
                accountTo = MT4Ctrl1.AccountNumber.ToString();
            }
            dAmount = Math.Abs(dAmount);
            dAmount = (double)Math.Round(dAmount * 100f) / 100f;


            RunOnUiThread(() =>
            {
                m_lblState.Text = "Rebalancing Now ...";
            });

            var result = await Execute(() => AccountRebalancer.startRebalance(
                10, UAGTRADE_Username, UAGTRADE_Password, accountFrom, accountTo, nWithrawRetryCount, nDepositeRetryCount));

            string strState = "";
            if (result == 0)
                strState = "Rebalancing Success!";
            else if (result == -1)
                strState = "Login Failed!";
            else if (result == -2)
                strState = "MT4 To Wallet Failed!";
            else if (result == -3)
                strState = "Wallet To MT4 Failed!";
            else if (result == -4)
                strState = "Error Happend!";

            if (result == -5)
                strState = "";
            else
                strState += "   ( WithrawRetryCount:" + AccountRebalancer.GetWithrawRetryCnt().ToString() + "     DepositeRetryCount:" + AccountRebalancer.GetDepositeCnt().ToString() +
                    "     DateTime:" + DateTime.Now.ToString() + "     Period:" + AccountRebalancer.GetPeriod().ToString() + "s )";

            RunOnUiThread(() =>
            {
                m_lblState.Text = strState;
            });
        }

    }
}

