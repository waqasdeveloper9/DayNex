using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DayNex.HolidayService.Application.Services;

public sealed class BankHolidayService(IGovUkHolidayApiClient apiClient, IHolidaySyncStore store,
    ILogger<BankHolidayService> logger) : IBankHoliday
{
    public async Task<int> SyncHolidaysFromGovApiAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting holiday sync from GOV.UK API");
        var response = await apiClient.FetchBankHolidaysAsync(cancellationToken)
            ?? throw new HttpRequestException("GOV.UK returned an empty holiday response.");
        var holidays = new List<BankHoliday>();
        Collect(response.EnglandAndWales, Region.EnglandAndWales, holidays);
        Collect(response.Scotland, Region.Scotland, holidays);
        Collect(response.NorthernIreland, Region.NorthernIreland, holidays);
        var inserted = await store.SynchronizeAsync(holidays, cancellationToken);
        logger.LogInformation("Holiday sync completed: {Count} records inserted", inserted);
        return inserted;
    }

    private static void Collect(GovUkHolidayDivisionDto? division, Region region, List<BankHoliday> holidays)
    {
        if (division is null) return;
        foreach (var holiday in division.Events)
            holidays.Add(new BankHoliday(region, holiday.Date, holiday.Title,
                string.IsNullOrWhiteSpace(holiday.Notes) ? null : holiday.Notes, holiday.Bunting));
    }
}
