using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Application.Common
{
    public class GovUkApiSettings
    {
        public const string SectionName = "GovUkApiSettings";

        public string BaseUrl { get; set; } = string.Empty;

        public string BankHolidaysEndpoint { get; set; } = string.Empty;
    }
}
