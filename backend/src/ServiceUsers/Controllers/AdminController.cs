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

    [HttpPost("partner-requests/{userEmail}/reject")]
    public async Task<ActionResult<MessageResponse>> Reject(
        string userEmail,
        [FromBody] RejectPartnerRequest data,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        await _admin.RejectPartnerRequestAsync(userEmail, data.Comment, cancellationToken);
        return Ok(new MessageResponse { Message = "Заявка отклонена" });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminSummaryResponse>> Summary(CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("companies")]
    public async Task<ActionResult<List<AdminCompanyResponse>>> Companies(CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.GetCompaniesAsync(cancellationToken));
    }

    [HttpPost("managers")]
    public async Task<ActionResult<AdminUserResponse>> CreateManager(
        [FromBody] AdminManagerCreate data,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.CreateManagerAsync(data, cancellationToken));
    }

    [HttpPut("managers/{userId:int}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateManager(
        int userId,
        [FromBody] AdminManagerUpdate data,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.UpdateManagerAsync(userId, data, cancellationToken));
    }

    [HttpPost("managers/{userId:int}/partners/{partnerId:int}")]
    public async Task<ActionResult<MessageResponse>> AssignPartner(
        int userId,
        int partnerId,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        await _admin.AssignPartnerAsync(userId, partnerId, cancellationToken);
        return Ok(new MessageResponse { Message = "Компания закреплена" });
    }

    [HttpDelete("managers/{userId:int}/partners/{partnerId:int}")]
    public async Task<ActionResult<MessageResponse>> UnassignPartner(
        int userId,
        int partnerId,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        await _admin.UnassignPartnerAsync(userId, partnerId, cancellationToken);
        return Ok(new MessageResponse { Message = "Компания снята" });
    }

    [HttpPost("partners")]
    public async Task<ActionResult<AdminCompanyResponse>> CreatePartner(
        [FromBody] AdminPartnerCreate data,
        CancellationToken cancellationToken)
    {
        await _currentUser.RequireAdminAsync(cancellationToken);
        return Ok(await _admin.CreatePartnerAsync(data, cancellationToken));
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
