using System.Net;
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
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        httpClient = new HttpClient(httpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www3.hkexnews.hk/");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://www3.hkexnews.hk");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://www3.hkexnews.hk/sdw/search/searchsdw.aspx");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux)");
    }
    public async Task<List<ShareholdingItem>> GetSearchSDWAsync(String stockCode, DateTime? shareholdingDate = null)
    {
        List<ShareholdingItem> result = [];
        if (shareholdingDate == null)
        {
            shareholdingDate = DateTime.Today;
        }

        Console.WriteLine("Load Page (SearchSDW)");
        // Get Page
        var searchPage = await GetPageAsync("/sdw/search/searchsdw.aspx");

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
            //{ "__EVENTVALIDATION", eventValidation },
            { "__VIEWSTATEGENERATOR", viewStateGenerator },
            { "today", "20260628" },
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
        var resultPage = "";
        using (var response = await httpClient.PostAsync("/sdw/search/searchsdw.aspx", content))
        {
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(responseBody);
            resultPage = responseBody;
        }

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



public class ShareholdingItem
{
    public String ID { get; set; } = String.Empty;
    public String Name { get; set; } = String.Empty;
    public String Address { get; set; } = String.Empty;
    public String Shareholding { get; set; } = String.Empty;
    public String Percentage { get; set; } = String.Empty;
}