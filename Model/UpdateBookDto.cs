namespace GyanSagarNew.Model
{
    public class UpdateBookDto
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string ISBN { get; set; }
        public decimal Price { get; set; }
        public string Language { get; set; }
        public string Format { get; set; }
        public string Publisher { get; set; }
        public int StockAvailability { get; set; }
        public bool IsOnSale { get; set; }
        public int SaleDiscount { get; set; }
        public IFormFile? BookImage { get; set; }
    }


}
