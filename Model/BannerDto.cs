namespace GyanSagarNew.Model
{
    public class BannerDto
    {
        public int BannerID { get; set; }
        public string Message { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
