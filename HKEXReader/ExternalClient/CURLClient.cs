using System.Diagnostics;
using System.Net;
using System.Text;

public class CURLClient : IDisposable
{
    private HttpMessageHandler httpMessageHandler;
    private bool disposedValue;

    public Uri BaseAddress { get; set; } = null!;
    public Dictionary<String, String> DefaultRequestHeaders { get; set; } = [];

    public CURLClient()
    {
        this.httpMessageHandler = null!;
    }
    public CURLClient(HttpMessageHandler httpMessageHandler)
    {
        this.httpMessageHandler = httpMessageHandler;
    }

    internal async Task<CURLResponse> GetAsync(string url)
    {
        //         /*
        // curl 'https://www3.hkexnews.hk/sdw/search/searchsdw.aspx' \
        //   -H 'accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' \
        //   -H 'accept-language: en,ja;q=0.9,en-US;q=0.8,zh-TW;q=0.7,zh-CN;q=0.6,zh;q=0.5' \
        //   -H 'cache-control: no-cache' \
        //   -b 'OptanonAlertBoxClosed=2026-06-28T18:11:57.522Z; AMCV_DD0356406298B0640A495CB8%40AdobeOrg=179643557%7CMCIDTS%7C20665%7CMCMID%7C90671310146753729672891447846900750018%7CvVersion%7C5.5.0; s_cc=true; bm_so=9E3895F4C9E094F4F8AD493F6BB1978D0E35F95FC30F3F3D232F85A968FEE6E3~YAAQKu8hF8HkuqSfAQAAiNcQuAhJriNl15GEv3suii+9X8aoSYkt9zwi0alWsGOBSJKwk6sRcUhh6l6LDczOlQOOMSVT+1RiZU7re3wdoUtgOFYIGY/PSfLDf9CTkQSSsG9Qe22X6R7ubCxugPP5IQaAdEfOeTKwn+MZCY6/Tc8TXmBFs2Eh91sbebIdpsA6wcEYZYoExLElwpYU24wvBLM2bhMhOJatgcTY/I+EMOtyfYdGERDURtXWkgKiqTSz5CyjKHSuITISA0UGv3ikFuFZjb/JNgJHVEaI9JrRwMTjtJmaqfXNJJDBS8v8I2MJ88iMze53xDOgq3C5HCtV4CvWFbXGVqacggtUVvo6vws35hQRUEi5xl1lohlKpCgaJDaoRzSEEeR73Sim5SYqM8pQODjn3JvthGeIOY+iNhR5TbRkeCaDWn5Rxz3CoD28o4ss4qyFteYzPLty8xjLKXWo0FQ=; OptanonConsent=isGpcEnabled=0&datestamp=Fri+Jul+31+2026+20%3A05%3A39+GMT%2B0800+(Hong+Kong+Standard+Time)&version=202303.2.0&browserGpcFlag=0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0001%3A1%2CC0003%3A1%2CC0004%3A1%2CC0002%3A1&geolocation=HK%3B&AwaitingReconsent=false; bm_lso=9E3895F4C9E094F4F8AD493F6BB1978D0E35F95FC30F3F3D232F85A968FEE6E3~YAAQKu8hF8HkuqSfAQAAiNcQuAhJriNl15GEv3suii+9X8aoSYkt9zwi0alWsGOBSJKwk6sRcUhh6l6LDczOlQOOMSVT+1RiZU7re3wdoUtgOFYIGY/PSfLDf9CTkQSSsG9Qe22X6R7ubCxugPP5IQaAdEfOeTKwn+MZCY6/Tc8TXmBFs2Eh91sbebIdpsA6wcEYZYoExLElwpYU24wvBLM2bhMhOJatgcTY/I+EMOtyfYdGERDURtXWkgKiqTSz5CyjKHSuITISA0UGv3ikFuFZjb/JNgJHVEaI9JrRwMTjtJmaqfXNJJDBS8v8I2MJ88iMze53xDOgq3C5HCtV4CvWFbXGVqacggtUVvo6vws35hQRUEi5xl1lohlKpCgaJDaoRzSEEeR73Sim5SYqM8pQODjn3JvthGeIOY+iNhR5TbRkeCaDWn5Rxz3CoD28o4ss4qyFteYzPLty8xjLKXWo0FQ=~1785499539780; bm_s=YAAQKu8hFz7luqSfAQAAYeQQuAXqavyXRH3XqGE/b4jYrPCRfUKnora8R8iRak4VVoZTa1FzELPZndFnjQx0A4A/fvjUOOy9wX3v+JLNj8TRzWYwaTn3BUqhSNzK5fl92I7wJhiYi8GTn27It1bIubjMOYadVxWEwC5vslMCl0lX4Sl2gNpOswiiSWAxCXAB8Gqt8f6PCMZc/JAEYQ302kRUbgWDnKW6hlpfcGPo/ZVdc/BS9FXmq4Vmwrg9d/Yvx2w7LSpiRGEi9ZLhZllmHGKcoL6o92jKYD3jA8nZl/1FBXCyOgSDgrxm8inXNmO2D+ZDD9jTrO2INmuS4krtECwDB7a9MBdvoPNkvwrPyCukQFbxBWKpUfUHByvSicNBNfjcLwKq+Vk+RIEHOOqAzxyOFSCeaPrnK7hMkcelf3U89b8rRdah7BzKKh6PuWysnrCRdUEgllJcxCqUFAaB37+j6GGfALDGgv2Ihbtm8tsrBWbm/kk4wP1e4ZuHJVoppKHsSmYBFnp+ZjY1xUAU6EYOtl7HCzizzDdWZt63VLbzncJaSwQ8YwwbL6yMJp7piDZzS9x2Rc7onmY4W8awvEwTihx4yE60N8oIdBBbGKRx8PWHwWn8g6rxQ5xhag5d2KpmT/9ipPkftLjfi7IFFVKD2hyx66Kr08sB4TisIi7QhTYNXyWjgeENBgmg478tKHFGYw6IOSZp9KRN1jSxM7YRYg60qCgs/rW8Ox87Mq2+T9OMk9hHAwNwGYoGpl0auOKLUIvjyGyS8mW0GaC9t/6ecVRtnCc/2v9wIYaldliFdiytDAmjeTmCP28DkxEMMubqv4adQZ5Qvcg+AqaWsrtH4sJ7qtRzLUPmTEHzwDl/uo/FJ0fyBsU5FiXFJNNCUG06YXC5542lp6KD5oNQ2XTzsy6PsA/gtlhyqhphq2I0EZ6B+IBsV6T1dZemwfOO7biMUGXW8sRBvEfUshDWUciW/zWH/M/J2+MJ9RFwmGrtfdLxG+Fkz7k2tiLvrRlXqQoi1bD25lmns/3fVPzauLGitKSf13cj19jZXRGOGn6p2Lhxc6kIb4R03Pw+rPC5B/Cm3aU9ZWEcFGF93Iff9Nyk' \
        //   -H 'pragma: no-cache' \
        //   -H 'priority: u=0, i' \
        //   -H 'referer: https://www.google.com/' \
        //   -H 'sec-ch-ua: "Not;A=Brand";v="8", "Chromium";v="150", "Google Chrome";v="150"' \
        //   -H 'sec-ch-ua-mobile: ?0' \
        //   -H 'sec-ch-ua-platform: "Chrome OS"' \
        //   -H 'sec-fetch-dest: document' \
        //   -H 'sec-fetch-mode: navigate' \
        //   -H 'sec-fetch-site: cross-site' \
        //   -H 'sec-fetch-user: ?1' \
        //   -H 'upgrade-insecure-requests: 1' \
        //   -H 'user-agent: Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36'
        //         */

        //StringBuilder curlCommand = new StringBuilder();
        List<String> args = [];
        var targetUrl = new Uri(BaseAddress, url);
        //curlCommand.Append($" '{targetUrl}' ");
        args.Add(targetUrl.ToString());
        //curlCommand.Append($"  -i \\\n");
        foreach (var header in DefaultRequestHeaders.OrderBy(h => h.Key).ToArray())
        {
            //curlCommand.Append($"  -H '{header.Key}: {header.Value}' ");
            args.Add("-H");
            args.Add($"{header.Key}: {header.Value}");

        }
        CookieContainer cookieContainer = null!;
        if (this.httpMessageHandler != null &&
        this.httpMessageHandler is HttpClientHandler httpClientHandler &&
        httpClientHandler.CookieContainer != null)
        {
            cookieContainer = httpClientHandler.CookieContainer;
            var cookies = cookieContainer.GetCookies(targetUrl);
            if (cookies.Count > 0)
            {
                //Console.WriteLine($"Cookie Count {cookies.Count}");
                StringBuilder cookieHeader = new StringBuilder();
                foreach (System.Net.Cookie cookie in cookies)
                {
                    cookieHeader.Append($"{cookie.Name}={cookie.Value}; ");
                }
                // Remove the trailing "; "
                if (cookieHeader.Length > 2)
                {
                    cookieHeader.Length -= 2;
                }
                //curlCommand.Append($"  -b '{cookieHeader}' ");
                args.Add("-b");
                args.Add(cookieHeader.ToString());
            }
        }

        args.Add("-i");
        args.Add("--no-keepalive");
        args.Add("--http2");
        args.Add("--compressed");

        //Console.WriteLine(curlCommand.ToString());

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "curl",
            //Arguments = curlCommand.ToString(),

            RedirectStandardOutput = true,
            RedirectStandardError = true,

        };

