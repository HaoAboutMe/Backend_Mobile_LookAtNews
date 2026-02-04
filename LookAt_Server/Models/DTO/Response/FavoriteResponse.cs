namespace LookAt_Server.Models.DTO.Response
{
    public class FavoriteResponse
    {
        public string Id { get; set; }
        public string ArticleId { get; set; }
        public string UserId { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public DateTime PubDate { get; set; }

        public DateTime SavedAt { get; set; }
    }
}
