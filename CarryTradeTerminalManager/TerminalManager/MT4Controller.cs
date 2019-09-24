using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MtApi;
using MtApi.Monitors;

namespace TerminalManager
{
    class MT4Controller
    {
        public event apiClient_QuoteEvent evtApiClientConnected;
        public event apiClient_QuoteEvent evtApiClientDisonnected;
        public event apiClient_QuoteEvent evtApiClientQuoteUpdate;



        private readonly List<Action> _groupOrderCommands = new List<Action>();
        private readonly MtApiClient _apiClient = new MtApiClient();
        private readonly TimerTradeMonitor _timerTradeMonitor;
        private readonly TimeframeTradeMonitor _timeframeTradeMonitor;

        public List<MtOrder> listOrders { get; set; } = new List<MtOrder>();
        public double AccountBalance { get; set; }
        public double AccountEquity { get; set; }
        public double AccountFreeMargin { get; set; }
        public double AccountMargin { get; set; }
        public double AccountProfit { get; set; }
        public int AccountNumber { get; set; }
        public double dSwapBuy { get; set; }
        public double dSwapSell { get; set; }


        public List<string> listSymbols { get; set; } = new List<string>();
        public MtQuote currentQuote { get; set; } = new MtQuote("", 0, 0);
        public bool bIsWithSwap { get; set; } = true;
        public double dCommissionPerLot { get; set; } = 0;

        public bool bAvailable { get; set; } = false;

        public bool bDemoAccount { get; set; } = true;

        /// <summary>
        /// MT4Controller
        /// </summary>
        public MT4Controller()
        {
            _apiClient.QuoteUpdated += apiClient_QuoteUpdated;
            _apiClient.QuoteUpdate += _apiClient_QuoteUpdate;
            _apiClient.QuoteAdded += apiClient_QuoteAdded;
            _apiClient.QuoteRemoved += apiClient_QuoteRemoved;
            _apiClient.ConnectionStateChanged += apiClient_ConnectionStateChanged;
            _apiClient.OnLastTimeBar += _apiClient_OnLastTimeBar;
            _apiClient.OnChartEvent += _apiClient_OnChartEvent;

            _timerTradeMonitor = new TimerTradeMonitor(_apiClient) { Interval = 10000 }; // 10 sec
            _timerTradeMonitor.AvailabilityOrdersChanged += _tradeMonitor_AvailabilityOrdersChanged;

            _timeframeTradeMonitor = new TimeframeTradeMonitor(_apiClient);
            _timeframeTradeMonitor.AvailabilityOrdersChanged += _tradeMonitor_AvailabilityOrdersChanged;
        }

        private void InitParams()
        {
            listOrders.Clear();
            AccountBalance = 0;
            AccountEquity = 0;
            AccountFreeMargin = 0;
            AccountMargin = 0;
            AccountProfit = 0;
            AccountNumber = 0;
            listSymbols.Clear();
            currentQuote = new MtQuote("", 0, 0);
            dSwapBuy = 0;
            dSwapSell = 0;
            bAvailable = false;
        }
        private void apiClient_ConnectionStateChanged(object sender, MtConnectionEventArgs e)
        {
            switch (e.Status)
            {
                case MtConnectionState.Connected:
                    var quotes = _apiClient.GetQuotes();
                    if (quotes != null)
                    {
                        GetAccountInfomation();
                        GetOpenOrders();
                        GetAvailableSymbols();

                        currentQuote = quotes[0];
                        bAvailable = true;
                        evtApiClientConnected(currentQuote);
                    }
                    break;
                case MtConnectionState.Disconnected:
                    InitParams();
                    bAvailable = false;
                    evtApiClientDisonnected();
                    break;
                case MtConnectionState.Failed:
                    InitParams();
                    bAvailable = false;
                    evtApiClientDisonnected();
                    break;
            }
        }

        private void apiClient_QuoteRemoved(object sender, MtQuoteEventArgs e)
        {
            if (e.Quote != null)
            {
                //RunOnUiThread(() => RemoveQuote(e.Quote));
            }
        }

