using Microsoft.AspNetCore.Mvc;

namespace BPS.Web.Controllers;

[Route("queue")]
public sealed class QueueController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

}
