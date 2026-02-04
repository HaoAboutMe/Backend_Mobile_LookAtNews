using LookAt_Server.Models;

namespace LookAt_Server.Models.DTO.Response
{
    public class CategoryResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<RssSource> RssSources { get; set; } = new List<RssSource>();
    }
}
