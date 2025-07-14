using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2API.Controllers;

[ApiController]
[Route("[controller]")]
public class SlideController : ChapControllerAbstractBase
{
    public SlideController(ILogger<SlideController> logger)
        : base(logger)
    {
    }
} 