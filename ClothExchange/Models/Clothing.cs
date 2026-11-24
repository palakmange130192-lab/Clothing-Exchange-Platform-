using System.ComponentModel.DataAnnotations;

namespace ClothExchange.Models;

public class Clothing : BaseEntity   // inherits Id and CreatedAt
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100)]
    public string Title { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a size")]
    public string Size { get; set; } = "";

    [Required(ErrorMessage = "Please choose a category")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Please choose a condition")]
    public string Condition { get; set; } = "";

    [Display(Name = "Image URL (optional)")]
    public string? ImageUrl { get; set; }

    public string Status { get; set; } = "Available";   // Available / Exchanged
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = "";

    // Polymorphism: Clothing gives its own version of GetSummary()
    public override string GetSummary()
    {
        return Title + " (" + Category + ", size " + Size + ", " + Condition + ")";
    }

    // Dropdown options
    public static readonly string[] Categories = { "Men", "Women", "Kids", "Traditional", "Winter", "Accessories" };
    public static readonly string[] Conditions = { "New", "Like New", "Good", "Fair" };
    public static readonly string[] Sizes = { "XS", "S", "M", "L", "XL", "XXL", "Free Size" };
}
