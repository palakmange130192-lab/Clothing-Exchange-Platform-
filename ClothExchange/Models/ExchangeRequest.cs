namespace ClothExchange.Models;

public class ExchangeRequest : BaseEntity   // inherits Id and CreatedAt
{
    public int RequestedItemId { get; set; }
    public int OfferedItemId { get; set; }
    public int RequesterId { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Pending";   // Pending / Accepted / Rejected

    // Extra fields filled by JOINs (only for display)
    public string RequestedTitle { get; set; } = "";
    public string OfferedTitle { get; set; } = "";
    public string RequesterName { get; set; } = "";
    public string OwnerName { get; set; } = "";

    // Polymorphism: ExchangeRequest gives its own version of GetSummary()
    public override string GetSummary()
    {
        return RequesterName + " offers \"" + OfferedTitle + "\" for \"" + RequestedTitle + "\"";
    }
}

// Data needed by the "Request Exchange" page
public class ExchangeRequestViewModel
{
    public Clothing Target { get; set; } = new Clothing();
    public List<Clothing> MyItems { get; set; } = new List<Clothing>();
}
