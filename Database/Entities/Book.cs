namespace GyanSagarNew.Model
{
    public class Book
    {
        public int BookID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public bool StockAvailability { get; set; }
        public double Rating { get; set; }
        public double TotalRating { get; set; }
        public int TotalPurchase { get; set; }
        public bool IsOnSale { get; set; }
        public decimal SaleDiscount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? BookImageUrl { get; set; }  // URL or path to image
    }
}
