﻿namespace GyanSagarNew.Model
{
    public class ReviewDto
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int Rating { get; set; }
        public string Review { get; set; }

        public string Username { get; set; }
    }

}
