using Microsoft.AspNetCore.Mvc;
using DBtoDB.Models;
using DBtoDB.Services;

namespace DBtoDB.Controllers;

/// <summary>
/// API controller for executing SQL queries.
/// Provides an endpoint to execute SQL queries and return their results.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SqlQueryController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<SqlQueryController> _logger;

    /// <summary>
    /// Initializes a new instance of the SqlQueryController.
    /// </summary>
    /// <param name="databaseService">Service for executing database operations.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SqlQueryController(
        IDatabaseService databaseService,
        ILogger<SqlQueryController> logger)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a SQL query and returns its results.
    /// </summary>
    /// <param name="request">The request containing the SQL query details and parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// 200 OK with the query results if successful.
    /// 500 Internal Server Error if an error occurs during execution.
    /// </returns>
    /// <remarks>
    /// Sample request:
    /// POST /api/SqlQuery/execute
    /// {
    ///     "sqlQuery": "SELECT * FROM Customers WHERE Region = @Region",
    ///     "parameters": {
    ///         "Region": "North"
    ///     },
    ///     "useCache": true,
    ///     "cacheMinutes": 5
    /// }
    /// </remarks>
    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteSqlQuery(
        [FromBody] SqlQueryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing SQL query: {SqlQuery}", request.SqlQuery);

            var results = await _databaseService.ExecuteSqlQueryAsync(
                request.SqlQuery,
                request.Parameters,
                request.UseCache,
                request.CacheMinutes,
                cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL query: {SqlQuery}", request.SqlQuery);
            return StatusCode(500, new { error = "An error occurred while executing the SQL query." });
        }
    }
} 