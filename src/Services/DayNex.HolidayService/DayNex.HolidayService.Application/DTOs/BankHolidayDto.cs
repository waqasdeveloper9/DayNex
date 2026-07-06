using DayNex.HolidayService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Application.DTOs
{
    public class BankHolidayDto
    {
        public Region Region { get;  set; }

        public DateOnly Date { get;  set; }

        public string Title { get;  set; } = string.Empty;

        public string? Notes { get;  set; }

        public bool Bunting { get;  set; }


    }
}
