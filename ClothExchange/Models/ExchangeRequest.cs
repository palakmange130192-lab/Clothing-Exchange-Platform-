namespace ClothExchange.Models
{
    public class ExchangeRequest : BaseEntity
    {
        public int RequesterId { get; set; }
        public int RequestedClothId { get; set; }   // the cloth the requester WANTS
        public int OfferedClothId { get; set; }     // the cloth the requester GIVES
        public string Message { get; set; }
        public string Status { get; set; }          // Pending / Accepted / Rejected

        // Extra fields filled by JOINs
        public string RequesterName { get; set; }
        public string OwnerName { get; set; }
        public string RequestedTitle { get; set; }
        public string OfferedTitle { get; set; }

        public ExchangeRequest()
        {
            Status = "Pending";
        }
    }
}
