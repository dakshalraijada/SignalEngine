namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Interface for unit of work pattern.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
