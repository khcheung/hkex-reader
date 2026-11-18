using System.Text.RegularExpressions;

namespace HKEXReader.Extensions;

/// <summary>
/// Helpers for reading date-window metadata embedded in the Search SDW page script.
/// The page JS looks like: <c>var options = { MAX: new Date('yyyy/MM/dd') };</c>
/// </summary>
public static class ASPNetPageExtensions
{
    extension(ASPNetPage page)
    {
        /// <summary>
        /// Latest shareholding date the HKEX search form will accept.
        /// Parsed from the inline <c>options.MAX</c> JavaScript on the page.
        /// </summary>
        public DateTime? GetMaxDate()
        {
            Regex rxMax = new Regex(@"var options = { MAX: new Date\('([^']*)'\) };");
            var match = rxMax.Match(page.pageContent);
            var maxDate = DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.AssumeLocal);
            return maxDate;
        }

        /// <summary>
        /// Earliest shareholding date the form accepts.
        /// Currently unused (returns null); callers fall back to Today - 365 days.
        /// </summary>
        public DateTime? GetMinDate()
        {
            return null;
        }
    }
}
