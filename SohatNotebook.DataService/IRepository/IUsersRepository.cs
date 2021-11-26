using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.DataService.IRepository;

public interface IUsersRepository : IGenericRepository<User>
{
    Task<bool> UpdateUserProfile(User user);
    Task<User> GetByIdentityId(Guid identityId);
}
