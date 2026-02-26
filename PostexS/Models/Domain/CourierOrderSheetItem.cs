using System.ComponentModel.DataAnnotations.Schema;

namespace PostexS.Models.Domain
{
    public class CourierOrderSheetItem : BaseModel
    {
        public long SheetId { get; set; }
        [ForeignKey("SheetId")]
        public virtual CourierOrderSheet Sheet { get; set; }

        public long OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}
