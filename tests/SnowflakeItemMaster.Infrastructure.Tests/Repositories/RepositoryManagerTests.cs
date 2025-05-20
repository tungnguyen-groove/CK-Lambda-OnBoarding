using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using SnowflakeItemMaster.Infrastructure.Repositories;

namespace SnowflakeItemMaster.Infrastructure.Tests.Repositories
{
    namespace SnowflakeItemMaster.Infrastructure.Tests.Repositories
    {
        public class RepositoryManagerTests
        {
            private readonly Mock<RepositoryContext> _mockContext;
            private readonly RepositoryManager _repositoryManager;

            public RepositoryManagerTests()
            {
                _mockContext = new Mock<RepositoryContext>(new DbContextOptions<RepositoryContext>());
                _repositoryManager = new RepositoryManager(_mockContext.Object);
            }

            [Fact]
            public void ItemMasterSourceLog_AccessingProperty_InitializesOnlyOnce()
            {
                // Act
                var repository1 = _repositoryManager.ItemMasterSourceLog;
                var repository2 = _repositoryManager.ItemMasterSourceLog;

                // Assert
                repository1.Should().NotBeNull();
                repository1.Should().BeSameAs(repository2);
            }

            [Fact]
            public void ItemMasterSourceLog_AccessingProperty_ReturnsCorrectType()
            {
                // Act
                var repository = _repositoryManager.ItemMasterSourceLog;

                // Assert
                repository.Should().NotBeNull();
                repository.Should().BeAssignableTo<IItemMasterSourceLogRepository>();
            }

            [Fact]
            public async Task SaveAsync_CallsContextSaveChanges()
            {
                // Arrange
                _mockContext.Setup(x => x.SaveChangesAsync(default))
                    .ReturnsAsync(1)
                    .Verifiable();

                // Act
                await _repositoryManager.SaveAsync();

                // Assert
                _mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
            }

            [Fact]
            public async Task SaveAsync_WhenContextThrows_PropagatesException()
            {
                // Arrange
                var expectedException = new DbUpdateException("Test exception");
                _mockContext.Setup(x => x.SaveChangesAsync(default))
                    .ThrowsAsync(expectedException);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<DbUpdateException>(
                    () => _repositoryManager.SaveAsync());

                exception.Should().BeSameAs(expectedException);
                _mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
            }

            [Fact]
            public void ItemMasterSourceLog_MultipleAccesses_UsesLazyLoading()
            {
                // Arrange
                var accessCount = 0;
                var manager = new RepositoryManager(_mockContext.Object);

                // Act
                // Access the property multiple times
                for (var i = 0; i < 3; i++)
                {
                    accessCount++;
                    var repository = manager.ItemMasterSourceLog;
                    repository.Should().NotBeNull();
                }

                // Assert
                // Verify that we got the same instance each time
                var finalAccess = manager.ItemMasterSourceLog;
                finalAccess.Should().NotBeNull();
                accessCount.Should().Be(3);

                // All accesses should return the same instance
                var instances = new HashSet<IItemMasterSourceLogRepository>();
                for (var i = 0; i < 3; i++)
                {
                    instances.Add(manager.ItemMasterSourceLog);
                }
                instances.Count.Should().Be(1);
            }
        }
    }
}