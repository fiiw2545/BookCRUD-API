using System.ComponentModel.DataAnnotations;

namespace BookAPI.Models
{
    public class UpdateUserModel
    {
        [Required(ErrorMessage = "กรุณากรอก Username")]
        public string? Usr { get; set; }

        [Required(ErrorMessage = "กรุณากรอก Name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "กรุณาเลือก Level")]
        [RegularExpression(
            "^(USER|ADMIN)$",
            ErrorMessage = "Level ต้องเป็น USER หรือ ADMIN"
        )]
        public string? Level { get; set; }
    }
}