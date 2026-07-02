using DayNex.HolidayService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.HolidayService.Infrastructure.Persistence
{
    public class HolidayDbContext : DbContext
    {
        public DbSet<BankHoliday>   BankHoliday { get; set; }   

    }
}
