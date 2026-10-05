using System.ComponentModel.DataAnnotations;
namespace BookAPI.Models
{
    public class CreateUserModel
    {
        [Required(ErrorMessage = "กรุณากรอก Username")]
        public string? Usr { get; set; }
        [Required(ErrorMessage = "กรุณากรอก Password")]
        [MinLength(6, ErrorMessage = "Password ต้องมีอย่างน้อย 6 ตัวอักษร")]
        public string? Pwd { get; set; }
        [Required(ErrorMessage = "กรุณากรอก Name")]
        public string? Name { get; set; }
        [Required(ErrorMessage = "กรุณาเลือก Level")]
        public string? Level { get; set; }
    }
}
