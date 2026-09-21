using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminService _admin;
    private readonly CurrentUserService _currentUser;

    public AdminController(AdminService admin, CurrentUserService currentUser)
    {
        _admin = admin;
        _currentUser = currentUser;
    }

    [HttpGet("partner-requests")]
    public async Task<ActionResult<AdminPartnerRequestListResponse>> GetRequests(
        [FromQuery] PartnerRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.GetPartnerRequestsAsync(status, page, limit, cancellationToken));
    }

    [HttpPost("partner-requests/{userEmail}")]
    public async Task<ActionResult<MessageResponse>> Approve(string userEmail, CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        await _admin.ApprovePartnerRequestAsync(userEmail, cancellationToken);
        return Ok(new MessageResponse { Message = "Заявка обработана" });
    }

    [HttpGet("users")]
    public async Task<ActionResult<AdminUserListResponse>> GetUsers(
        [FromQuery] UserRole? role,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.GetUsersAsync(role, search, page, limit, cancellationToken));
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        await _admin.DeleteUserAsync(userId, cancellationToken);
        return Ok(new MessageResponse { Message = "Пользователь был полностью удален" });
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CategoryCreate data, CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.CreateCategoryAsync(data, cancellationToken));
    }
}
