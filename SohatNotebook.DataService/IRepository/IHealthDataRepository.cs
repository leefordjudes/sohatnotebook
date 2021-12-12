using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.DataService.IRepository;

public interface IHealthDataRepository : IGenericRepository<HealthData>
{
}
