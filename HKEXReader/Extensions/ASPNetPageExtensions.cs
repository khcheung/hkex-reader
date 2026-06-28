using System.Text.RegularExpressions;

namespace HKEXReader.Extensions;

public static class ASPNetPageExtensions
{
    extension(ASPNetPage page)
    {
        //var options = { MAX: new Date('2026/06/27') };
        //if (checkShareholdingDate.toLowerCase() == "true") {
        //options.MIN = new Date('2025/06/28')
        public DateTime? GetMaxDate()
        {
            Regex rxMax = new Regex(@"var options = { MAX: new Date\('([^']*)'\) };");
            var match = rxMax.Match(page.pageContent);
            var maxDate = DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.AssumeLocal);
            return maxDate;
        }

        public DateTime? GetMinDate()
        {
            return null;
        }
    }
}