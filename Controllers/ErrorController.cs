using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballClubManagement.Controllers;

[AllowAnonymous]
public sealed class ErrorController : Controller
{
    [HttpGet]
    public IActionResult NotFound(int? statusCode)
    {
        ViewData["StatusCode"] = statusCode ?? 404;
        Response.StatusCode = statusCode ?? 404;
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    [HttpGet]
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        return View();
    }
}
