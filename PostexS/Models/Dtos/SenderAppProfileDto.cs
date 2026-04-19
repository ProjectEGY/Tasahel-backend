using PostexS.Models.Domain;

namespace PostexS.Models.Dtos
{
    public class SenderAppProfileDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string WhatsappPhone { get; set; }
        public string SecondaryPhone { get; set; }
        public string Address { get; set; }
        public double Wallet { get; set; }
        public BranchDto Branch { get; set; }
        public bool HideSenderName { get; set; }
        public bool HideSenderPhone { get; set; }
        public bool HideSenderCode { get; set; }
        public string Token { get; set; }

        public SenderAppProfileDto() { }

        public SenderAppProfileDto(ApplicationUser user, Branch branch = null)
        {
            Id = user.Id;
            Name = user.Name;
            Phone = user.PhoneNumber;
            Email = user.Email;
            WhatsappPhone = user.WhatsappPhone;
            SecondaryPhone = user.SecondaryPhone;
            Address = user.Address;
            Wallet = user.Wallet;
            Branch = new BranchDto(branch);
            HideSenderName = user.HideSenderName;
            HideSenderPhone = user.HideSenderPhone;
            HideSenderCode = user.HideSenderCode;
        }
    }
}
