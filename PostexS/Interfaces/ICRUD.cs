using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface ICRUD<T> where T: BaseModel
    {
        Task<bool> ToggleDelete(long Id);
        Task<bool> Update(long Id);
    }
}
