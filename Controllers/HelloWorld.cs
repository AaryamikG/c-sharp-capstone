using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloWorld : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello World");
    }
}