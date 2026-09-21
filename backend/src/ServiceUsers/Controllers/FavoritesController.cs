using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceUsers.Services;
using StudentPass.Contracts;

namespace ServiceUsers.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/favorites")]
public sealed class FavoritesController : ControllerBase
{
    private readonly AdService _ads;
    private readonly CurrentUserService _currentUser;

    public FavoritesController(AdService ads, CurrentUserService currentUser)
    {
        _ads = ads;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<FavoriteListResponse>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(cancellationToken);
        return Ok(await _ads.GetFavoritesAsync(user.Email, page, limit, cancellationToken));
    }

    [HttpPost("{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Add(int adId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetRequiredUserAsync(cancellationToken);
        await _ads.AddFavoriteAsync(user.Email, adId, cancellationToken);
        return Ok(new MessageResponse { Message = "Добавлено в избранное" });
    }

    [HttpDelete("{adId:int}")]
    public async Task<ActionResult<MessageResponse>> Remove(int adId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetRequiredUserAsync(cancellationToken);
        await _ads.RemoveFavoriteAsync(user.Email, adId, cancellationToken);
        return Ok(new MessageResponse { Message = "Удалено из избранного" });
    }
}
