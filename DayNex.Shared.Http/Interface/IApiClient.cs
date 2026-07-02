using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayNex.Shared.Http.Interface
{
    public interface  IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
        Task<T?> PostAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default);
        Task<T?> PutAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default);
        Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    }
}
