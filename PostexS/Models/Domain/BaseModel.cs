using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class BaseModel
    {
        public BaseModel()
        {
            CreateOn = DateTime.Now.ToUniversalTime();
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime CreateOn { get; set; }
        public DateTime? DeletedOn { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsModified { get; set; }
       
    }
}
