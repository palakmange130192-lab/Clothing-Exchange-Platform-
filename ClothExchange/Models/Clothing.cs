using System.ComponentModel.DataAnnotations;

namespace ClothExchange.Models
{
    public class Clothing : BaseEntity
    {
        public int OwnerId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be 3 to 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be 10 to 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Please select a size")]
        public string Size { get; set; }

        [Required(ErrorMessage = "Please select the condition")]
        public string Condition { get; set; }

        // path of the uploaded photo, e.g. /uploads/abc123.jpg
        [Display(Name = "Photo")]
        public string ImageUrl { get; set; }

        public string Status { get; set; }       // Available / Exchanged

        // These two come from the JOIN with the Users table (not stored in Clothes)
        public string OwnerName { get; set; }
        public string OwnerCity { get; set; }

        // Constructor: every new clothing starts as Available
        public Clothing()
        {
            Status = "Available";
        }
    }
}
