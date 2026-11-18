using System.Text.RegularExpressions;

namespace HKEXReader.Extensions;

/// <summary>
/// Thin wrapper around an ASP.NET WebForms HTML page.
/// Extracts the hidden fields (__VIEWSTATE, etc.) required for postbacks.
/// </summary>
public class ASPNetPage
{
    /// <summary>Raw HTML of the page response.</summary>
    public string pageContent { get; private set; }

    public ASPNetPage(string pageContent)
    {
        this.pageContent = pageContent;
        this.ExtractPage();
    }

    /// <summary>Serialized control tree; must be echoed on every form POST.</summary>
    public string ViewState { get; private set; } = String.Empty;

    /// <summary>Event validation token used by ASP.NET to validate postbacks.</summary>
    public string EventValidation { get; private set; } = String.Empty;

    /// <summary>Optional event target (usually empty on the initial form).</summary>
    public string EventTarget { get; private set; } = String.Empty;

    /// <summary>ViewState generator ID for the page class.</summary>
    public string ViewStateGenerator { get; private set; } = String.Empty;


    /// <summary>Scan hidden inputs and capture the ASP.NET state fields we need for POSTs.</summary>
    private void ExtractPage()
    {
        Regex rxHidden = new Regex(@"<input type=""hidden"" name=""(?<name>[^""]+)"" id=""(?<id>[^""]+)"" value=""(?<value>[^""]*)"" />", RegexOptions.Compiled);
        MatchCollection matches = rxHidden.Matches(pageContent);

        foreach (Match match in matches)
        {
            string name = match.Groups["name"].Value;
            string id = match.Groups["id"].Value;
            string value = match.Groups["value"].Value;

            switch (name)
            {
                case "__VIEWSTATE":
                    this.ViewState = value;
                    break;
                case "__EVENTVALIDATION":
                    this.EventValidation = value;
                    break;
                case "__EVENTTARGET":
                    this.EventTarget = value;
                    break;
                case "__VIEWSTATEGENERATOR":
                    this.ViewStateGenerator = value;
                    break;
                default:
                    break;
            }
        }
    }


    /// <summary>Allow assigning HTML strings directly to <see cref="ASPNetPage"/>.</summary>
    public static implicit operator ASPNetPage(string pageContent)
    {
        return new ASPNetPage(pageContent);
    }
}
