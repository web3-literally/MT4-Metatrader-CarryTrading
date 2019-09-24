using HtmlAgilityPack;
using Mono.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace TerminalManager
{
    class AccountRebalancer
    {
        #region STATIC VARIABLES
        static string username = "fgffsdsxdf";
        static string password = "Abc12345";
        static string accountA = "2016345";
        static string accountB = "6005547";
        // by default : withdraw from A, deposit to B

        static CookieContainer cookiejar = new CookieContainer();

        // for withdraw section
        static string SessionToken = "";   //they share the same token, which changes every login

        // for deposit section
        static string sourceWallet = "Withdraw - $";  //Wallet Source : Withdraw
        static string accountValue = "";  // this will not be scraped anymore, but taken from stored array
        static string walletValue = "";   // since "wallet source" is fixed, therefore this value is also fixed

        //static Dictionary<string, string> storedValues = new Dictionary<string, string>()
        //{
        //    { accountA, "a0pvYjhXN1hOZVY4Yk9OckNlYTRWdz09" },
        //    { accountB, "ZnZUTnczdXk1YndyV1p4c1hJdXpzdz09" },
        //    { sourceWallet, "dUdKZ0ZiczROTnBRT3RDOTk0a1N4UT09" }
        //};
        static int nWithrawCnt = 0;
        static int nDepositeCnt = 0;
        static int nPeriod = 0;

        static bool bRebalancing = false;
        #endregion

        public static int GetWithrawRetryCnt()
        {
            return nWithrawCnt;
        }
        public static int GetDepositeCnt()
        {
            return nDepositeCnt;
        }
        public static int GetPeriod()
        {
            return nPeriod;
        }
        public static int startRebalance(double dAmount, string strUsername, string strPassword, string accountFrom, string accountTo, int nWithrawRetryCount, int nDepositeRetryCount)
        {
            Stopwatch PeriodTimer = new Stopwatch();
            PeriodTimer.Start();

            try
            {
                if (bRebalancing || dAmount == 0) return -5;

                bRebalancing = true;

                username = strUsername;
                password = strPassword;
                accountA = accountFrom;
                accountB = accountTo;
                
                SessionToken = "";

                int nRet = Cycle(dAmount, nWithrawRetryCount, nDepositeRetryCount);

                nPeriod = (int)PeriodTimer.ElapsedMilliseconds / 1000;
                PeriodTimer.Stop();

                bRebalancing = false;

                return nRet;
                //switch (Cycle(dAmount))
                //{
                //    case 0:
                //        break;
                //    case -1:
                //        Console.WriteLine("\nLogin Failed :: Cycle Aborted\n");
                //        break;

                //    case -2:
                //        Console.WriteLine("\nMT4 to Wallet (Withdraw) Failed :: Cycle Aborted\n");
                //        break;

                //    case -3:
                //        Console.WriteLine("\nWallet to MT4 (Deposit) Failed :: Cycle Aborted\n");
                //        break;
                //}
            }
            catch (Exception ex)
            {
                nPeriod = (int)PeriodTimer.ElapsedMilliseconds / 1000;
                PeriodTimer.Stop();

                bRebalancing = false;
                return -4;
            }
        }
        private static int Cycle(double transferAmount, int nWithrawRetryCount, int nDepositeRetryCount)
        {
            //if (SessionToken == "")  //we don't need to login again, if we keep sessions between cycles

            if (!Login())
                return -1;

            nWithrawCnt = 0;
            while (!Withdraw(accountA, transferAmount))
            {
                nWithrawCnt++;
                if (nWithrawCnt > nWithrawRetryCount)
                    return -2;
            }

            nDepositeCnt = 0;
            while (!Deposit(accountB, transferAmount))
            {
                nDepositeCnt++;
                if (nDepositeCnt > nDepositeRetryCount)
                    return -3;
            }
            
            return 0;
        }
        private static string GetTaccode()
        {
            string GetTaccodeURL = String.Format("https://client.uagtrade.com/member/taccode?s=Y&i={0}", SessionToken);
            string jsonContent = HttpGetRequest(GetTaccodeURL);        //{"status":"OK","code":"UR2GL7"}

            dynamic jsonObj = JsonConvert.DeserializeObject(jsonContent);

            if (jsonObj.status != "OK")
            {
                Console.WriteLine(String.Format("{0} :: {1}", jsonObj.status, jsonObj.msg));
                return "Error Get Taccode";
            }
            
            return jsonObj.code;

        }
        private static bool Withdraw(string accountID, double withdrawAmount)
        {
            string withdrawURL = "https://client.uagtrade.com/member/withdraw/ajax/mt4-wdraw.php?";
            withdrawURL += String.Format("ta={0}&a={1}&t={2}", accountID, withdrawAmount, GetTaccode());
            string jsonContent = HttpGetRequest(withdrawURL);     //{"status":"OK","msg":"Withdraw successful"}

            dynamic jsonObj = JsonConvert.DeserializeObject(jsonContent);

            if (jsonObj.status != "OK")
            {
                Console.WriteLine(String.Format("{0} :: {1}", jsonObj.status, jsonObj.msg));
                return false;
            }

            return true;
        }
        private static bool Deposit(string accountID, double depositAmount)
        {
            string initURL = "https://client.uagtrade.com/member/deposit/mt4?a=deposit";
            string htmlContent = HttpGetRequest(initURL);

            HtmlNode.ElementsFlags.Remove("option");
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            if (htmlDoc.DocumentNode == null)
                return false;

            
            string accountXpath = String.Format("//select[@name=\"tradingacc\"]/option[text()=\"{0}\"]", accountID);
            HtmlNode accountNode = htmlDoc.DocumentNode.SelectSingleNode(accountXpath);
            if (accountNode == null)
                return false;

            accountValue = accountNode.Attributes["value"].Value;

            string walletXpath = String.Format("//select[@name=\"wallet\"]/option[contains(text(),\"{0}\")]", sourceWallet);
            HtmlNode walletNode = htmlDoc.DocumentNode.SelectSingleNode(walletXpath);
            if (walletNode == null)
                return false;

            walletValue = walletNode.Attributes["value"].Value;

            
            //walletValue = storedValues[sourceWallet];
            //accountValue = storedValues[accountID];

            string depositURL = "https://client.uagtrade.com/member/deposit/ajax/mt4-depo.php?";
            depositURL += String.Format("w={0}&ta={1}&a={2}&t={3}", walletValue, accountValue, depositAmount, GetTaccode());
            string jsonContent = HttpGetRequest(depositURL);       //{"status":"OK","msg":"Deposit successful"}

            dynamic jsonObj = JsonConvert.DeserializeObject(jsonContent);
            if (jsonObj.status != "OK")
            {
                Console.WriteLine(String.Format("{0} :: {1}", jsonObj.status, jsonObj.msg));
                return false;
            }

            return true;
        }
        private static bool Login()
        {
            string loginURL = "https://client.uagtrade.com/member/login_proc";
            //string coid = "SnlSb1dSK3FKZXg1Zk1OQWU3cEFMdz09";

            string loginQuery = String.Format("{0}={1}&", "username", HttpUtility.UrlEncode(username));
            loginQuery += String.Format("{0}={1}&", "password", HttpUtility.UrlEncode(password));
            //loginQuery += String.Format("{0}={1}&submitBut=", "coid", HttpUtility.UrlEncode(coid));

            string redirectURL = HttpPostRequest(loginURL, loginQuery);

            //checking login success
            if (!redirectURL.Contains("private"))  //login failed
                return false;

            string initURL = "https://client.uagtrade.com/member/withdraw/mt4?a=wdraw";
            string htmlContent = HttpGetRequest(initURL);
            Regex regex = new Regex("taccode\\?s=Y&i=(.*?)\"");
            Match match = regex.Match(htmlContent);
            if (!match.Success)
                return false;

            SessionToken = match.Groups[1].Value;

            Console.WriteLine(String.Format("\n{0} :: {1}\n", "STATUS", "Login Successfull"));
            return true;
        }
        private static string HttpGetRequest(string URL)
        {
            string htmlres = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "GET";
            request.Host = "client.uagtrade.com";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
            request.CookieContainer = cookiejar;
            request.AllowAutoRedirect = false;
            request.Referer = "https://client.uagtrade.com/member/";
            request.Headers["Origin"] = "https://client.uagtrade.com";
            //request.Headers["Connection"] = "keep-alive";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                htmlres = reader.ReadToEnd();
            }
            return htmlres;
        }
        private static string HttpPostRequest(string URL, string DATA)
        {
            string redirectURL = "";
            byte[] postbytes = Encoding.UTF8.GetBytes(DATA);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.ContentLength = postbytes.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.Host = "client.uagtrade.com";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
            request.CookieContainer = cookiejar;
            request.AllowAutoRedirect = false;
            request.Referer = "https://client.uagtrade.com/member/";
            request.Headers["Origin"] = "https://client.uagtrade.com";
            //request.Headers["Connection"] = "keep-alive";

            using (Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(postbytes, 0, postbytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                redirectURL = response.Headers["Location"];
            }

            return redirectURL;
        }
    }
}
