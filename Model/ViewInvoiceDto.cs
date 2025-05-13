namespace GyanSagarNew.Model
{
    public class ViewInvoiceDto
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserAddress { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<BookDInvoice> Books { get; set; } = new();
    }
}
