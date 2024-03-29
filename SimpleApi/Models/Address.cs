﻿using System.ComponentModel.DataAnnotations;

namespace SimpleApi.Models
{
    public class Address
    {
        [Key]
        public int Id {  get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
