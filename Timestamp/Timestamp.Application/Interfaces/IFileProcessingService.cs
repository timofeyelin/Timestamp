using Microsoft.AspNetCore.Http;

namespace Timestamp.Application.Interfaces
{
    public interface IFileProcessingService
    {
        Task ProcessFileAsync(IFormFile file, CancellationToken cancellationToken);
    }
}
