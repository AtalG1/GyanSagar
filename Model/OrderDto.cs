namespace GyanSagarNew.Model
{
    public class OrderDto
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; } 
        public int? PromoCodeID { get; set; }
        public string VerificationCode { get; set; }

        public string Status { get; set; }


        public List<OrderDetailsDto> Books { get; set; } = new();
    }


}
