namespace DBtoDB.Models;

/// <summary>
/// Represents a request to execute a SQL Server stored procedure.
/// </summary>
public class StoredProcedureRequest
{
    /// <summary>
    /// The name of the stored procedure to execute.
    /// Can include schema name (e.g., "dbo.GetCustomerDetails").
    /// </summary>
    public required string ProcedureName { get; set; }

    /// <summary>
    /// Optional dictionary of parameter names and their values.
    /// Key is the parameter name (without @ symbol), value is the parameter value.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Indicates whether to cache the results of this stored procedure execution.
    /// Default is false.
    /// </summary>
    public bool UseCache { get; set; } = false;

    /// <summary>
    /// Optional cache duration in minutes.
    /// If not specified, the default timeout from configuration will be used.
    /// </summary>
    public int? CacheMinutes { get; set; }
} 