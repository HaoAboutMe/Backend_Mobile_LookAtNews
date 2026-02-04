using Google.Apis.Auth;
using LookAt_Server.Models;
using LookAt_Server.Models.DTO.Request;
using LookAt_Server.Models.DTO.Response;
using LookAt_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LookAt_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private readonly IMongoCollection<User> _usersCollection;

        public AuthController(UserService userService, IConfiguration config, EmailService emailService, IMongoDatabase database)
        {
            _userService = userService;
            _config = config;
            _emailService = emailService;
            _usersCollection = database.GetCollection<User>("Users");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (await _userService.GetByEmailAsync(request.Email) != null)
                return BadRequest(new { message = "Email đã tồn tại" });

            // Tạo mã OTP 6 chữ số
            var otpCode = GenerateOTP();

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                AvatarUrl = request.AvatarUrl,
                IsVerified = false,
                VerifyToken = otpCode, // Lưu OTP vào VerifyToken
                VerifyTokenExpiry = DateTime.UtcNow.AddMinutes(10) // OTP có hiệu lực 10 phút
            };

            await _userService.CreateUserAsync(user, request.Password);

            // Tạo email body với OTP
            string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,sans-serif;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4;padding:20px 0;'>
                        <tr>
                            <td align='center'>
                                <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:10px;box-shadow:0 4px 6px rgba(0,0,0,0.1);overflow:hidden;'>
                    
                                    <tr>
                                        <td style='background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);padding:40px 30px;text-align:center;'>
                                            <h1 style='color:#ffffff;margin:0;font-size:28px;font-weight:bold;'>
                                                🎉 Chào mừng đến với LookAt!
                                            </h1>
                                        </td>
                                    </tr>
                    
                                    <tr>
                                        <td style='padding:40px 30px;'>
                                            <h2 style='color:#333333;margin:0 0 20px 0;font-size:24px;'>
                                                Xin chào <span style='color:#667eea;'>{user.Username}</span>,
                                            </h2>
                            
                                            <p style='color:#666666;font-size:16px;line-height:1.6;margin:0 0 20px 0;'>
                                                Cảm ơn bạn đã đăng ký tài khoản! Để hoàn tất quá trình đăng ký, 
                                                vui lòng sử dụng mã xác nhận bên dưới:
                                            </p>
                            
                                            <!-- OTP Code Box -->
                                            <table width='100%' cellpadding='0' cellspacing='0' style='margin:30px 0;'>
                                                <tr>
                                                    <td align='center'>
                                                        <div style='display:inline-block;
                                                                    background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);
                                                                    padding:20px 40px;
                                                                    border-radius:10px;
                                                                    box-shadow:0 4px 15px rgba(102,126,234,0.4);'>
                                                            <h1 style='color:#ffffff;
                                                                       margin:0;
                                                                       font-size:36px;
                                                                       font-weight:bold;
                                                                       letter-spacing:8px;
                                                                       font-family:monospace;'>
                                                                {otpCode}
                                                            </h1>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>

                                            <div style='background-color:#f8f9fa;border-left:4px solid #667eea;padding:15px 20px;margin:20px 0;border-radius:5px;'>
                                                <p style='color:#555555;margin:0;font-size:14px;'>
                                                    <strong>⏰ Lưu ý:</strong> Mã OTP này sẽ hết hiệu lực sau <strong>10 phút</strong>.
                                                </p>
                                            </div>

                                            <p style='color:#666666;font-size:14px;line-height:1.6;margin:20px 0 0 0;'>
                                                Vui lòng không chia sẻ mã này với bất kỳ ai.
                                            </p>
                                        </td>
                                    </tr>
                    
                                    <tr>
                                        <td style='background-color:#f8f9fa;padding:30px;text-align:center;border-top:1px solid #eeeeee;'>
                                            <p style='color:#999999;font-size:14px;margin:0 0 10px 0;'>
                                                Bạn nhận được email này vì đã đăng ký tài khoản trên LookAt.
                                            </p>
                                            <p style='color:#999999;font-size:13px;margin:0;'>
                                                Nếu bạn không thực hiện hành động này, vui lòng bỏ qua email này.
                                            </p>
                            
                                            <div style='margin-top:20px;'>
                                                <p style='color:#666666;font-size:12px;margin:0 0 10px 0;'>
                                                    © 2025 LookAtNews. All rights reserved.
                                                </p>
                                            </div>
                                        </td>
                                    </tr>
                    
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
             ";

            await _emailService.SendEmailAsync(user.Email, "Mã xác nhận tài khoản LookAt", body);

            return Ok(new
            {
                message = "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác nhận.",
                email = user.Email // Trả về email để Flutter biết
            });
        }


        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            var user = await _usersCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user == null)
                return BadRequest(new { message = "Email không tồn tại." });

            if (user.IsVerified)
                return Ok(new { message = "Tài khoản đã được xác nhận trước đó." });

            // Kiểm tra OTP có hợp lệ không
            if (user.VerifyToken != request.OtpCode)
                return BadRequest(new { message = "Mã OTP không đúng." });

            // Kiểm tra OTP có hết hạn không
            if (user.VerifyTokenExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới." });

            // Xác nhận tài khoản
            user.IsVerified = true;
            user.VerifyToken = null;
            user.VerifyTokenExpiry = null;

            await _usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

            return Ok(new { message = "Xác nhận tài khoản thành công! Bạn có thể đăng nhập ngay." });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOTP([FromBody] ResendOTPRequest request)
        {
            var user = await _usersCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user == null)
                return BadRequest(new { message = "Email không tồn tại." });

            if (user.IsVerified)
                return BadRequest(new { message = "Tài khoản đã được xác nhận." });

            // Tạo OTP mới
            var otpCode = GenerateOTP();
            user.VerifyToken = otpCode;
            user.VerifyTokenExpiry = DateTime.UtcNow.AddMinutes(10);

            await _usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

            // Gửi email với OTP mới
            string body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                </head>
                <body style='font-family:Arial,sans-serif;'>
                    <div style='max-width:600px;margin:0 auto;padding:20px;'>
                        <h2>Mã OTP mới của bạn</h2>
                        <p>Xin chào {user.Username},</p>
                        <p>Đây là mã OTP mới của bạn:</p>
                        <div style='background:#667eea;color:white;padding:15px;text-align:center;font-size:24px;letter-spacing:5px;font-weight:bold;margin:20px 0;'>
                            {otpCode}
                        </div>
                        <p style='color:#666;'>Mã này có hiệu lực trong 10 phút.</p>
                    </div>
                </body>
                </html>
            ";

            await _emailService.SendEmailAsync(user.Email, "Mã OTP mới - LookAt", body);

            return Ok(new { message = "Đã gửi mã OTP mới đến email của bạn." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Sai email hoặc mật khẩu" });

            if (user.IsGoogleAccount)
                return BadRequest(new { message = "Tài khoản này đăng nhập bằng Google, không dùng mật khẩu!" });

            if (!user.IsVerified)
                return BadRequest(new { message = "Tài khoản chưa được xác minh. Vui lòng kiểm tra email để xác nhận." });

            if (!_userService.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Sai email hoặc mật khẩu" });

            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            // Cập nhật refresh token trực tiếp vào database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
            user.UpdatedAt = DateTime.UtcNow;

            // Sử dụng MongoDB collection để update trực tiếp
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, refreshToken)
                .Set(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(30))
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, update);

            return Ok(new
            {
                accessToken,
                refreshToken,
                user
            });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest(new { message = "Thiếu Google ID Token!" });

            var token = await VerifyGoogleTokenAsync(request.IdToken);

            if (token == null)
                return Unauthorized(new { message = "Google ID Token không hợp lệ!" });

            var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            var picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (email == null)
                return Unauthorized(new { message = "Không lấy được email từ token!" });

            var user = await _userService.GetByEmailAsync(email);

            if (user != null && user.IsGoogleAccount == false)
            {
                return BadRequest(new { message = "Email này đã tồn tại với tài khoản không phải Google" });
            }

            if (user == null)
            {
                user = new User
                {
                    Username = name ?? email,
                    Email = email,
                    AvatarUrl = picture,
                    IsVerified = true,
                    IsGoogleAccount = true,
                    PasswordHash = null
                };

                await _usersCollection.InsertOneAsync(user);
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            // Cập nhật refresh token trực tiếp vào database
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, refreshToken)
                .Set(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(30))
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, update);

            return Ok(new { accessToken, refreshToken, user = MapToUserResponse(user) });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] Models.DTO.Request.ForgotPasswordRequest request, [FromServices] EmailService emailService)
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
                return BadRequest(new { message = "Email không tồn tại." });

            if (user.IsGoogleAccount)
                return BadRequest(new { message = "Tài khoản Google không thể đặt lại mật khẩu." });

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _userService.UpdateUserAsync(user);

            var resetLink = $"{_config["Frontend:BaseUrl"]}/reset-password?token={user.ResetToken}";
            var subject = "Đặt lại mật khẩu LookAt";
            var body = $@"
                <h3>Xin chào {user.Username},</h3>
                <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
                <p>Nhấn vào link bên dưới để đặt lại mật khẩu (hiệu lực 1 giờ):</p>
                <a href='{resetLink}'>Đặt lại mật khẩu</a>";

            await emailService.SendEmailAsync(user.Email, subject, body);
            return Ok(new { message = "Email đặt lại mật khẩu đã được gửi." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] Models.DTO.Request.ResetPasswordRequest request)
        {
            var user = await _userService.GetByResetTokenAsync(request.Token);
            if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

            if (user.IsGoogleAccount)
                return BadRequest(new { message = "Tài khoản Google không hỗ trợ mật khẩu." });

            user.PasswordHash = _userService.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _userService.UpdateUserAsync(user);

            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var user = await _userService.GetByRefreshTokenAsync(request.RefreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized(new { message = "Refresh token invalid or expired" });

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = Guid.NewGuid().ToString();

            // Cập nhật refresh token trực tiếp vào database
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, newRefreshToken)
                .Set(u => u.RefreshTokenExpiry, DateTime.UtcNow.AddDays(30))
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, update);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userService.GetByEmailAsync(email);

            if (user == null)
                return Unauthorized();

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userService.UpdateUserAsync(user);

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromServices] EmailService emailService)
        {
            await emailService.SendEmailAsync(
                "lookatwidget@gmail.com",
                "Test Gmail SMTP",
                "<h3>Mail gửi thành công!</h3><p>Nếu bạn thấy mail này, cấu hình SMTP đã hoạt động.</p>"
            );

            return Ok("Email sent successfully!");
        }

        // Helper method: Tạo mã OTP 6 chữ số
        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("username", user.Username),
                new Claim("userId", user.Id),
                new Claim("isGoogleAccount", user.IsGoogleAccount.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"✅ Token generated for user: {user.Email}, UserId: {user.Id}");

            return tokenString;
        }

        private async Task<JwtSecurityToken?> VerifyGoogleTokenAsync(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever()
            );

            var config = await configManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://accounts.google.com",
                ValidateIssuer = true,
                ValidAudience = "706618149089-4tnjpt3kgdoetkrf80m89kijq8cn67le.apps.googleusercontent.com",
                ValidateAudience = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true
            };

            try
            {
                handler.ValidateToken(idToken, validationParameters, out var validatedToken);
                return (JwtSecurityToken)validatedToken;
            }
            catch
            {
                return null;
            }
        }

        private UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                IsVerified = user.IsVerified,
                IsGoogleAccount = user.IsGoogleAccount
            };
        }
    }
}