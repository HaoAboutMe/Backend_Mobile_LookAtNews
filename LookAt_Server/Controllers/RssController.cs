using LookAt_Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LookAt_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private readonly RssService _rssService;

        public RssController(RssService rssService)
        {
            _rssService = rssService;
        }

        // Fetch all RSS
        [HttpPost("fetch")]
        public async Task<IActionResult> Fetch()
        {
            var result = await _rssService.FetchAllRssAsync();
            return Ok(new { added = result.TotalAdded, details = result.Details });
        }
    }
}
