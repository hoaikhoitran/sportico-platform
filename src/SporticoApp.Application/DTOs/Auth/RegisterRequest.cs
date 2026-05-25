using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Email format is invalid")]
        [MaxLength(320, ErrorMessage = "Email is too long")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100, ErrorMessage = "Password is too long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
        [MaxLength(150, ErrorMessage = "Full name is too long")]
        public string FullName { get; set; } = string.Empty;
    }
}
