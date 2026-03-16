using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Dtos
{
    public class SenderAppRegisterDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string Email { get; set; }
        public string WhatsappPhone { get; set; }
        public string Address { get; set; }

        [Required]
        public long BranchId { get; set; }
    }
}
