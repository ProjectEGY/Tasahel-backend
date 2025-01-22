using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class CRUD<T> : ICRUD<T> where T :BaseModel
    {
        private readonly ApplicationDbContext _context;
        public CRUD(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ToggleDelete(long Id)
        {
            var obj = _context.Set<T>().FirstOrDefault(x => x.Id == Id);
            obj.IsDeleted = !obj.IsDeleted;
            obj.DeletedOn = DateTime.Now.ToUniversalTime();
            return await Save();
        }

        public async Task<bool> Update(long Id)
        {
            var obj = _context.Set<T>().FirstOrDefault(x => x.Id == Id);
            obj.IsModified = true;
            obj.ModifiedOn = DateTime.Now.ToUniversalTime();
            return await Save();
        }
        private async Task<bool> Save()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