        args.ForEach(a =>
        {
            psi.ArgumentList.Add(a);
        });

        //Console.WriteLine(String.Join("' '", args));

        //Console.WriteLine(curlCommand.ToString());
        var p = System.Diagnostics.Process.Start(psi);

        // Task t = Task.Run(async () =>
        // {
        //     var output = await p!.StandardOutput.ReadToEndAsync();
        //     Console.WriteLine(output);
        // });

        var output = await p!.StandardOutput.ReadToEndAsync();
        await p!.WaitForExitAsync();


        var parts = output.Split("\r\n\r\n", 2);
        //Console.WriteLine(parts.Length);
        var responseHeader = parts[0];
        var body = parts[1];

        var headers = responseHeader.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        //var count = 0;
        foreach (var h in headers)
        {
            //Console.WriteLine($"{count++} - {h}");
            var headerParts = h.Split(": ", 2);
            if (headerParts.Length == 2)
            {
                switch (headerParts[0].ToLower())
                {
                    case "set-cookie":
                        var cookie = headerParts[1];
                        var cookieParts = cookie.Split(";", 2);
                        var cookieValue = cookieParts[0];
                        var cookieValueParts = cookieValue.Split("=", 2);
                        if (cookieValueParts.Length == 2)
                        {
                            cookieContainer.Add(new Cookie(cookieValueParts[0], cookieValueParts[1], "/", ".hkexnews.hk"));
                        }
                        break;
                }
            }
        }

