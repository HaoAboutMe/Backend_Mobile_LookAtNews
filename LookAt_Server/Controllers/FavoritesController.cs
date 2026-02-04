using LookAt_Server.Models;
using LookAt_Server.Models.DTO.Request;
using LookAt_Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LookAt_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly IMongoCollection<Favorite> _favorites;

        public FavoritesController(IMongoDatabase db)
        {
            _favorites = db.GetCollection<Favorite>("Favorites");
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFavorites([FromServices] UserService users)
        {
            var user = await users.GetCurrentUser(HttpContext);
            var list = await _favorites.Find(f => f.UserId == user.Id).ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddFavoriteRequest req,
                                     [FromServices] UserService users,
                                     [FromServices] IMongoDatabase db)
        {
            var user = await users.GetCurrentUser(HttpContext);
            if (user == null)
                return Unauthorized();

            var articles = db.GetCollection<Article>("Articles");

            // Lấy bài viết thật từ DB
            var article = await articles.Find(a => a.Id == req.ArticleId).FirstOrDefaultAsync();
            if (article == null)
                return NotFound(new { message = "Article not found" });

            // Tạo bản sao lưu
            var fav = new Favorite
            {
                UserId = user.Id,
                ArticleId = req.ArticleId,

                Title = article.Title,
                Description = article.Description,
                ImageUrl = article.Thumbnail,
                Link = article.Link,
                Source = article.Source,
                PubDate = article.PubDate,

                SavedAt = DateTime.UtcNow
            };

            await _favorites.InsertOneAsync(fav);
            return Ok(fav);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(string id, [FromServices] UserService users)
        {
            var user = await users.GetCurrentUser(HttpContext);
            await _favorites.DeleteOneAsync(f => f.Id == id && f.UserId == user.Id);

            return Ok(new { message = "Removed" });
        }
    }
}
