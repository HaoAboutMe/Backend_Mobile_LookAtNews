using LookAt_Server.Models;
using LookAt_Server.Models.DTO.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LookAt_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly IMongoCollection<Article> _articles;

        public ArticlesController(IMongoDatabase db)
        {
            _articles = db.GetCollection<Article>("Articles");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _articles.Find(_ => true).SortByDescending(a => a.PubDate).ToListAsync());


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
            => Ok(await _articles.Find(a => a.Id == id).FirstOrDefaultAsync());

        [HttpGet("category/{name}")]
        public async Task<IActionResult> GetByCategory(string name)
            => Ok(await _articles.Find(a => a.Category == name).ToListAsync());
    }
}
