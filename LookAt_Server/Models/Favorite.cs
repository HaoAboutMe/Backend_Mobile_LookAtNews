using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LookAt_Server.Models
{
    public class Favorite
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string ArticleId { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public DateTime PubDate { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
