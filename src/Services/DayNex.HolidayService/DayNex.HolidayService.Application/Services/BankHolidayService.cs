using DayNex.Domain.Common.Interface;
using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Domain.Entities;
using DayNex.HolidayService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DayNex.HolidayService.Application.Services;

public class BankHolidayService : IBankHoliday
{
    private readonly IRepository<BankHoliday> _repository;
    private readonly IGovUkHolidayApiClient _apiClient;
    private readonly ILogger<BankHolidayService> _logger;

    public BankHolidayService(
        IGovUkHolidayApiClient apiClient,
        IRepository<BankHoliday> repository,
    ILogger<BankHolidayService> logger)
    {
         _apiClient = apiClient;
        _logger = logger;
        _repository = repository;
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
        await CollectNewHolidaysAsync(response, newHolidays, cancellationToken);

        if (newHolidays.Count > 0)
        {
            await _repository.AddRangeAsync(newHolidays);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Holiday sync completed. {Count} new records inserted", newHolidays.Count);
        return newHolidays.Count;
    }

   
    private async Task CollectNewHolidaysAsync(
       GovUkApiResponseDto response,
       List<BankHoliday> newHolidays,
       CancellationToken cancellationToken)
    {
        var divisions = new Dictionary<Region, GovUkHolidayDivisionDto?>
    {
        { Region.EnglandAndWales, response.EnglandAndWales },
        { Region.Scotland, response.Scotland },
        { Region.NorthernIreland, response.NorthernIreland }
    };

        foreach (var (region, division) in divisions)
        {
            if (division is null)
                continue;

            foreach (var holiday in division.Events)
            {
                //var exists = await _repository.ExistsAsync(
                //    holiday.Title,
                //    holiday.Date,
                //    region,
                //    cancellationToken);

                //if (exists)
                //    continue;

                newHolidays.Add(new BankHoliday(
                    region,
                    holiday.Date,
                    holiday.Title,
                    string.IsNullOrWhiteSpace(holiday.Notes)
                        ? null
                        : holiday.Notes,
                    holiday.Bunting
                    ));
            }
        }
    }
    private static BankHolidayDto MapToDto(BankHoliday holiday) => new()
    {
        Title = holiday.Title,
        Date = holiday.Date,
        Notes = holiday.Notes,
        Bunting = holiday.Bunting,
        Region = holiday.Region
    };
}