using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace DBtoDB.Services;

/// <summary>
/// SQL Server implementation of the database service.
/// Provides functionality to execute stored procedures and SQL queries with caching support.
/// </summary>
public class SqlServerService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqlServerService> _logger;
    private readonly int _commandTimeout;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the SqlServerService.
    /// </summary>
    /// <param name="configuration">Application configuration containing connection strings and settings.</param>
    /// <param name="cache">Memory cache service for caching query results.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null or SQL Server connection string is missing.</exception>
    public SqlServerService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<SqlServerService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionString = configuration.GetConnectionString("SqlServer") 
            ?? throw new ArgumentNullException(nameof(configuration), "SQL Server connection string is missing");
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandTimeout = configuration.GetValue<int>("DatabaseSettings:CommandTimeoutSeconds");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteStoredProcedureAsync(
        string procedureName,
        Dictionary<string, object>? parameters = null,
        bool useCache = false,
        int? cacheMinutes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(procedureName))
            throw new ArgumentNullException(nameof(procedureName));

        var cacheKey = GetCacheKey("SP_" + procedureName, parameters);
        
        // Try to get results from cache if caching is enabled
        if (useCache && _cache.TryGetValue(cacheKey, out IEnumerable<Dictionary<string, object>>? cachedResult))
        {
            _logger.LogInformation("Cache hit for procedure: {ProcedureName}", procedureName);
            return cachedResult!;
        }

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _commandTimeout
        };

        AddParametersToCommand(command, parameters);

        try
        {
            await connection.OpenAsync(cancellationToken);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = await ReadResultsAsync(reader, cancellationToken);

            // Cache the results if caching is enabled
            if (useCache)
            {
                CacheResults(cacheKey, results, cacheMinutes);
                _logger.LogInformation("Cached results for procedure: {ProcedureName}", procedureName);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure: {ProcedureName}", procedureName);
            throw new InvalidOperationException($"Failed to execute stored procedure: {procedureName}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteSqlQueryAsync(
        string sqlQuery,
        Dictionary<string, object>? parameters = null,
        bool useCache = false,
        int? cacheMinutes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sqlQuery))
            throw new ArgumentNullException(nameof(sqlQuery));

        var cacheKey = GetCacheKey("SQL_" + sqlQuery, parameters);
        
        // Try to get results from cache if caching is enabled
        if (useCache && _cache.TryGetValue(cacheKey, out IEnumerable<Dictionary<string, object>>? cachedResult))
        {
            _logger.LogInformation("Cache hit for SQL query: {SqlQuery}", sqlQuery);
            return cachedResult!;
        }

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sqlQuery, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = _commandTimeout
        };

        AddParametersToCommand(command, parameters);

        try
        {
            await connection.OpenAsync(cancellationToken);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = await ReadResultsAsync(reader, cancellationToken);

            // Cache the results if caching is enabled
            if (useCache)
            {
                CacheResults(cacheKey, results, cacheMinutes);
                _logger.LogInformation("Cached results for SQL query: {SqlQuery}", sqlQuery);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL query: {SqlQuery}", sqlQuery);
            throw new InvalidOperationException($"Failed to execute SQL query: {sqlQuery}", ex);
        }
    }

    /// <summary>
    /// Reads results from a SqlDataReader and converts them to a list of dictionaries.
    /// </summary>
    /// <param name="reader">The SQL data reader containing the results.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of dictionaries where each dictionary represents a row.</returns>
    private async Task<List<Dictionary<string, object>>> ReadResultsAsync(
        SqlDataReader reader, 
        CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object>>();
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            
            // Convert each column to a dictionary entry
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value == DBNull.Value ? null! : value;
            }
            
            results.Add(row);
        }

        return results;
    }

    /// <summary>
    /// Adds parameters to a SQL command.
    /// </summary>
    /// <param name="command">The command to add parameters to.</param>
    /// <param name="parameters">The parameters to add.</param>
    private void AddParametersToCommand(SqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }
    }

    /// <summary>
    /// Caches the results of a query or stored procedure.
    /// </summary>
    /// <param name="cacheKey">The cache key to use.</param>
    /// <param name="results">The results to cache.</param>
    /// <param name="cacheMinutes">Optional cache duration in minutes.</param>
    private void CacheResults(string cacheKey, IEnumerable<Dictionary<string, object>> results, int? cacheMinutes)
    {
        var cacheTimeout = TimeSpan.FromMinutes(
            cacheMinutes ?? 
            _configuration.GetValue<int>("DatabaseSettings:CacheTimeoutMinutes"));
        
        _cache.Set(cacheKey, results, cacheTimeout);
    }

    /// <summary>
    /// Generates a unique cache key for a database operation based on its name and parameters.
    /// </summary>
    /// <param name="operationName">The name of the operation (stored procedure or SQL query).</param>
    /// <param name="parameters">The parameters passed to the operation.</param>
    /// <returns>A string that can be used as a cache key.</returns>
    private static string GetCacheKey(string operationName, Dictionary<string, object>? parameters)
    {
        if (parameters == null || !parameters.Any())
            return operationName;

        // Create a deterministic string from parameters
        var paramString = string.Join("_", 
            parameters.OrderBy(p => p.Key)
                     .Select(p => $"{p.Key}={p.Value}"));
        
        return $"{operationName}_{paramString}";
    }
} 