using SnowflakeItemMaster.Application.Contracts.Persistence;

namespace SnowflakeItemMaster.Infrastructure.Repositories
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly RepositoryContext _repositoryContext;
        private readonly Lazy<IItemMasterSourceLogRepository> _itemMasterSourceLogRepository;

        public RepositoryManager(RepositoryContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
            _itemMasterSourceLogRepository = new Lazy<IItemMasterSourceLogRepository>(() => new ItemMasterSourceLogRepository(repositoryContext));
        }

        public IItemMasterSourceLogRepository ItemMasterSourceLog => _itemMasterSourceLogRepository.Value;

        public async Task SaveAsync() => await _repositoryContext.SaveChangesAsync();
    }
}