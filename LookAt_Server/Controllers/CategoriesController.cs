using LookAt_Server.Models;
using LookAt_Server.Models.DTO.Request;
using LookAt_Server.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LookAt_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IMongoCollection<Category> _categories;
        private readonly IMongoCollection<Article> _articles;

        public CategoriesController(IMongoDatabase db)
        {
            _categories = db.GetCollection<Category>("Categories");
            _articles = db.GetCollection<Article>("Articles");
        }

        // GET all categories
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var list = await _categories.Find(_ => true).ToListAsync();

            return Ok(list.Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                RssSources = c.RssSources
            }));
        }

        // GET by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null)
                return NotFound(new { message = "Category not found" });

            return Ok(new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                RssSources = category.RssSources
            });
        }

        // POST create new category
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Name là bắt buộc." });

            if (req.RssSources == null || req.RssSources.Count == 0)
                return BadRequest(new { message = "Phải có ít nhất một RSS source." });

            // Validate từng RSS source
            foreach (var source in req.RssSources)
            {
                if (string.IsNullOrWhiteSpace(source.Name) || string.IsNullOrWhiteSpace(source.Url))
                    return BadRequest(new { message = "Mỗi RSS source phải có Name và Url." });
            }

            var category = new Category
            {
                Name = req.Name,
                RssSources = req.RssSources
            };

            await _categories.InsertOneAsync(category);

            return Ok(new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                RssSources = category.RssSources
            });
        }

        // PUT update category
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CategoryRequest req)
        {
            var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null)
                return NotFound(new { message = "Category not found" });

            var oldName = category.Name;

            var update = Builders<Category>.Update
                .Set(c => c.Name, req.Name)
                .Set(c => c.RssSources, req.RssSources);

            await _categories.UpdateOneAsync(c => c.Id == id, update);

            // Cập nhật tên category trong tất cả các articles liên quan
            if (oldName != req.Name)
            {
                var articleUpdate = Builders<Article>.Update
                    .Set(a => a.Category, req.Name);
                
                await _articles.UpdateManyAsync(a => a.Category == oldName, articleUpdate);
            }

            return Ok(new { message = "Updated successfully" });
        }

        // DELETE category
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _categories.DeleteOneAsync(c => c.Id == id);

            if (result.DeletedCount == 0)
                return NotFound(new { message = "Category not found" });

            return Ok(new { message = "Deleted successfully" });
        }
    }
}