        var response = new CURLResponse()
        {
            Content = new CURLContent(body!)
        };
        //throw new NotImplementedException();
        return response;
    }

    internal async Task<CURLResponse> PostAsync(string url, HttpContent content)
    {
        // curl --url 'https://www3.hkexnews.hk/sdw/search/searchsdw.aspx' \
        //   -H 'accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7' \
        //   -H 'accept-language: en,ja;q=0.9,en-US;q=0.8,zh-TW;q=0.7,zh-CN;q=0.6,zh;q=0.5' \
        //   -H 'cache-control: no-cache' \
        //   -H 'content-type: application/x-www-form-urlencoded' \
        //   -b 'OptanonAlertBoxClosed=2026-06-28T18:11:57.522Z; bm_so=CB273EAA7A4382929BBDB2AF57A8E1457B6755E6586275073DE883A07F3E6A7B~YAAQZlDGy+EzVeqfAQAA5s6NLwjn7QF4/L0Ey+ZwXDxi7WBNFzRVA75yCJf94VQs3P2samMUKVTKw4Z4eLoB5hj1FG76wg43ZrcKShi8SnJvjlP7s+SUomcd78swA+CYs+md4b8VNGWoBZgnYD9LRUth07RyN0ZTG6Uyo6IAljep0mCeaZxRNQL/KBRn/oczoYXU38onCrpIUuD41YA0J7eutLVYKhS59J9td3GiILeATzwfgFuaOackbsjYgOT8bBA0QDZrUQXNHHe8xX6dZPMyXT71WLdkRqTL0daEDavTb7h1CM7G/nhyJACFlnadXQk+iB+0vHqhujug4wOZgTJpbMpqG+EpPvZFuJHyUDPRwPcCb7QEjtTsPnBC+sBGKQnP30wPD2cfGFg/Zoiy6n8FhjMeLAkfLuC8OY9/FqDSQZ7E/ybel6BrLPM+/j2or6apCHkWNhb+1NFm+u1llBBS; bm_lso=CB273EAA7A4382929BBDB2AF57A8E1457B6755E6586275073DE883A07F3E6A7B~YAAQZlDGy+EzVeqfAQAA5s6NLwjn7QF4/L0Ey+ZwXDxi7WBNFzRVA75yCJf94VQs3P2samMUKVTKw4Z4eLoB5hj1FG76wg43ZrcKShi8SnJvjlP7s+SUomcd78swA+CYs+md4b8VNGWoBZgnYD9LRUth07RyN0ZTG6Uyo6IAljep0mCeaZxRNQL/KBRn/oczoYXU38onCrpIUuD41YA0J7eutLVYKhS59J9td3GiILeATzwfgFuaOackbsjYgOT8bBA0QDZrUQXNHHe8xX6dZPMyXT71WLdkRqTL0daEDavTb7h1CM7G/nhyJACFlnadXQk+iB+0vHqhujug4wOZgTJpbMpqG+EpPvZFuJHyUDPRwPcCb7QEjtTsPnBC+sBGKQnP30wPD2cfGFg/Zoiy6n8FhjMeLAkfLuC8OY9/FqDSQZ7E/ybel6BrLPM+/j2or6apCHkWNhb+1NFm+u1llBBS~1787504218065; AMCV_DD0356406298B0640A495CB8%40AdobeOrg=179643557%7CMCIDTS%7C20689%7CMCMID%7C38953796920595910233361173386624516549%7CvVersion%7C5.5.0; s_cc=true; bm_s=YAAQZlDGywo0VeqfAQAAF+KNLwXtRVeFwDaJAxjQqQKi5rEe72uzstWJg+TUCMN9ZW7IL2ZpXhAwRbCoPF2tSN5abZWPG56Oa8pA3vdTQaL1CqK2r/1S8jR3F1oPaZggwjRoIEJWGhl/RXhg3MEEerDlqqZzE24rqtQvyHUI43/IglDcgqwKygBeET7ahajfM+2ddmAt1emrBpqxcs4tSxk2jxGqHP62C6+dxVVVcXR4LQXbjY8JL766FsOpaH772e74XhrJbIE02ooJEo1fT8VdEn/b5CxS2mCRrQQSCO/uXmphXYCO3aK62JimxN8j1ObgmWo9wNsLv6TDIcHwlKpXQM6y7nP2L3FH+DbF04ca4kqXEnNZNZa4q/0Q8JXo6hV6eFqPm4L0Q4dGZf/vu3Jhel/TNp7mwYNWTqdjp9xOJFdeX+wYkDu81YmMiyyGzqaPw2KfLg+LC6z9AstyaA2Ro+XWg8dKHMvR61M9MGpPCoazHWptrZ+qmFn4VB5SYwdeo1JR1Zkz1P84HyBgE9BdR1wZOaDSYi5/NUB/1+39Q2IP6iNEQLW5EqMM8dM8I3kvKjVGlU0HUwGWg2c+L/Lnt+OHXHkG6AdeZt2PczbgFSFdlwVmZ9KGHdKNG2ghfrdlnHcYeWl4UgU89lx4ZcM30ubI9FgiZ7CG8l/5dDQw3XH5BdvYcmA09cWgdthyEdkdBK6wHbC/VWp5AgOHFs6TW6sUy8fcDTDlDhvuh/DQ4ryVIXY394akoicKZGtQgeFol7ojUydrxdrDTdqnRN6L6xRc+CVo8Hdw8p6ZWFCiSfDAIAI7JQflwXRhBukCqcC/YEE/TtiTEfIkKpIDYKplN+wk3QukhVb5H0DyV8NQ/yLyuCH2IqeDL+UcSy1sPXWhkEZws5p57YO87EHrD/i61ZWUOJyvL8rN81ulR/KgF4ADiC34kEF3NXJxPQfoobEK6UwHJf/LKcwti897E/VrSw+Oi2FqFhtXP+xzrtLeEHKHq4x/9TnBkNrTazIpBRx+FWtGZ3QSdu67hPxGKANwoIYeM/L/7jYIvKt1chyRaO+M9Cy0ZWTfRBYdwilUWFwSuPn2dTGgQdqTINpoEe8yzi2f6PE8h+I+Q61ltzBouQIe6HRTXIxo97D9wDS8P+fNWR8boyYz; OptanonConsent=isGpcEnabled=0&datestamp=Mon+Aug+24+2026+00%3A57%3A02+GMT%2B0800+(Hong+Kong+Standard+Time)&version=202303.2.0&browserGpcFlag=0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0001%3A1%2CC0003%3A1%2CC0004%3A1%2CC0002%3A1&geolocation=HK%3B&AwaitingReconsent=false' \
        //   -H 'origin: https://www3.hkexnews.hk' \
        //   -H 'pragma: no-cache' \
        //   -H 'priority: u=0, i' \
        //   -H 'referer: https://www3.hkexnews.hk/sdw/search/searchsdw.aspx' \
        //   -H 'sec-ch-ua: "Not=A?Brand";v="99", "Google Chrome";v="151", "Chromium";v="151"' \
        //   -H 'sec-ch-ua-mobile: ?0' \
        //   -H 'sec-ch-ua-platform: "Chrome OS"' \
        //   -H 'sec-fetch-dest: document' \
        //   -H 'sec-fetch-mode: navigate' \
        //   -H 'sec-fetch-site: same-origin' \
        //   -H 'sec-fetch-user: ?1' \
        //   -H 'upgrade-insecure-requests: 1' \
        //   -H 'user-agent: Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36' \
        //   --data-raw '__EVENTTARGET=btnSearch&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE1Nzg3NjcwNjdkZJW3ITBSje1dkFJLjneiDSfdsLh3&__VIEWSTATEGENERATOR=A7B2BBE2&today=20260824&sortBy=shareholding&sortDirection=desc&originalShareholdingDate=2026%2F08%2F23&alertMsg=&txtShareholdingDate=2026%2F08%2F22&txtStockCode=00005&txtStockName=HSBC+HOLDINGS+PLC&txtParticipantID=&txtParticipantName=&txtSelPartID='        


        List<String> args = [];
        var targetUrl = new Uri(BaseAddress, url);
        //curlCommand.Append($" '{targetUrl}' ");
        args.Add(targetUrl.ToString());
        //curlCommand.Append($"  -i \\\n");
        foreach (var header in DefaultRequestHeaders.OrderBy(h => h.Key).ToArray())
        {
            //curlCommand.Append($"  -H '{header.Key}: {header.Value}' ");
            args.Add("-H");
            args.Add($"{header.Key}: {header.Value}");

        }
        CookieContainer cookieContainer = null!;
        if (this.httpMessageHandler != null &&
        this.httpMessageHandler is HttpClientHandler httpClientHandler &&
        httpClientHandler.CookieContainer != null)
        {
            cookieContainer = httpClientHandler.CookieContainer;
            var cookies = cookieContainer.GetCookies(targetUrl);
            if (cookies.Count > 0)
            {
                //Console.WriteLine($"Cookie Count {cookies.Count}");
                StringBuilder cookieHeader = new StringBuilder();
                foreach (System.Net.Cookie cookie in cookies)
                {
                    cookieHeader.Append($"{cookie.Name}={cookie.Value}; ");
                }
                // Remove the trailing "; "
                if (cookieHeader.Length > 2)
                {
                    cookieHeader.Length -= 2;
                }
                //curlCommand.Append($"  -b '{cookieHeader}' ");
                args.Add("-b");
                args.Add(cookieHeader.ToString());
            }
        }

        args.Add("-i");
        args.Add("--no-keepalive");
        args.Add("--http2");
        args.Add("--compressed");

         var stringContent = await content.ReadAsStringAsync();
        args.Add("--data-raw");
        args.Add(stringContent);
        //Console.WriteLine($"Post Data: {stringContent}");

        //Console.WriteLine(curlCommand.ToString());

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "curl",
            //Arguments = curlCommand.ToString(),

            RedirectStandardOutput = true,
            RedirectStandardError = true,

        };

        args.ForEach(a =>
        {
            psi.ArgumentList.Add(a);
        });

        //Console.WriteLine(String.Join("' '", args));

        //Console.WriteLine(curlCommand.ToString());
        var p = System.Diagnostics.Process.Start(psi);

        // Task t = Task.Run(async () =>
        // {
        //     var output = await p!.StandardOutput.ReadToEndAsync();
        //     Console.WriteLine(output);
        // });

        var output = await p!.StandardOutput.ReadToEndAsync();

        await p!.WaitForExitAsync();


        var parts = output.Split("\r\n\r\n", 2);
        //Console.WriteLine(parts.Length);
        var responseHeader = parts[0];
        var body = parts[1];

        var headers = responseHeader.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        //var count = 0;
        foreach (var h in headers)
        {
            //Console.WriteLine($"{count++} - {h}");
            var headerParts = h.Split(": ", 2);
            if (headerParts.Length == 2)
            {
                switch (headerParts[0].ToLower())
                {
                    case "set-cookie":
                        var cookie = headerParts[1];
                        var cookieParts = cookie.Split(";", 2);
                        var cookieValue = cookieParts[0];
                        var cookieValueParts = cookieValue.Split("=", 2);
                        if (cookieValueParts.Length == 2)
                        {
                            cookieContainer.Add(new Cookie(cookieValueParts[0], cookieValueParts[1], "/", ".hkexnews.hk"));
                        }
                        break;
                }
            }
        }

        //Console.WriteLine($"Response Body: {body}");

        var response = new CURLResponse()
        {
            Content = new CURLContent(body!)
        };
        //throw new NotImplementedException();
        return response;

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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

public class CURLResponse : IDisposable
{
    private bool disposedValue;

    internal void EnsureSuccessStatusCode()
    {
        //throw new NotImplementedException();
    }

    public CURLContent Content { get; set; } = null!;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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

public class CURLContent
{
    private string body;

    public CURLContent(string body)
    {
        this.body = body;
    }

    internal async Task<string> ReadAsStringAsync()
    {
        return this.body;
    }
}