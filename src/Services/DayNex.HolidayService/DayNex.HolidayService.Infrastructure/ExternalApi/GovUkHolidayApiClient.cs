using DayNex.HolidayService.Application.Common.Interfaces;
using DayNex.HolidayService.Application.DTOs;
using DayNex.HolidayService.Infrastructure.Setting;
using DayNex.Shared.Http.Interface;
using Microsoft.Extensions.Options;

namespace DayNex.HolidayService.Infrastructure.ExternalApi;

public class GovUkHolidayApiClient : IGovUkHolidayApiClient
{
    private readonly IApiClient _apiClient;
    private readonly GovUkApiSettings _settings;

    public GovUkHolidayApiClient(
        IApiClient apiClient,
        IOptions<GovUkApiSettings> options)
    {
        _apiClient = apiClient;
        _settings = options.Value;
    }

    public async Task<GovUkApiResponseDto?> FetchBankHolidaysAsync(
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<GovUkApiResponseDto>(
            _settings.BankHolidaysUrl,
            cancellationToken);
    }
}