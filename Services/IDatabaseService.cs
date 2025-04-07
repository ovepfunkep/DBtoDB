using System.Data;

namespace DBtoDB.Services;

/// <summary>
/// Defines operations for executing database stored procedures.
/// This interface abstracts the database operations to allow for different implementations
/// (e.g., SQL Server, PostgreSQL, etc.).
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Executes a stored procedure asynchronously and returns the results.
    /// </summary>
    /// <param name="procedureName">The name of the stored procedure to execute.</param>
    /// <param name="parameters">Optional parameters for the stored procedure.</param>
    /// <param name="useCache">Whether to cache the results.</param>
    /// <param name="cacheMinutes">Optional cache duration in minutes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of dictionaries where each dictionary represents a row,
    /// with column names as keys and column values as values.
    /// </returns>
    /// <exception cref="ArgumentNullException">When procedureName is null.</exception>
    /// <exception cref="InvalidOperationException">When the database operation fails.</exception>
    Task<IEnumerable<Dictionary<string, object>>> ExecuteStoredProcedureAsync(
        string procedureName,
        Dictionary<string, object>? parameters = null,
        bool useCache = false,
        int? cacheMinutes = null,
        CancellationToken cancellationToken = default);
} 