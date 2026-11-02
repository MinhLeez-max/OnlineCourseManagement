using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;
    }
}