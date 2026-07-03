using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HKEXReader.Extensions;

namespace HKEXReader.ExternalClient;

public class HKEXCCASSReader : IDisposable
{
    private HttpClient httpClient = null!;
    private CookieContainer cookieContainer = null!;
    private HttpMessageHandler httpMessageHandler = null!;
    private bool disposedValue;

    public HKEXCCASSReader()
    {
        this.InitializeClient();
    }

    private void InitializeClient()
    {
        cookieContainer = new CookieContainer();

        httpMessageHandler = new HttpClientHandler
        {
            //UseProxy = false,
            //Proxy = new WebProxy("127.0.0.1", 8888),
            //ServerCertificateCustomValidationCallback = (m, c, cc, p) => true,
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ClientCertificateOptions = ClientCertificateOption.Manual
        };

        httpClient = new HttpClient(httpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www3.hkexnews.hk/");
        //httpClient.DefaultRequestHeaders.Add("Origin", "https://www3.hkexnews.hk");
        //httpClient.DefaultRequestHeaders.Add("Referer", "https://www3.hkexnews.hk/sdw/search/searchsdw.aspx");
        httpClient.DefaultRequestHeaders.Add("connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("cache-control", "max-age=0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"149\", \"Chromium\";v=\"149\", \"Not)A;Brand\";v=\"24\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        //httpClient.DefaultRequestHeaders.Add("accept-encoding", "*,compress;q=0");
        //httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Looker Browser\"");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br, zstd");
        httpClient.DefaultRequestHeaders.Add("accept-language", "en,ja;q=0.9,en-US;q=0.8,zh-TW;q=0.7,zh-CN;q=0.6,zh;q=0.5");
        //httpClient.DefaultRequestHeaders.Add("pragma", "no-cache");
        //httpClient.DefaultRequestHeaders.Add("priority", "u=0, i");


        cookieContainer.Add(new System.Net.Cookie("OptanonConsent", "isGpcEnabled=0&datestamp=Sun+Jun+29+2026+02%3A20%3A58+GMT%2B0800+(Hong+Kong+Standard+Time)&version=202303.2.0&browserGpcFlag=0&isIABGlobal=false&hosts=&landingPath=https%3A%2F%2Fwww3.hkexnews.hk%2Fsdw%2Fsearch%2Fsearchsdw.aspx&groups=C0001%3A1%2CC0003%3A0%2CC0004%3A0%2CC0002%3A0&AwaitingReconsent=false", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("s_cc", "true", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("sclang", "zh-HK", "/", ".hkexnews.hk"));



    }

    public async Task<List<ShareholdingItem>> GetSearchSDWAsync(String stockCode, DateTime? shareholdingDate = null)
    {
        Stopwatch sw = new();
        sw.Start();

        List<ShareholdingItem> result = [];
        if (shareholdingDate == null)
        {
            shareholdingDate = DateTime.Today;
        }

        Console.WriteLine("Load Page (SearchSDW)");

        // Get Page
        var searchPage = await GetPageAsync("/sdw/search/searchsdw.aspx");
        Console.WriteLine("Loaded Page");
        Console.WriteLine($"Elapsed: {sw.Elapsed.ToString()}");

        Random random = new Random();
        await Task.Delay(random.Next(2_000, 4_000));


        ASPNetPage aspNetPage = searchPage;
        var maxDate = aspNetPage.GetMaxDate() ?? DateTime.Today;
        var minDate = aspNetPage.GetMinDate() ?? DateTime.Today.AddDays(-365);

        if (shareholdingDate > maxDate)
        {
            shareholdingDate = maxDate;
        }

        if (shareholdingDate < minDate)
        {
            shareholdingDate = minDate;
        }

        // Get ViewState
        var viewState = aspNetPage.ViewState;
        // Get EventValidation
        var eventValidation = aspNetPage.EventValidation;
        // Get __VIEWSTATEGENERATOR
        var viewStateGenerator = aspNetPage.ViewStateGenerator;

        // Prepare Form Data
        var formData = new Dictionary<string, string>
        {
            { "__EVENTTARGET", "btnSearch" },
            { "__EVENTARGUMENT", "" },
            { "__VIEWSTATE", viewState },
            { "__EVENTVALIDATION", eventValidation },
            { "__VIEWSTATEGENERATOR", viewStateGenerator },
            { "today", DateTime.Now.ToString("yyyyMMdd") },
            { "sortBy", "shareholding" },
            { "sortDirection", "desc" },
            { "originalShareholdingDate",shareholdingDate?.ToString("yyyy/MM/dd")?? ""},
            { "alertMsg", "" },
            { "txtShareholdingDate",shareholdingDate?.ToString("yyyy/MM/dd")??""},
            { "txtStockCode", stockCode },
            { "txtStockName", ""},
            { "txtParticipantID", "" },
            { "txtParticipantName", "" },
            { "txtSelPartID", "" },
        };


        var content = new FormUrlEncodedContent(formData);

        Console.WriteLine($"Submit Search {stockCode}");
        // Post Form Data
        var resultPage = await PostPageAsync("/sdw/search/searchsdw.aspx", content);

        Console.WriteLine("Submit Response");
        Console.WriteLine($"Elapsed: {sw.Elapsed.ToString()}");

        Console.WriteLine("Process Response");
        // Process Response
        Regex rxTable = new Regex(@"<table class=""table(?:[^>]*)>(.*?)</table>", RegexOptions.Singleline);
        var mTable = rxTable.Match(resultPage);
        if (mTable.Success)
        {
            var tableContent = mTable.Groups[1].Value;

            Regex rxBody = new Regex(@"<tbody>(.*?)</tbody>", RegexOptions.Singleline);
            var mBody = rxBody.Match(tableContent);
            if (mBody.Success)
            {
                var tableBody = mBody.Groups[1].Value;

                Regex rxRow = new Regex(@"<tr>(.*?)</tr>", RegexOptions.Singleline);
                Regex rxData = new Regex(@"div class=""mobile-list-body"">([^<]*)</div>");
                var mRowCollection = rxRow.Matches(tableBody);
                foreach (var mRow in mRowCollection.OfType<Match>())
                {
                    var row = mRow.Groups[1].Value;
                    var mData = rxData.Matches(row);
                    if (mData.Count == 5)
                    {
                        result.Add(new ShareholdingItem()
                        {
                            ID = mData[0].Groups[1].Value,
                            Name = mData[1].Groups[1].Value,
                            Address = mData[2].Groups[1].Value,
                            Shareholding = mData[3].Groups[1].Value,
                            Percentage = mData[4].Groups[1].Value,
                        });
                    }
                    else
                    {
                        Console.WriteLine("Data Exception");
                    }

                }
            }
        }
        Console.WriteLine("Finish Process");
        Console.WriteLine($"Elapsed: {sw.Elapsed.ToString()}");
        return result;
    }

    private async Task<String> GetPageAsync(String url)
    {
        using (var response = await httpClient.GetAsync(url))
        {
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
    }
    private async Task<String> PostPageAsync(String url, HttpContent content)
    {
        var cookie = cookieContainer.GetCookies(new Uri("https://www3.hkexnews.hk"));
        var soCookie = cookie.Where(c => c.Name == "bm_so").FirstOrDefault();
        if (soCookie != null)
        {
            var lsoValue = $"{soCookie.Value}~{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            cookieContainer.Add(new System.Net.Cookie("bm_lso", lsoValue, "/", ".hkexnews.hk"));
        }
        using (var response = await httpClient.PostAsync(url, content))
        {
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (httpClient != null)
                {
                    httpClient.Dispose();
                }
            }

            disposedValue = true;
        }
    }


    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
