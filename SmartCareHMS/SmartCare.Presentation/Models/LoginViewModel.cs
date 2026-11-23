using System.ComponentModel.DataAnnotations;

namespace SmartCare.Presentation.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "OTP code")]
        public string? OtpCode { get; set; }
    }
}
