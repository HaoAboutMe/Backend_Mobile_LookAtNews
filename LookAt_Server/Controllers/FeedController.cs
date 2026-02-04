using LookAt_Server.Models;
using LookAt_Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LookAt_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedController : ControllerBase
    {
        private readonly IMongoCollection<Article> _articles;
        private readonly UserService _users;

        public FeedController(IMongoDatabase db, UserService users)
        {
            _articles = db.GetCollection<Article>("Articles");
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonalFeed()
        {
            var user = await _users.GetCurrentUser(HttpContext);

            if (user.FavoriteCategories.Count == 0)
                return Ok(await _articles.Find(FilterDefinition<Article>.Empty).Limit(20).ToListAsync());

            var filter = Builders<Article>.Filter.In(a => a.Category, user.FavoriteCategories);
            var list = await _articles.Find(filter).Limit(40).ToListAsync();

            return Ok(list);
        }
    }
}
