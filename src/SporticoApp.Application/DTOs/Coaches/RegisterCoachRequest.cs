using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.DTOs.Coaches
{
    public class RegisterCoachRequest
    {
        [Required(ErrorMessage = "Headline is required")]
        [MinLength(5, ErrorMessage = "Headline must be at least 5 characters")]
        [MaxLength(255, ErrorMessage = "Headline is too long")]
        public string Headline { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Bio is too long")]
        public string? Bio { get; set; }

        [Range(0, 60, ErrorMessage = "Experience years must be between 0 and 60")]
        public int ExperienceYears { get; set; }

        public List<int> SportIds { get; set; } = new();
    }
}
