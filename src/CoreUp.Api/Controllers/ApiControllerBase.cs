using System.Security.Claims;
using CoreUp.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new AppException(401, "The access token does not contain a valid user ID.");
}
