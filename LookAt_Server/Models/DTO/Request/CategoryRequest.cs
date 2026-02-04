using LookAt_Server.Models;

namespace LookAt_Server.Models.DTO.Request
{
    public class CategoryRequest
    {
        public string Name { get; set; }
        public List<RssSource> RssSources { get; set; } = new List<RssSource>();
    }
}
