namespace GyanSagarNew.Model
{
    public class ViewCartItemDto
    {
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImagePath { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal SaleDiscount { get; set; } 
        public bool IsOnSale { get; set; }
    }
}
