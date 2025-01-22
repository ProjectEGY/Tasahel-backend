using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class SubmitLoginDto
    {
        [Required]
        public string Phone { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
