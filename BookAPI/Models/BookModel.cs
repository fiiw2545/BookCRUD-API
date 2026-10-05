using System.ComponentModel.DataAnnotations;
namespace BookAPI.Models
{
    public class BookModel
    {
        public int Id { get; set; }

        [Required]
        public string? Isbn { get; set; }

        [Required]
        public string? Name { get; set; }

        [Range(1, int.MaxValue)]
        public int Price { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }
}
