namespace LookAt_Server.Models.DTO.Request
{
    public class UpdateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
