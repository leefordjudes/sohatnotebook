using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SohatNotebook.DataService.IRepository;

namespace SohatNotebook.DataService.IConfiguration;

public interface IUnitOfWork
{
    IUsersRepository Users { get; }
    IRefreshTokensRepository RefreshTokens { get; }
    IHealthDataRepository HealthData {get;}

    Task CompleteAsync();
}
