﻿namespace GyanSagarNew.Model
{
    public class OrderDetailsDto
    {

        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public decimal Total { get; set; }

    }
}
