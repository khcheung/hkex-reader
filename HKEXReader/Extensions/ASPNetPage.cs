using System.Text.RegularExpressions;

namespace HKEXReader.Extensions;

public class ASPNetPage
{
    public string pageContent { get; private set; }

    public ASPNetPage(string pageContent)
    {
        // Initialize the ASPNetPage with the provided page content
        this.pageContent = pageContent;
        this.ExtractPage();
    }

    public string ViewState { get; private set; } = String.Empty;
    public string EventValidation { get; private set; } = String.Empty;
    public string EventTarget { get; private set; } = String.Empty;
    public string ViewStateGenerator { get; private set; } = String.Empty;


    private void ExtractPage()
    {
        Regex rxHidden = new Regex(@"<input type=""hidden"" name=""(?<name>[^""]+)"" id=""(?<id>[^""]+)"" value=""(?<value>[^""]*)"" />", RegexOptions.Compiled);
        MatchCollection matches = rxHidden.Matches(pageContent);

        foreach (Match match in matches)
        {
            string name = match.Groups["name"].Value;
            string id = match.Groups["id"].Value;
            string value = match.Groups["value"].Value;

            // Process the extracted values as needed
            // For example, you can store them in a dictionary or perform other operations
            switch (name)
            {
                case "__VIEWSTATE":
                    // Store or process the __VIEWSTATE value
                    this.ViewState = value;
                    break;
                case "__EVENTVALIDATION":
                    // Store or process the __EVENTVALIDATION value
                    this.EventValidation = value;
                    break;
                case "__EVENTTARGET":
                    // Store or process the __EVENTTARGET value
                    this.EventTarget = value;
                    break;
                case "__VIEWSTATEGENERATOR":
                    // Store or process the __VIEWSTATEGENERATOR value
                    this.ViewStateGenerator = value;
                    break;
                default:
                    // Handle other hidden input fields if needed
                    break;
            }
        }
    }


    public static implicit operator ASPNetPage(string pageContent)
    {
        return new ASPNetPage(pageContent);
    }
}
