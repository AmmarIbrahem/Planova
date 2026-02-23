using Planova.Application.Common.Interfaces;

namespace Planova.Infrastructure.Persistence
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly AppDbContext _context;

		public UnitOfWork(AppDbContext context)
		{
			_context = context;
		}

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
			=> _context.SaveChangesAsync(cancellationToken);
	}
}
