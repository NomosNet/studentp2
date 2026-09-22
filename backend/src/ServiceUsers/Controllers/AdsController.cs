using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Route("api/v1/ads")]
public sealed class AdsController : ControllerBase
{
    private readonly AdService _ads;
    private readonly CurrentUserService _currentUser;

    public AdsController(AdService ads, CurrentUserService currentUser)
    {
        _ads = ads;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<AdListResponse>> GetAds(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _ads.GetAdsAsync(category, search, sort, page, limit, _currentUser.TryGetEmail(), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await _ads.GetCategoriesAsync(cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{adId:int}")]
    public async Task<ActionResult<AdDetailResponse>> GetAd(int adId, CancellationToken cancellationToken)
    {
        return Ok(await _ads.GetAdAsync(adId, _currentUser.TryGetEmail(), cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("{adId:int}/click")]
    public async Task<IActionResult> Click(int adId, CancellationToken cancellationToken)
    {
        var url = await _ads.ClickAdAsync(adId, cancellationToken);
        return Redirect(url);
    }
}
