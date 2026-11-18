using System.Net;
using System.Text.RegularExpressions;
using HKEXReader.Extensions;

namespace HKEXReader.ExternalClient;

/// <summary>
/// HTTP client for the HKEX CCASS "Search SDW" pages.
/// Mimics a browser session (cookies + headers), posts ASP.NET WebForms
/// ViewState fields, and scrapes shareholding HTML into DTOs.
/// </summary>
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

    /// <summary>
    /// Configure HttpClient to look like a Chrome-on-ChromeOS browser talking to hkexnews.hk.
    /// Cookie consent / analytics cookies are seeded so the site does not redirect to a banner.
    /// </summary>
    private void InitializeClient()
    {
        cookieContainer = new CookieContainer();

        httpMessageHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls13,
            ClientCertificateOptions = ClientCertificateOption.Manual
        };

        httpClient = new HttpClient(httpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www3.hkexnews.hk/");

        // Browser-like request headers expected by the HKEX news site.
        httpClient.DefaultRequestHeaders.Add("connection", "close");
        httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"149\", \"Chromium\";v=\"149\", \"Not)A;Brand\";v=\"24\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Chrome OS\"");
        httpClient.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "navigate");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-user", "?1");
        httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "document");
        httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate, br, zstd");
        httpClient.DefaultRequestHeaders.Add("accept-language", "en,ja;q=0.9,en-US;q=0.8,zh-TW;q=0.7,zh-CN;q=0.6,zh;q=0.5");

        // Pre-seed consent / analytics cookies so searches are not blocked by the banner.
        cookieContainer.Add(new System.Net.Cookie("OptanonAlertBoxClosed", "2026-06-29T18:11:57.522Z", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("OptanonConsent", "isGpcEnabled=0&datestamp=Sun+Jun+29+2026+02%3A20%3A58+GMT%2B0800+(Hong+Kong+Standard+Time)&version=202303.2.0&browserGpcFlag=0&isIABGlobal=false&hosts=&landingPath=https%3A%2F%2Fwww3.hkexnews.hk%2Fsdw%2Fsearch%2Fsearchsdw.aspx&groups=C0001%3A1%2CC0003%3A0%2CC0004%3A0%2CC0002%3A0&AwaitingReconsent=false", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("s_cc", "true", "/", ".hkexnews.hk"));
        cookieContainer.Add(new System.Net.Cookie("sclang", "zh-HK", "/", ".hkexnews.hk"));
    }

    /// <summary>
    /// Cached last ASP.NET page. Reused so subsequent POSTs can send a valid ViewState
    /// without reloading the empty search form every time.
    /// </summary>
    private ASPNetPage lastPage = null!;

    /// <summary>GET the empty Search SDW form and parse its ASP.NET hidden fields.</summary>
    private async Task<ASPNetPage> LoadPageAsync()
    {
        var searchPage = await GetPageAsync("/sdw/search/searchsdw.aspx");
        ASPNetPage aspNetPage = searchPage;

        return aspNetPage;
    }

    /// <summary>
    /// Fetch the CCASS stock list JSON for a shareholding date (clamped to the site's min/max).
    /// </summary>
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

        // Clamp requested date to the window published on the Search SDW page.
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

        // HTML listing page (not parsed further; warms the session / date context).
        var stockListPage = await GetPageAsync($"/sdw/search/ccass_stock_list.htm?sortby=stockcode&shareholdingdate={shareholdingDate:yyyyMMdd}");

        // JSON endpoint returns [{ "c": code, "n": name }, ...].
        var stockApiUrl = $"/sdw/search/stocklist.aspx?sortby=stockname&shareholdingdate={shareholdingDate:yyyyMMdd}";
        var stockResult = await GetPageAsync(stockApiUrl);
        var stockList = System.Text.Json.JsonSerializer.Deserialize<List<StockListItemDto>>(stockResult)!;

        stockList = stockList.OrderBy(s => s.StockCode).ToList();
        result.StockList = stockList;
        result.RecordDate = shareholdingDate.Value;
        return result;


    }

    /// <summary>
    /// Search shareholding disclosure for one stock on a given date.
    /// Posts the ASP.NET form, then regex-scrapes the summary and participant tables.
    /// </summary>
    public async Task<SearchSDWResultDto> GetSearchSDWAsync(String stockCode, DateTime? shareholdingDate = null)
    {
        SearchSDWResultDto result = new();

        if (shareholdingDate == null)
        {
            shareholdingDate = DateTime.Today;
        }

        Console.WriteLine("Load Page (SearchSDW)");

        // Ensure we have a valid ViewState from a prior GET (or a previous search POST).
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

        // ASP.NET WebForms anti-forgery / state fields required on every postback.
        var viewState = aspNetPage.ViewState;
        var eventValidation = aspNetPage.EventValidation;
        var viewStateGenerator = aspNetPage.ViewStateGenerator;

        // Form fields mirror a browser click of the Search button on searchsdw.aspx.
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

        var resultPage = await PostPageAsync("/sdw/search/searchsdw.aspx", content);

        // Keep the result page's ViewState for the next search POST.
        ASPNetPage rPage = resultPage;
        this.lastPage = rPage;


        // --- Parse summary categories (Market Intermediaries, NCIP, etc.) ---
        Regex rxSummaryTable = new Regex(@"<div class=""ccass-search-summary-table(.*?)<hr />", RegexOptions.Singleline);
        var mSummaryTable = rxSummaryTable.Match(resultPage);
        if (mSummaryTable.Success)
        {
            // Each datarow: category, shareholding, participant count, % of issued shares.
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

            // Total issued shares shown in the summary footer.
            Regex rxSummaryValue = new Regex(@"<div class=""summary-value"">([^<]*)</div>");
            var mSummaryValue = rxSummaryValue.Match(mSummaryTable.Groups[1].Value);
            if (mSummaryValue.Success)
            {
                var totalShareholding = mSummaryValue.Groups[1].Value;
                result.TotalShareholding = totalShareholding;
            }

        }


        // --- Parse participant detail table (ID, Name, Address, Holding, %) ---
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
                // Mobile layout cells hold the values we need (5 per data row).
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

    /// <summary>
    /// POST form data. Before posting, refresh the Akamai <c>bm_lso</c> cookie from
    /// the current <c>bm_so</c> value (timestamp suffix) when present.
    /// </summary>
    private async Task<String> PostPageAsync(String url, HttpContent content)
    {
        var cookie = cookieContainer.GetCookies(new Uri("https://www3.hkexnews.hk"));
        var soCookie = cookie.Where(c => c.Name == "bm_so").FirstOrDefault();
        if (soCookie != null)
        {
            // bm_lso = bm_so value + "~" + current unix ms — expected by bot-management.
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
