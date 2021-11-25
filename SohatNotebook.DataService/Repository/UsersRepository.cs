using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SohatNotebook.DataService.Data;
using SohatNotebook.DataService.IRepository;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.DataService.Repository
{
    public class UsersRepository : GenericRepository<User>, IUsersRepository
    {
        public UsersRepository(AppDbContext context, ILogger logger) : base (context, logger)
        {
        }

        public override async Task<IEnumerable<User>> All()
        {
            try
            {
                return await dbSet.Where(x => x.Status == 1).AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                _loger.LogError(ex, "{Repo} All method has generated an error", typeof(UsersRepository));
                return new List<User>();
            }
        }

        public async Task<bool> UpdateUserProfile(User user)
        {
            try
            {
                var existingUser = await dbSet.Where(x => x.Status == 1 && x.Id == user.Id).FirstOrDefaultAsync();
                if(existingUser == null) return false;

                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.MobileNumber = user.MobileNumber;
                existingUser.Phone = user.Phone;
                existingUser.Sex = user.Sex;
                existingUser.Address = user.Address;
                existingUser.UpdateDate = DateTime.UtcNow;

                return true;
            }
            catch (Exception ex)
            {
                _loger.LogError(ex, "{Repo} UpdateUserProfile method has generated an error", typeof(UsersRepository));
                return false;
            }
        }

        public async Task<User> GetByIdentityId(Guid identityId)
        {
            try
            {
                return await dbSet.Where(x => x.Status == 1 && x.IdentityId == identityId)
                                .AsNoTracking()
                                .FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _loger.LogError(ex, "{Repo} GetByIdentityId method has generated an error", typeof(UsersRepository));
                return null;
            }
        }
    }
}