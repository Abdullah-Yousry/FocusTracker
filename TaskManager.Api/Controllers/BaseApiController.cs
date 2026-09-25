using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Domain.Entities;
namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
