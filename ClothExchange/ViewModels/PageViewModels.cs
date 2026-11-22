using ClothExchange.Models;

namespace ClothExchange.ViewModels
{
    public class HomeViewModel
    {
        public List<Clothing> Latest { get; set; } = new List<Clothing>();
        public int TotalUsers { get; set; }
        public int TotalClothes { get; set; }
        public int TotalExchanges { get; set; }
    }

    // Used by the "Request exchange" page
    public class RequestViewModel
    {
        public Clothing Wanted { get; set; }
        public List<Clothing> MyClothes { get; set; } = new List<Clothing>();
    }

    // Used by the "My requests" page
    public class MyRequestsViewModel
    {
        public List<ExchangeRequest> Sent { get; set; } = new List<ExchangeRequest>();
        public List<ExchangeRequest> Received { get; set; } = new List<ExchangeRequest>();
    }
}
