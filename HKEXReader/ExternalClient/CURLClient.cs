using System.Diagnostics;
using System.Net;
using System.Text;

/// <summary>
/// Experimental HttpClient-compatible wrapper that shells out to the system <c>curl</c>
/// binary instead of using .NET sockets. Useful when comparing browser-like TLS/HTTP2
/// behaviour; not currently wired into the main download path.
/// </summary>
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

    /// <summary>
    /// GET <paramref name="url"/> via curl (<c>-i --http2 --compressed</c>).
    /// Parses response headers (including Set-Cookie) and returns the body.
    /// </summary>
    internal async Task<CURLResponse> GetAsync(string url)
    {
        List<String> args = [];
        var targetUrl = new Uri(BaseAddress, url);
        args.Add(targetUrl.ToString());

        // Mirror DefaultRequestHeaders as curl -H pairs.
        foreach (var header in DefaultRequestHeaders.OrderBy(h => h.Key).ToArray())
        {
            args.Add("-H");
            args.Add($"{header.Key}: {header.Value}");

        }

        // If an HttpClientHandler was supplied, forward its CookieContainer as -b.
        CookieContainer cookieContainer = null!;
        if (this.httpMessageHandler != null &&
        this.httpMessageHandler is HttpClientHandler httpClientHandler &&
        httpClientHandler.CookieContainer != null)
        {
            cookieContainer = httpClientHandler.CookieContainer;
            var cookies = cookieContainer.GetCookies(targetUrl);
            if (cookies.Count > 0)
            {
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
                args.Add("-b");
                args.Add(cookieHeader.ToString());
            }
        }

        args.Add("-i");            // include response headers in stdout
        args.Add("--no-keepalive");
        args.Add("--http2");
        args.Add("--compressed");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "curl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,

        };

        args.ForEach(a =>
        {
            psi.ArgumentList.Add(a);
        });

        var p = System.Diagnostics.Process.Start(psi);

        var output = await p!.StandardOutput.ReadToEndAsync();
        await p!.WaitForExitAsync();

        // curl -i output: headers\r\n\r\nbody
        var parts = output.Split("\r\n\r\n", 2);
        var responseHeader = parts[0];
        var body = parts[1];

        // Feed Set-Cookie headers back into the shared CookieContainer.
        var headers = responseHeader.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var h in headers)
        {
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
        return response;
    }

    /// <summary>
    /// POST form body via curl (<c>--data-raw</c>), same header/cookie handling as GET.
    /// </summary>
    internal async Task<CURLResponse> PostAsync(string url, HttpContent content)
    {
        List<String> args = [];
        var targetUrl = new Uri(BaseAddress, url);
        args.Add(targetUrl.ToString());

        foreach (var header in DefaultRequestHeaders.OrderBy(h => h.Key).ToArray())
        {
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

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "curl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,

        };

        args.ForEach(a =>
        {
            psi.ArgumentList.Add(a);
        });

        var p = System.Diagnostics.Process.Start(psi);

        var output = await p!.StandardOutput.ReadToEndAsync();

        await p!.WaitForExitAsync();


        var parts = output.Split("\r\n\r\n", 2);
        var responseHeader = parts[0];
        var body = parts[1];

        var headers = responseHeader.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var h in headers)
        {
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

/// <summary>Minimal response object mimicking HttpResponseMessage for CURLClient callers.</summary>
public class CURLResponse : IDisposable
{
    private bool disposedValue;

    internal void EnsureSuccessStatusCode()
    {
        // Status checking not implemented yet for the curl path.
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

/// <summary>In-memory response body returned by <see cref="CURLClient"/>.</summary>
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
