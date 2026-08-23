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
            //UseProxy = true,
            //Proxy = new WebProxy("127.0.0.1", 8888),
            //ServerCertificateCustomValidationCallback = (m, c, cc, p) => true,
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls13,
            ClientCertificateOptions = ClientCertificateOption.Manual
        };

        httpClient = new HttpClient(httpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www3.hkexnews.hk/");
        //httpClient.DefaultRequestHeaders.Add("origin", "https://www3.hkexnews.hk");
        //httpClient.DefaultRequestHeaders.Add("referer", "https://www3.hkexnews.hk/sdw/search/searchsdw.aspx");
        //httpClient.DefaultRequestHeaders.Add("connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("connection", "close");
        //httpClient.DefaultRequestHeaders.Add("cache-control", "max-age=0");
        httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"149\", \"Chromium\";v=\"149\", \"Not)A;Brand\";v=\"24\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        //httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Chrome OS\"");
        httpClient.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
        //httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        //httpClient.DefaultRequestHeaders.Add("accept-encoding", "*,compress;q=0");
        //httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Looker Browser\"");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
        httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br, zstd");
        httpClient.DefaultRequestHeaders.Add("accept-language", "en,ja;q=0.9,en-US;q=0.8,zh-TW;q=0.7,zh-CN;q=0.6,zh;q=0.5");
        //httpClient.DefaultRequestHeaders.Add("pragma", "no-cache");
        //httpClient.DefaultRequestHeaders.Add("priority", "u=0, i");
        cookieContainer.Add(new System.Net.Cookie("OptanonAlertBoxClosed", "2026-06-29T18:11:57.522Z", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("OptanonConsent", "isGpcEnabled=0&datestamp=Sun+Jun+29+2026+02%3A20%3A58+GMT%2B0800+(Hong+Kong+Standard+Time)&version=202303.2.0&browserGpcFlag=0&isIABGlobal=false&hosts=&landingPath=https%3A%2F%2Fwww3.hkexnews.hk%2Fsdw%2Fsearch%2Fsearchsdw.aspx&groups=C0001%3A1%2CC0003%3A0%2CC0004%3A0%2CC0002%3A0&AwaitingReconsent=false", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("s_cc", "true", "/", ".hkexnews.hk"));
        //cookieContainer.Add(new Cookie("s_fid", "522FCF490A797461-364606907A0C49F8", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("sclang", "zh-HK", "/", ".hkexnews.hk"));
        //cookieContainer.Add(new Cookie("bm_s", "YAAQDO8hF243CKyfAQAAPm6TuAWBoyOGa7eEmtv62555VvmjPpd+gNqUkpyGzW8w1Oqy1tSUNN4LvOWjDL1EXBbhikaEzlMzmLpE+vEceK5BW5AGSEyUxi6BN8v566/qT5k4Nm1yGh/74PAjo3M7FC15GIsTWfZcZB2FOlSU8TquHj8FbTBrZPnPkSNSnhxFa4S0/CnvHEBCqWeTSKoH1U++59K8g/4py+XdR7GaXv75/MbTLMwALWLqZwoszrwKccLDP0YBL0CffZY5sHgbIm8omaP9vT6775w8KuOxe9HHFa14FCiuoIikK/YokNpOv8yiUDOwWh8uIbKdIOjOCBbTAank7j3a5jfkCufZ/2JJq2lGdPabDwpQuu9+KWHNV7Ktex2r/wSs1RiZQFYA9eNUfK6MIbRRz/Iys+dOMCJSSQOoYTu8RcUUBX02WaAN1kAuWYUFepP7qZNhJZQGfl7oY9pEbaYpxi7hgALvS5TkklGm9DXrLi26l2BJIkHQzJrEYOvgDecl88tloq7g/PoEapSdy2uIkPjIOlSnfrWPuM7UCmJuIKwKVYM4noTcT8YAij3MhZfvG1x83frNpHjISnzEWWJ6ghNlA98nstYbvUDqJyXVeTyXdZ9hxEQYs4VtKkrqk98riRML8tRip+vz6H2KvKf6LZNM2En0mByWsdB8eIL3gareHXOJ1Q8Bd4K8rVUKygk6c/UZWjXKn9gfTjEotnjMYKtFJjS/DSE8Lx4pGmLmMEmEUwZbUQKgfbJ1sRbG3UiMqEa/hBNOQjjIiv0vJyrcLoK1ZxpS9/upkHBqZP1iyL6YJXrSPX2FU63ERs4Htofiv3iHP2V11DY/hC9pqAZf6wAVeyNk/jk5TpA771XvmE0YgnST/K89kU0gcBTPk7YD9X9A5p7pB7DNKfY2cojw05RMhFOhRiqEb1Av4xPv7mI/ZtdnXkjO2mpzWVolcSnbzFc2Lpja7SlIHZvg9AVl173n8bTQo2hxkIRPy7CJQtyNMjb9q3b8k9Me7khxdwcxE1lK0Jn8RosPFsKbrYXaDKFY96rLUrw7onnhfLaZcXRBXqLShEVZIsZ7ZqToHWdT1Q8Lt3Nu7Tb7", "/", ".hkexnews.hk"));
        //cookieContainer.Add(new Cookie("bm_so",  "AABE5CD415ECB70394520168D8D1837F5E78A1C38E944A085671C956DD54C7A1~YAAQDO8hF1U3CKyfAQAAxmmTuAiPD2ow0NX/4r3ZuIH1y0uRcjkuINbsMTn+OwFmzjgl5Pre6k1EFHxtl2ybwD1UiIjnRE0404fuRXJZYCviHnviCMDpUPbtPWkzY5EyuXBsqUJKceEJrkcHCfZLBIRr1iMjXoXJx92keDTpIxu/9+0VM/q3j0CxV+Amg6vkAOnshqUOGQ4GdORoUT4FqsaU08mqAu/sUnJ3estU7hCaH6nIcpKc/AoeWonKiFXH25OY7AIEoPQb4NYjpIVWClYX5ETilZHH4iwNq8HVHr29u1PbtjIVM/UKgVaZusfIa+uOSMXhqHwyOIwO00wPaXqvaZ2nl3nfw2d2N9CsTcsH3F+8GVr+i/SKFIFKKcWzJ6FDGXWEY1p8kFJSO9c9dsgi+f2e0QVJ6quyjSNwBXxCksEsSWOutVJeKAcemRdWb4xlWcG6w7Hf33xsY8AL1A8WCx0=", "/", ".hkexnews.hk"));
        //cookieContainer.Add(new Cookie("bm_lso", "AABE5CD415ECB70394520168D8D1837F5E78A1C38E944A085671C956DD54C7A1~YAAQDO8hF1U3CKyfAQAAxmmTuAiPD2ow0NX/4r3ZuIH1y0uRcjkuINbsMTn+OwFmzjgl5Pre6k1EFHxtl2ybwD1UiIjnRE0404fuRXJZYCviHnviCMDpUPbtPWkzY5EyuXBsqUJKceEJrkcHCfZLBIRr1iMjXoXJx92keDTpIxu/9+0VM/q3j0CxV+Amg6vkAOnshqUOGQ4GdORoUT4FqsaU08mqAu/sUnJ3estU7hCaH6nIcpKc/AoeWonKiFXH25OY7AIEoPQb4NYjpIVWClYX5ETilZHH4iwNq8HVHr29u1PbtjIVM/UKgVaZusfIa+uOSMXhqHwyOIwO00wPaXqvaZ2nl3nfw2d2N9CsTcsH3F+8GVr+i/SKFIFKKcWzJ6FDGXWEY1p8kFJSO9c9dsgi+f2e0QVJ6quyjSNwBXxCksEsSWOutVJeKAcemRdWb4xlWcG6w7Hf33xsY8AL1A8WCx0=~1785508095918", "/", ".hkexnews.hk"));
        //cookieContainer.Add(new Cookie("AMCV_DD0356406298B0640A495CB8%40AdobeOrg", "179643557%7CMCIDTS%7C20666%7CMCMID%7C38953796920595910233361173386624516549%7CvVersion%7C5.5.0", "/", ".hkexnews.hk"));
    }

    private ASPNetPage lastPage = null!;

    private async Task<ASPNetPage> LoadPageAsync()
    {
        // Get Page
        //Console.WriteLine(DateTime.Now);
        var searchPage = await GetPageAsync("/sdw/search/searchsdw.aspx");
        // var b = new Regex("src=\"([^\"]*)\" defer></script>");
        // var mSrc = b.Match(searchPage);
        // if (mSrc.Success)
        // {
        //     var mUrl = mSrc.Groups[1].Value;
        //     Console.WriteLine(DateTime.Now);
        //     var mmPage = await GetPageAsync(mUrl);
        // }
        //Console.WriteLine(DateTime.Now);

        // var cookie = cookieContainer.GetCookies(new Uri("https://www3.hkexnews.hk"));
        // var soCookie = cookie.Where(c => c.Name == "bm_so").FirstOrDefault();
        // if (soCookie != null)
        // {
        //     var lsoValue = $"{soCookie.Value}~{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
        //     cookieContainer.Add(new System.Net.Cookie("bm_lso", lsoValue, "/", ".hkexnews.hk"));
        // }

        // var searchPage2 = await GetPageAsync("/sdw/search/searchsdw.aspx");
        // Console.WriteLine(DateTime.Now);
        ASPNetPage aspNetPage = searchPage;

        return aspNetPage;
    }

    public async Task<StockListResultDto> GetStockListAsync(DateTime? shareholdingDate = null)
    {
        StockListResultDto result = new();

        if (shareholdingDate == null)
        {
            shareholdingDate = DateTime.Today;
        }

        if (lastPage == null)
        {
            lastPage = await LoadPageAsync();
        }
        var aspNetPage = lastPage;
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

        var stockListPage = await GetPageAsync($"/sdw/search/ccass_stock_list.htm?sortby=stockcode&shareholdingdate={shareholdingDate:yyyyMMdd}");

        var stockApiUrl = $"/sdw/search/stocklist.aspx?sortby=stockname&shareholdingdate={shareholdingDate:yyyyMMdd}";
        var stockResult = await GetPageAsync(stockApiUrl);
        var stockList = System.Text.Json.JsonSerializer.Deserialize<List<StockListItemDto>>(stockResult)!;

        stockList = stockList.OrderBy(s => s.StockCode).ToList();
        result.StockList = stockList;
        result.RecordDate = shareholdingDate.Value;
        return result;


    }

    public async Task<SearchSDWResultDto> GetSearchSDWAsync(String stockCode, DateTime? shareholdingDate = null)
    {
        SearchSDWResultDto result = new();

        if (shareholdingDate == null)
        {
            shareholdingDate = DateTime.Today;
        }

        Console.WriteLine("Load Page (SearchSDW)");

        // Get Page
        if (lastPage == null)
        {
            lastPage = await LoadPageAsync();
        }
        var aspNetPage = lastPage;

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

        result.RecordDate = shareholdingDate.Value;

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

        // Post Form Data
        var resultPage = await PostPageAsync("/sdw/search/searchsdw.aspx", content);

        ASPNetPage rPage = resultPage;
        this.lastPage = rPage;


        // Process Response

        Regex rxSummaryTable = new Regex(@"<div class=""ccass-search-summary-table(.*?)<hr />", RegexOptions.Singleline);
        var mSummaryTable = rxSummaryTable.Match(resultPage);
        if (mSummaryTable.Success)
        {

            // <div class="ccass-search-datarow">
            // <div class="summary-category">Market Intermediaries</div>
            // <div class="shareholding">
            // <div class="header">Shareholding in CCASS </div>
            // <div class="value">1,546,313,565</div>
            // </div>
            // <div class="number-of-participants">
            // <div class="header">Number of Participants</div>
            // <div class="value">170</div>
            // </div>
            // <div class="percent-of-participants">
            // <div class="header">% of the total number of Issued Shares/ Warrants/ Units</div>
            // <div class="value">83.70%</div>
            // </div>
            // </div>
            Regex rxRow = new Regex(@"<div class=""ccass-search-datarow"">(?:.*?)<div class=""summary-category"">([^>]*)</div>(?:.*?)<div class=""value"">([^>]*)</div>(?:.*?)<div class=""value"">([^>]*)</div>(?:.*?)<div class=""value"">([^>]*)</div>", RegexOptions.Singleline);

            var mRowCollection = rxRow.Matches(mSummaryTable.Groups[0].Value);
            foreach (var mRow in mRowCollection.OfType<Match>())
            {
                var category = mRow.Groups[1].Value;
                var holding = mRow.Groups[2].Value;
                var participants = mRow.Groups[3].Value;
                var percentage = mRow.Groups[4].Value;

                result.shareholdingSummaryList.Add(new ShareholdingSummaryItemDto()
                {
                    Category = category,
                    Shareholding = holding,
                    Participants = participants,
                    Percentage = percentage
                });

            }

            //<div class="summary-value">1,847,269,704</div>
            Regex rxSummaryValue = new Regex(@"<div class=""summary-value"">([^<]*)</div>");
            var mSummaryValue = rxSummaryValue.Match(mSummaryTable.Groups[1].Value);
            if (mSummaryValue.Success)
            {
                var totalShareholding = mSummaryValue.Groups[1].Value;
                result.TotalShareholding = totalShareholding;
            }

        }


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
                        result.ShareholdingList.Add(new ShareholdingItem()
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
