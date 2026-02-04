namespace LookAt_Server.Models.DTO.Response
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsGoogleAccount { get; set; }
        public List<string> FavoriteCategories { get; set; }
        public List<string> SavedArticles { get; set; }
    }
}
