using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Timestamp.Domain.Models;

namespace Timestamp.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Values> Values { get; set; }
        DbSet<Results> Results { get; set; }
        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
