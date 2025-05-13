namespace GyanSagarNew.Model
{
    public class PromoCode
    {
        public int PromoCodeID { get; set; }
        public string Code { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

}
