namespace GyanSagarNew.Model
{
    public class InvoiceDto
    {
        // public int InvoiceID { get; set; }
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string VerificationCode { get; set; }
        public string Status { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }

        public List<OrderDetailsDto> Books { get; set; } = new();


    }

}
