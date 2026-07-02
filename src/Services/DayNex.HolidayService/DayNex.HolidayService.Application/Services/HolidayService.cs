using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DayNex.HolidayService.Application.Services;

public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _repository;
    private readonly IGovUkHolidayApiClient _apiClient;
    private readonly ILogger<HolidayService> _logger;

    public HolidayService(
        IHolidayRepository repository,
        IGovUkHolidayApiClient apiClient,
        ILogger<HolidayService> logger)
    {
        _repository = repository;
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<BankHolidayDto>> GetByRegionAsync(
        UkRegion region, CancellationToken cancellationToken = default)
    {
        var holidays = await _repository.GetByRegionAsync(region, cancellationToken);

        return holidays.Select(MapToDto).ToList();
    }

    public async Task<BankHolidayDto?> GetNextHolidayAsync(
        Region region, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var holiday = await _repository.GetNextHolidayAsync(region, today, cancellationToken);

        return holiday is null ? null : MapToDto(holiday);
    }

    public async Task<int> SyncHolidaysFromGovApiAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting holiday sync from GOV.UK API");

        var response = await _apiClient.FetchBankHolidaysAsync(cancellationToken);

        if (response is null)
        {
            _logger.LogWarning("GOV.UK API returned null response");
            return 0;
        }

        var newHolidays = new List<BankHoliday>();

        await CollectNewHolidaysAsync(response.EnglandAndWales, Region.EnglandAndWales, newHolidays, cancellationToken);
        await CollectNewHolidaysAsync(response.Scotland, Region.Scotland, newHolidays, cancellationToken);
        await CollectNewHolidaysAsync(response.NorthernIreland, Region.NorthernIreland, newHolidays, cancellationToken);

        if (newHolidays.Count > 0)
        {
            await _repository.AddRangeAsync(newHolidays, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Holiday sync completed. {Count} new records inserted", newHolidays.Count);

        return newHolidays.Count;
    }

    private async Task CollectNewHolidaysAsync(
        DTOs.GovUkDivisionDto? division,
        Region region,
        List<BankHoliday> newHolidays,
        CancellationToken cancellationToken)
    {
        if (division is null) return;

        foreach (var ev in division.Events)
        {
            var date = DateOnly.Parse(ev.Date);

            var exists = await _repository.ExistsAsync(ev.Title, date, region, cancellationToken);
            if (exists) continue;

            newHolidays.Add(new BankHoliday
            {
                Title = ev.Title,
                Date = date,
                Notes = string.IsNullOrWhiteSpace(ev.Notes) ? null : ev.Notes,
                Bunting = ev.Bunting,
                Region = region
            });
        }
    }

    private static BankHolidayDto MapToDto(BankHoliday holiday) => new()
    {
        Title = holiday.Title,
        Date = holiday.Date,
        Notes = holiday.Notes,
        Bunting = holiday.Bunting,
        Region = holiday.Region.ToString()
    };
}