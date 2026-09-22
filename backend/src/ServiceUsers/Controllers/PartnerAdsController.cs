using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/partner/ads")]
public sealed class PartnerAdsController : ControllerBase
{
    private readonly PartnerService _partners;
    private readonly CurrentUserService _currentUser;

    public PartnerAdsController(PartnerService partners, CurrentUserService currentUser)
    {
        _partners = partners;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PartnerAdsResponse>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var partner = await _currentUser.RequirePartnerAsync(_partners, cancellationToken);
        return Ok(await _partners.GetMyAdsAsync(partner, page, limit, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Create([FromBody] AdCreate data, CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequirePartnerAsync(_partners, cancellationToken);
        await _partners.CreateAdAsync(partner, data, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление создано" });
    }

    [HttpPut("{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Update(int adId, [FromBody] AdUpdate data, CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequirePartnerAsync(_partners, cancellationToken);
        await _partners.UpdateAdAsync(partner, adId, data, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление обновлено" });
    }

    [HttpDelete("{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Delete(int adId, CancellationToken cancellationToken)
    {
        var partner = await _currentUser.RequirePartnerAsync(_partners, cancellationToken);
        await _partners.DeleteAdAsync(partner, adId, cancellationToken);
        return Ok(new MessageResponse { Message = "Объявление удалено" });
    }
}
