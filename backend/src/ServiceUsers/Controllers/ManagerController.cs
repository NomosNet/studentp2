using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/manager")]
public sealed class ManagerController : ControllerBase
{
    private readonly PartnerService _partners;
    private readonly CurrentUserService _currentUser;

    public ManagerController(PartnerService partners, CurrentUserService currentUser)
    {
        _partners = partners;
        _currentUser = currentUser;
    }

    [HttpGet("companies")]
    public async Task<ActionResult<List<ManagerCompanyResponse>>> Companies(CancellationToken cancellationToken)
    {
        var manager = await _currentUser.RequireManagerAsync(cancellationToken);
        return Ok(await _partners.GetManagedCompaniesAsync(manager.Email, cancellationToken));
    }

    [HttpGet("companies/{partnerId:int}/ads")]
    public async Task<ActionResult<PartnerAdsResponse>> Ads(
        int partnerId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var partner = await _currentUser.RequireAssignedPartnerAsync(partnerId, cancellationToken);
        return Ok(await _partners.GetMyAdsAsync(partner, page, limit, cancellationToken));
    }

    [HttpPost("companies/{partnerId:int}/ads")]
    public async Task<ActionResult<MessageResponse>> Create(
        int partnerId,
        [FromBody] AdCreate data,
        CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequireAssignedPartnerAsync(partnerId, cancellationToken);
        await _partners.CreateAdAsync(partner, data, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление создано" });
    }

    [HttpPut("companies/{partnerId:int}/ads/{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Update(
        int partnerId,
        int adId,
        [FromBody] AdUpdate data,
        CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequireAssignedPartnerAsync(partnerId, cancellationToken);
        await _partners.UpdateAdAsync(partner, adId, data, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление обновлено" });
    }

    [HttpDelete("companies/{partnerId:int}/ads/{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Delete(
        int partnerId,
        int adId,
        CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequireAssignedPartnerAsync(partnerId, cancellationToken);
        await _partners.DeleteAdAsync(partner, adId, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление удалено" });
    }
}
