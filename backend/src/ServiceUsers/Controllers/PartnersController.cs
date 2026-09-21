using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Route("api/v1/partners")]
public sealed class PartnersController : ControllerBase
{
    private readonly PartnerService _partners;

    public PartnersController(PartnerService partners)
    {
        _partners = partners;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<PartnerResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _partners.GetPartnersAsync(search, limit, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{partnerId:int}/ads")]
    public async Task<ActionResult<AdListResponse>> GetAds(
        int partnerId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _partners.GetPartnerAdsPublicAsync(partnerId, page, limit, cancellationToken));
    }
}
