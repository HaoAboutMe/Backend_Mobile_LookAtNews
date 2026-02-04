using BCrypt.Net;
using LookAt_Server.Config;
using LookAt_Server.Controllers;
using LookAt_Server.Models;
using LookAt_Server.Models.DTO.Request;
using LookAt_Server.Models.DTO.Response;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LookAt_Server.Services
{
    public class UserService
    {
        //Singleton cho phép tái sử dụng kết nối MongoDB
        private readonly IMongoCollection<User> _users;

        //Constructor nhận cấu hình MongoDB từ IOptions
        public UserService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<User>("Users");

        }

        //Phương thức lấy người dùng theo Id
        public async Task<User?> GetByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        //Phương thức lấy người dùng theo email
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        // Phương thức lấy người dùng theo tên đăng nhập
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        // Phương thức tạo người dùng mới với mã hóa mật khẩu
        public async Task<User> CreateUserAsync(User user, string password)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await _users.InsertOneAsync(user);
            return user;
        }

        // Phương thức xác minh mật khẩu
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        // Phương thức cập nhật hồ sơ người dùng (username, avatar)
        public async Task<User?> UpdateUserProfileAsync(string id, UpdateUserRequest request)
        {
            var update = Builders<User>.Update
                .Set(u => u.Username, request.Username)
                .Set(u => u.AvatarUrl, request.AvatarUrl);

            await _users.UpdateOneAsync(u => u.Id == id, update);
            return await GetByIdAsync(id);
        }

        // Phương thức băm mật khẩu
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Phương thức lấy người dùng theo token đặt lại mật khẩu
        public async Task<User?> GetByResetTokenAsync(string token) =>
            await _users.Find(u => u.ResetToken == token).FirstOrDefaultAsync();

        // Phương thức cập nhật người dùng
        public async Task UpdateUserAsync(User user) =>
            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);

        // Phương thức cập nhật refresh token và thời hạn của nó
        public async Task UpdateRefreshToken(string userId, string token, DateTime expiry)
        {
            //filter là điều kiện để tìm người dùng cần cập nhật trong cơ sở dữ liệu
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, token)
                .Set(u => u.RefreshTokenExpiry, expiry);

            await _users.UpdateOneAsync(filter, update);
        }

        // Phương thức lấy người dùng theo refresh token
        public async Task<User?> GetByRefreshTokenAsync(string token)
        {
            return await _users.Find(u => u.RefreshToken == token).FirstOrDefaultAsync();
        }
        public async Task<User?> GetCurrentUser(HttpContext httpContext)
        {
            var email = httpContext.User.FindFirstValue(ClaimTypes.Email)
                        ?? httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Email);

            if (email == null)
                return null;

            return await GetByEmailAsync(email);
        }

    }
}
