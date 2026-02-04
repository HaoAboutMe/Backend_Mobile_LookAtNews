namespace LookAt_Server.Models.DTO.Request
{
    public class VerifyOTPRequest
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
    }
}
