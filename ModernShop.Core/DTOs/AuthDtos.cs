using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.DTOs
{
    public class SendOtpRequestDto
    {
        public string PhoneNumber { get; set; } = null!;
    }

    public class VerifyOtpRequestDto
    {
        public string PhoneNumber { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsNewUser { get; set; }   // برای تشخیص ثبت‌نام تازه از ورود قبلی
    }
}
