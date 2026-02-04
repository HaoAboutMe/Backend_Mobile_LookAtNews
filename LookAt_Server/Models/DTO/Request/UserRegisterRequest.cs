namespace LookAt_Server.Models.DTO.Request
{
    public class UserRegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
