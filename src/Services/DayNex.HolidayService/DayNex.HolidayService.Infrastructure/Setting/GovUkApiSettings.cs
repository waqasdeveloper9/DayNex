using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Infrastructure.Setting
{
    public class GovUkApiSettings
    {
        public const string SectionName = "GovUkApiSettings";

        public string BankHolidaysUrl { get; set; } = string.Empty;
    }
}
