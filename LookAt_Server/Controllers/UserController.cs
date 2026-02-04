using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LookAt_Server.Models;
using LookAt_Server.Services;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LookAt_Server.Models.DTO.Request;

namespace LookAt_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu có token mới truy cập được
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        // Lấy thông tin người dùng hiện tại
        [HttpGet("me")]
        public async Task<IActionResult> GetMyInfo()
        {
            // Lấy userId từ token - hỗ trợ nhiều claim type
            var userId = User.Claims.FirstOrDefault(c => 
                c.Type == "sub" || 
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("🔴 Không tìm thấy userId trong claims");
                Console.WriteLine($"Available claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                return Unauthorized(new { message = "Không tìm thấy userId trong token" });
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(user);
        }

        // 📌 Cập nhật thông tin cá nhân (username, avatar)
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => 
                c.Type == "sub" || 
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không tìm thấy userId trong token" });

            var updated = await _userService.UpdateUserProfileAsync(userId, request);
            return Ok(new { message = "Cập nhật thành công", user = updated });
        }

        // 📌 Xem thông tin người khác qua id (ví dụ xem profile bạn bè)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Lấy email từ token (Token chứa các thông tin như sub, email, name, avatar)
            // Tìm kiếm email trong claims
            // Sau đó mới tìm user qua email trong database
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userService.GetByEmailAsync(email!);
            if (user == null)
                return Unauthorized();

            if (user.IsGoogleAccount)
                return BadRequest(new { message = "Tài khoản Google không thể đổi mật khẩu." });

            if (!_userService.VerifyPassword(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu cũ không đúng." });

            user.PasswordHash = _userService.HashPassword(request.NewPassword);
            await _userService.UpdateUserAsync(user);

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
    }
}
