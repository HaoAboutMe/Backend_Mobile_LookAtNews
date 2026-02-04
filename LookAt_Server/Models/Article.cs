using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LookAt_Server.Models
{
    public class Article
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Thumbnail { get; set; }
        public string Link { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }

        public DateTime PubDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
