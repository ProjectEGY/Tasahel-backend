using PostexS.Models.Domain;

namespace PostexS.Models.Dtos
{
    public class BranchDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Whatsapp { get; set; }

        public BranchDto() { }

        public BranchDto(Branch branch)
        {
            if (branch == null) return;
            Id = branch.Id;
            Name = branch.Name;
            Address = branch.Address;
            PhoneNumber = branch.PhoneNumber;
            Whatsapp = branch.Whatsapp;
        }
    }
}