        private void apiClient_QuoteAdded(object sender, MtQuoteEventArgs e)
        {
            if (e.Quote != null)
            {
                //RunOnUiThread(() => AddNewQuote(e.Quote));
                currentQuote = _apiClient.GetQuotes()[0];
            }
        }

        private void _apiClient_OnLastTimeBar(object sender, TimeBarArgs e)
        {
            //var msg =
            //    $"TimeBar: ExpertHandle = {e.ExpertHandle}, Symbol = {e.TimeBar.Symbol}, OpenTime = {e.TimeBar.OpenTime}, CloseTime = {e.TimeBar.CloseTime}, Open = {e.TimeBar.Open}, Close = {e.TimeBar.Close}, High = {e.TimeBar.High}, Low = {e.TimeBar.Low}";
            //Console.WriteLine(msg);
            //PrintLog(msg);
        }

        private void _apiClient_OnChartEvent(object sender, ChartEventArgs e)
        {
            //var msg =
            //    $"OnChartEvent: ExpertHandle = {e.ExpertHandle}, ChartId = {e.ChartId}, EventId = {e.EventId}, Lparam = {e.Lparam}, Dparam = {e.Dparam}, Sparam = {e.Sparam}";
            //Console.WriteLine(msg);
            //PrintLog(msg);
        }

        private void apiClient_QuoteUpdated(object sender, string symbol, double bid, double ask)
        {
            //Console.WriteLine(@"Quote: Symbol = {0}, Bid = {1}, Ask = {2}", symbol, bid, ask);
        }

        private void _apiClient_QuoteUpdate(object sender, MtQuoteEventArgs e)
        {
            //GetAvailableSymbols();
            //GetAccountInfomation();
            //GetOpenOrders();
            currentQuote = e.Quote;
            evtApiClientQuoteUpdate(e.Quote);
        }

