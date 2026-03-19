using System;
using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.ViewModels
{
    public class ReportVM
    {
        [Required]
        public DateTime fromDate { get; set; }
        [Required]
        public DateTime toDate { get; set; }
    }
}
