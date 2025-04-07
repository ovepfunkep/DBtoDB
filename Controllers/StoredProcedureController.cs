using Microsoft.AspNetCore.Mvc;
using DBtoDB.Models;
using DBtoDB.Services;

namespace DBtoDB.Controllers;

/// <summary>
/// API controller for executing SQL Server stored procedures.
/// Provides an endpoint to execute stored procedures and return their results.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StoredProcedureController : ControllerBase
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<StoredProcedureController> _logger;

    /// <summary>
    /// Initializes a new instance of the StoredProcedureController.
    /// </summary>
    /// <param name="databaseService">Service for executing database operations.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public StoredProcedureController(
        IDatabaseService databaseService,
        ILogger<StoredProcedureController> logger)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a stored procedure and returns its results.
    /// </summary>
    /// <param name="request">The request containing the stored procedure details and parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// 200 OK with the stored procedure results if successful.
    /// 500 Internal Server Error if an error occurs during execution.
    /// </returns>
    /// <remarks>
    /// Sample request:
    /// POST /api/StoredProcedure/execute
    /// {
    ///     "procedureName": "GetCustomerDetails",
    ///     "parameters": {
    ///         "CustomerId": "12345",
    ///         "IncludeOrders": true
    ///     },
    ///     "useCache": true,
    ///     "cacheMinutes": 5
    /// }
    /// </remarks>
    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteStoredProcedure(
        [FromBody] StoredProcedureRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing stored procedure: {ProcedureName}", request.ProcedureName);

            var results = await _databaseService.ExecuteStoredProcedureAsync(
                request.ProcedureName,
                request.Parameters,
                request.UseCache,
                request.CacheMinutes,
                cancellationToken);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure: {ProcedureName}", request.ProcedureName);
            return StatusCode(500, new { error = "An error occurred while executing the stored procedure." });
        }
    }
} 