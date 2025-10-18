// ============================================
// File: DevPioneers.Api/Controllers/WalletController.cs
// Wallet management controller with full CRUD operations
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.Commands;
using DevPioneers.Application.Features.Wallet.DTOs;
using DevPioneers.Application.Features.Wallet.Queries;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Wallet management controller for handling wallet operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication for all actions
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IMediator mediator,
        ILogger<WalletController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ============================================
    // GET: Get Wallet Balance
    // ============================================

    /// <summary>
    /// Get current user's wallet balance and details
    /// </summary>
    /// <returns>Wallet information with balance and points</returns>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWalletBalance()
    {
        try
        {
            // Get current user ID from JWT claims
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            var query = new GetWalletBalanceQuery(userId);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse.NotFound(result.ErrorMessage ?? "Wallet not found"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Wallet balance retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet balance");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving wallet balance"));
        }
    }

    /// <summary>
    /// Get wallet balance for specific user (Admin only)
    /// </summary>
    /// <param name="userId">User ID to get wallet for</param>
    /// <returns>Wallet information for specified user</returns>
    [HttpGet("balance/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserWalletBalance(int userId)
    {
        try
        {
            var query = new GetWalletBalanceQuery(userId);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse.NotFound(result.ErrorMessage ?? "Wallet not found"));
            }

            return Ok(ApiResponse.Ok(result.Data, $"Wallet balance for user {userId} retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet balance for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving wallet balance"));
        }
    }

    // ============================================
    // GET: Transaction History
    // ============================================

    /// <summary>
    /// Get current user's transaction history with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="type">Transaction type filter (Credit/Debit)</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="searchTerm">Search term for description</param>
    /// <returns>Paginated list of transactions</returns>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TransactionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Get current user ID
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            var query = new GetTransactionHistoryQuery(
                userId,
                pageNumber,
                pageSize,
                type,
                fromDate,
                toDate,
                searchTerm);

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve transaction history"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Transaction history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving transaction history"));
        }
    }

    /// <summary>
    /// Get transaction history for specific user (Admin only)
    /// </summary>
    /// <param name="userId">User ID to get transactions for</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="type">Transaction type filter</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <param name="searchTerm">Search term for description</param>
    /// <returns>Paginated list of transactions for specified user</returns>
    [HttpGet("transactions/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTransactionHistory(
        int userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TransactionType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = new GetTransactionHistoryQuery(
                userId,
                pageNumber,
                pageSize,
                type,
                fromDate,
                toDate,
                searchTerm);

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve transaction history"));
            }

            return Ok(ApiResponse.Ok(result.Data, $"Transaction history for user {userId} retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving transaction history"));
        }
    }

    // ============================================
    // POST: Credit Wallet
    // ============================================

    /// <summary>
    /// Credit amount to user's wallet
    /// </summary>
    /// <param name="request">Credit wallet request</param>
    /// <returns>Transaction details</returns>
    [HttpPost("credit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreditWallet([FromBody] CreditWalletRequest request)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            // Validate request
            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Amount must be greater than zero"));
            }

            var command = new CreditWalletCommand(
                userId,
                request.Amount,
                request.Currency ?? "EGP",
                request.Description ?? "Wallet credit",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to credit wallet"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Wallet credited successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting wallet");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while crediting wallet"));
        }
    }

    /// <summary>
    /// Credit amount to specific user's wallet (Admin only)
    /// </summary>
    /// <param name="userId">User ID to credit</param>
    /// <param name="request">Credit wallet request</param>
    /// <returns>Transaction details</returns>
    [HttpPost("credit/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreditUserWallet(int userId, [FromBody] CreditWalletRequest request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Amount must be greater than zero"));
            }

            var command = new CreditWalletCommand(
                userId,
                request.Amount,
                request.Currency ?? "EGP",
                request.Description ?? "Admin wallet credit",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to credit wallet"));
            }

            return Ok(ApiResponse.Ok(result.Data, $"Wallet credited successfully for user {userId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting wallet for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ServerError("An error occurred while crediting wallet"));
        }
    }

    // ============================================
    // POST: Debit Wallet
    // ============================================

    /// <summary>
    /// Debit amount from user's wallet
    /// </summary>
    /// <param name="request">Debit wallet request</param>
    /// <returns>Transaction details</returns>
    [HttpPost("debit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DebitWallet([FromBody] DebitWalletRequest request)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            // Validate request
            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Amount must be greater than zero"));
            }

            var command = new DebitWalletCommand(
                userId,
                request.Amount,
                request.Currency ?? "EGP",
                request.Description ?? "Wallet debit",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to debit wallet"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Wallet debited successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debiting wallet");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while debiting wallet"));
        }
    }

    /// <summary>
    /// Debit amount from specific user's wallet (Admin only)
    /// </summary>
    /// <param name="userId">User ID to debit</param>
    /// <param name="request">Debit wallet request</param>
    /// <returns>Transaction details</returns>
    [HttpPost("debit/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DebitUserWallet(int userId, [FromBody] DebitWalletRequest request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Amount must be greater than zero"));
            }

            var command = new DebitWalletCommand(
                userId,
                request.Amount,
                request.Currency ?? "EGP",
                request.Description ?? "Admin wallet debit",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to debit wallet"));
            }

            return Ok(ApiResponse.Ok(result.Data, $"Wallet debited successfully for user {userId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debiting wallet for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ServerError("An error occurred while debiting wallet"));
        }
    }

    // ============================================
    // POST: Transfer Points
    // ============================================

    /// <summary>
    /// Transfer points between wallets
    /// </summary>
    /// <param name="request">Transfer points request</param>
    /// <returns>Transfer transaction details</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferPoints([FromBody] TransferPointsRequest request)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var fromUserId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            // Validate request
            if (request.ToUserId == fromUserId)
            {
                return BadRequest(ApiResponse.BadRequest("Cannot transfer to yourself"));
            }

            if (request.Points <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Points must be greater than zero"));
            }

            var command = new TransferPointsCommand(
                fromUserId,
                request.ToUserId,
                request.Points,
                request.Description ?? "Points transfer");

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to transfer points"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Points transferred successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring points");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while transferring points"));
        }
    }

    // ============================================
    // GET: Wallet Statistics (Admin only)
    // ============================================

    /// <summary>
    /// Get comprehensive wallet statistics for admin dashboard
    /// </summary>
    /// <returns>Comprehensive wallet statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComprehensiveWalletStatistics()
    {
        try
        {
            var query = new GetComprehensiveWalletStatisticsQuery();
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve wallet statistics"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Wallet statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet statistics");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving wallet statistics"));
        }
    }

    /// <summary>
    /// Get wallet statistics for current user
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <returns>User wallet statistics</returns>
    [HttpGet("my-statistics")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWalletStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            var query = new GetWalletStatisticsQuery(userId, fromDate, toDate);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve wallet statistics"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Wallet statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my wallet statistics");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving wallet statistics"));
        }
    }

    // ============================================
    // GET: All User Wallets (Admin only)
    // ============================================

    /// <summary>
    /// Get paginated list of all user wallets for admin
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term for user name or email</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>Paginated list of user wallets</returns>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserWallets(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = new GetUserWalletsQuery(
                pageNumber,
                pageSize,
                searchTerm,
                isActive);

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve user wallets"));
            }

            return Ok(ApiResponse.Ok(result.Data, "User wallets retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user wallets");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while retrieving user wallets"));
        }
    }

    /// <summary>
    /// Add points to user's wallet
    /// </summary>
    [HttpPost("add-points")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPoints([FromBody] AddPointsRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            if (request.Points <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Points must be greater than zero"));
            }

            var command = new AddPointsCommand(
                userId,
                request.Points,
                request.Description ?? "Points added",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to add points"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Points added successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding points");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while adding points"));
        }
    }

    /// <summary>
    /// Deduct points from user's wallet
    /// </summary>
    [HttpPost("deduct-points")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeductPoints([FromBody] DeductPointsRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.Unauthorized("Invalid user token"));
            }

            if (request.Points <= 0)
            {
                return BadRequest(ApiResponse.BadRequest("Points must be greater than zero"));
            }

            var command = new DeductPointsCommand(
                userId,
                request.Points,
                request.Description ?? "Points deducted",
                request.RelatedEntityType,
                request.RelatedEntityId);

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to deduct points"));
            }

            return Ok(ApiResponse.Ok(result.Data, "Points deducted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting points");
            return StatusCode(500, ApiResponse.ServerError("An error occurred while deducting points"));
        }
    }
}

/// <summary>
/// Request model for adding points
/// </summary>
public class AddPointsRequest
{
    public int Points { get; set; }
    public string? Description { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

/// <summary>
/// Request model for deducting points
/// </summary>
public class DeductPointsRequest
{
    public int Points { get; set; }
    public string? Description { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

// ============================================
// Request/Response DTOs
// ============================================

/// <summary>
/// Request model for crediting wallet
/// </summary>
public class CreditWalletRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; } = "EGP";
    public string? Description { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

/// <summary>
/// Request model for debiting wallet
/// </summary>
public class DebitWalletRequest
{
    public decimal Amount { get; set; }
    public string? Currency { get; set; } = "EGP";
    public string? Description { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}

/// <summary>
/// Request model for transferring points
/// </summary>
public class TransferPointsRequest
{
    public int ToUserId { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Result model for points transfer
/// </summary>
public class TransferPointsResult
{
    public TransactionDto FromTransaction { get; set; } = null!;
    public TransactionDto ToTransaction { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}