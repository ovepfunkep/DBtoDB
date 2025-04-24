namespace DBtoDB.Models;

/// <summary>
/// Represents a request to execute a SQL query.
/// </summary>
public class SqlQueryRequest
{
    /// <summary>
    /// The SQL query to execute.
    /// Can be any valid SQL query supported by SQL Server.
    /// </summary>
    public required string SqlQuery { get; set; }

    /// <summary>
    /// Optional dictionary of parameter names and their values.
    /// Key is the parameter name (without @ symbol), value is the parameter value.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Indicates whether to cache the results of this query execution.
    /// Default is false.
    /// </summary>
    public bool UseCache { get; set; } = false;

    /// <summary>
    /// Optional cache duration in minutes.
    /// If not specified, the default timeout from configuration will be used.
    /// </summary>
    public int? CacheMinutes { get; set; }
} 