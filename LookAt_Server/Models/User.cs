using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LookAt_Server.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        [BsonElement("passwordHash")]
        public string? PasswordHash { get; set; } = string.Empty;

        [BsonElement("avatarUrl")]
        public string AvatarUrl { get; set; } = string.Empty;

        [BsonElement("isGoogleAccount")]
        public bool IsGoogleAccount { get; set; } = false;

        [BsonElement("isVerified")]
        public bool IsVerified { get; set; } = false;

        [BsonElement("verifyToken")]
        public string? VerifyToken { get; set; }

        [BsonElement("verifyTokenExpiry")]
        public DateTime? VerifyTokenExpiry { get; set; }

        //Reset lại token khi quên mật khẩu
        [BsonElement("resetToken")]
        public string? ResetToken { get; set; }

        [BsonElement("resetTokenExpiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        //Refresh token khi đăng nhập
        [BsonElement("refreshToken")]
        public string? RefreshToken { get; set; }

        [BsonElement("refreshTokenExpiry")]
        public DateTime? RefreshTokenExpiry { get; set; }

        [BsonElement("favoriteCategories")]
        public List<string> FavoriteCategories { get; set; } = new();

        [BsonElement("savedArticles")]
        public List<string> SavedArticles { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
