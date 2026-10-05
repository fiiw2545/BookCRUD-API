using System.ComponentModel.DataAnnotations;

namespace BookAPI.Models
{
    public class LoginModel
    {
        [Required]
        [MinLength(4)]
        public string? Usr { get; set; }

        [Required]
        [MinLength(6)]
        public string? Pwd { get; set; }
    }
}