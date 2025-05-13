namespace GyanSagarNew.Model
{
    public class BookDInvoice
    {
        public int BookID { get; set; }
        public string BookTitle { get; set; }
        public string BookAuthor { get; set; }
        public decimal BookPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