        private void _tradeMonitor_AvailabilityOrdersChanged(object sender, AvailabilityOrdersEventArgs e)
        {
            //if (e.Opened != null)
            //{
            //    PrintLog($"{sender.GetType()}: Opened orders - {string.Join(", ", e.Opened.Select(o => o.Ticket).ToList())}");
            //}

            //if (e.Closed != null)
            //{
            //    PrintLog($"{sender.GetType()}: Closed orders - {string.Join(", ", e.Closed.Select(o => o.Ticket).ToList())}");
            //}
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
        public async void BeginConnect(int nPort)
        {
            if (bAvailable) return;
            await Execute(() =>
            {
                //_timerTradeMonitor.Start();
                //_timeframeTradeMonitor.Start();
                _apiClient.BeginConnect(nPort);
            });
        }
        public void BeginDisconnect()
        {
            //_timerTradeMonitor.Stop();
            //_timeframeTradeMonitor.Stop();
            _apiClient.BeginDisconnect();
        }
        public void ChangeAccount(string strLogin, string strPassword, string strMT4Server)
        {
            var resultAccountNumber = _apiClient.AccountNumber();
            if (resultAccountNumber.ToString() == strLogin) return;
            
            _apiClient.ChangeAccount(strLogin, strPassword, strMT4Server);
            GetAccountInfomation();
        }
        public void GetAvailableSymbols()
        {
            listSymbols.Clear();
            try
            {
                if (_apiClient.ConnectionState == MtConnectionState.Connected && _apiClient.IsConnected())
                {
                    for (int i = 0; i < _apiClient.SymbolsTotal(true); i++)
                    {
                        string strSymbolName = _apiClient.SymbolName(i, true);
                        listSymbols.Add(strSymbolName);
                    }
                }
            }
            catch{}
        }
        public async void GetOpenOrders()
        {
            await Execute(() =>
            {
                if (bAvailable)
                {
                    listOrders = _apiClient.GetOrders(MtApi.OrderSelectSource.MODE_TRADES);
                }
            });
        }
        public async void GetAccountInfomation()
        {
            await Execute(() =>
            {
                if (bAvailable)
                {
                    AccountBalance = _apiClient.AccountBalance();
                    AccountEquity = _apiClient.AccountEquity();
                    AccountFreeMargin = _apiClient.AccountFreeMargin();
                    AccountMargin = _apiClient.AccountMargin();
                    AccountProfit = _apiClient.AccountProfit();
                    AccountNumber = _apiClient.AccountNumber();

                    string strAccountNumber, strFirst;
                    strAccountNumber = AccountNumber.ToString();
                    strFirst = strAccountNumber.Substring(0, char.IsHighSurrogate(strAccountNumber[0]) ? 2 : 1);

                    if (strFirst == "2")
                    {
                        bIsWithSwap = false;
                        dCommissionPerLot = 17;
                        bDemoAccount = false;
                    }
                    else if (strFirst == "6")
                    {
                        bIsWithSwap = true;
                        dCommissionPerLot = 17;
                        bDemoAccount = false;
                    }
                    else if (strFirst == "7")
                    {
                        bIsWithSwap = false;
                        dCommissionPerLot = 5;
                        bDemoAccount = false;
                    }
                    else if (strFirst == "4")
                    {
                        bIsWithSwap = true;
                        dCommissionPerLot = 17;
                        bDemoAccount = true;
                    }
                    else
                    {
                        bIsWithSwap = true;
                        dCommissionPerLot = 17;
                        bDemoAccount = false;
                    }

                    // For test purpose only
                    if (strAccountNumber == "4009787")
                    {
                        bIsWithSwap = false;
                    }


                    string strSymbol = currentQuote.Instrument;
                    dSwapBuy = GetSwapBuy(strSymbol);
                    dSwapSell = GetSwapSell(strSymbol);
                    //if (dSwapBuy == 0 && dSwapSell == 0)
                    //    bIsWithSwap = false;
                    //else
                    //    bIsWithSwap = true;

                }
            });
        }
        public async void OpenOrder(string strSymbol, TradeOperation Cmd, double dVolumn, int nSlippage, int nMagic)
        {
            await Execute(() => {
                if (bAvailable)
                {
                    double dPrice = 0;
                    if (Cmd == TradeOperation.OP_BUY)
                        dPrice = currentQuote.Ask;
                    else if (Cmd == TradeOperation.OP_SELL)
                        dPrice = currentQuote.Bid;
                    _apiClient.OrderSend(strSymbol, Cmd, dVolumn, dPrice, nSlippage, 0, 0, null, nMagic);
                }
            });
        }
        public async void CloseOrder(int nTicket, int nSlippage)
        {
            await Execute(() => {
                if (bAvailable)
                {
                    _apiClient.OrderClose(nTicket, nSlippage);
                }
            });
        }
        public async void SetCurrencyPair(string strSymbolName)
        {
            await Execute(() => {
                if (bAvailable)
                {
                    long firstChartId = _apiClient.ChartFirst();
                    long newChartId = _apiClient.ChartOpen(strSymbolName, ENUM_TIMEFRAMES.PERIOD_CURRENT);
                    _apiClient.ChartApplyTemplate(newChartId, "MtApi.tpl");
                    _apiClient.ChartClose(firstChartId);
                }
            });
            
        }
        public async void CloseAllPositions()
        {
            await Execute(() =>
            {
                _apiClient.OrderCloseAll();
            });
        }

        /// <summary>
        /// For Open Order
        /// </summary>
        /// <returns></returns>
        public int GetAccountLeverage()
        {
            return _apiClient.AccountLeverage();
        }
        public double GetSwapBuy(string strSymbol)
        {
            return _apiClient.SymbolInfoDouble(strSymbol, EnumSymbolInfoDouble.SYMBOL_SWAP_LONG);
        }
        public double GetSwapSell(string strSymbol)
        {
            return _apiClient.SymbolInfoDouble(strSymbol, EnumSymbolInfoDouble.SYMBOL_SWAP_SHORT);
        }
        public double GetSymbolTradeContractSize(string strSymbol)
        {
            return _apiClient.SymbolInfoDouble(strSymbol, EnumSymbolInfoDouble.SYMBOL_TRADE_CONTRACT_SIZE);
        }
        public long GetSymbolSpread(string strSymbol)
        {
            return _apiClient.SymbolInfoInteger(strSymbol, EnumSymbolInfoInteger.SYMBOL_SPREAD);
        }
    }
}
