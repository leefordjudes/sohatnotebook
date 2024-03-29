using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SohatNotebook.DataService.Data;
using SohatNotebook.DataService.IRepository;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.DataService.Repository;

public class RefreshTokensRepository : GenericRepository<RefreshToken>, IRefreshTokensRepository
{
    public RefreshTokensRepository(
        AppDbContext context,
        ILogger logger
    ) : base(context, logger)
    {
    }

    public override async Task<IEnumerable<RefreshToken>> All()
    {
        try
        {
            return await dbSet.Where(x => x.Status == 1)
                            .AsNoTracking()
                            .ToListAsync();
        }
        catch (Exception ex)
        {
            _loger.LogError(ex, "{Repo} All method has generated an error", typeof(RefreshTokensRepository));
            return new List<RefreshToken>();
        }
    }

    public async Task<RefreshToken> GetByRefreshToken(string refreshToken)
    {
        try
        {
            return await dbSet.Where(x => x.Token.ToLower() == refreshToken.ToLower())
                            .AsNoTracking()
                            .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _loger.LogError(ex, "{Repo} GetByRefreshToken method has generated an error", typeof(RefreshTokensRepository));
            return null;
        }
    }

    public async Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken)
    {
        try
        {
            var token = await dbSet.Where(x => x.Token.ToLower() == refreshToken.Token.ToLower())
                            .AsNoTracking()
                            .FirstOrDefaultAsync();
            if (token == null) return false;
            token.IsUsed = refreshToken.IsUsed;
            return true;
        }
        catch (Exception ex)
        {
            _loger.LogError(ex, "{Repo} MarkRefreshTokenAsUsed method has generated an error", typeof(RefreshTokensRepository));
            return false;
        }
    }
}
