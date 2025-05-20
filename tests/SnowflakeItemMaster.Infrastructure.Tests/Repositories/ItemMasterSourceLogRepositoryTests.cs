using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SnowflakeItemMaster.Domain.Entities;
using SnowflakeItemMaster.Infrastructure.Repositories;
using System.Text.Json;

namespace SnowflakeItemMaster.Infrastructure.Tests.Repositories
{
    public class ItemMasterSourceLogRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<RepositoryContext> _options;
        private readonly RepositoryContext _context;
        private readonly ItemMasterSourceLogRepository _repository;

        public ItemMasterSourceLogRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<RepositoryContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new RepositoryContext(_options);
            _repository = new ItemMasterSourceLogRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private ItemMasterSourceLog CreateTestLog(string sku = "TEST-SKU", string status = "Valid")
        {
            return new ItemMasterSourceLog
            {
                Sku = sku,
                SourceModel = JsonSerializer.Serialize(new { test = "source" }),
                CommonModel = JsonSerializer.Serialize(new { test = "common" }),
                ValidationStatus = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task Create_ValidLog_SavesSuccessfully()
        {
            // Arrange
            var log = CreateTestLog();

            // Act
            _repository.Create(log);
            await _context.SaveChangesAsync();

            // Assert
            var savedLog = await _context.ItemMasterSourceLogs.FirstOrDefaultAsync();
            savedLog.Should().NotBeNull();
            savedLog!.Id.Should().BeGreaterThan(0);
            savedLog.Sku.Should().Be(log.Sku);
            savedLog.ValidationStatus.Should().Be(log.ValidationStatus);
            savedLog.IsSentToSqs.Should().BeFalse();
            savedLog.Errors.Should().BeNull();
        }

        [Fact]
        public void FindAll_WithNoTracking_ReturnsUnTrackedEntities()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            _context.SaveChanges();

            // Act
            var result = _repository.FindAll(trackChanges: false);

            // Assert
            result.Should().NotBeNull();
            var entry = _context.Entry(result.First());
            entry.State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public void FindByCondition_WithValidCondition_ReturnsMatchingEntities()
        {
            // Arrange
            var logs = new[]
            {
                CreateTestLog("SKU-1", "Valid"),
                CreateTestLog("SKU-2", "Invalid"),
                CreateTestLog("SKU-3", "Valid")
            };
            _context.ItemMasterSourceLogs.AddRange(logs);
            _context.SaveChanges();

            // Act
            var result = _repository.FindByCondition(x => x.ValidationStatus == "Valid", false).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(x => x.ValidationStatus == "Valid");
        }

        [Fact]
        public async Task UpdateSentStatus_ValidId_UpdatesSuccessfully()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.UpdateSentStatusAsync(log.Id, true, "Test error");
            await _context.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();
            var updatedLog = await _context.ItemMasterSourceLogs.FindAsync(log.Id);
            updatedLog.Should().NotBeNull();
            updatedLog!.IsSentToSqs.Should().BeTrue();
            updatedLog.Errors.Should().Be("Test error");
        }

        [Fact]
        public async Task UpdateSentStatus_InvalidId_ReturnsFalse()
        {
            // Act
            var result = await _repository.UpdateSentStatusAsync(999, true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddRange_ValidLogs_AddsAllSuccessfully()
        {
            // Arrange
            var logs = new[]
            {
                CreateTestLog("SKU-1"),
                CreateTestLog("SKU-2"),
                CreateTestLog("SKU-3")
            };

            // Act
            _repository.AddRange(logs);
            await _context.SaveChangesAsync();

            // Assert
            var savedLogs = await _context.ItemMasterSourceLogs.ToListAsync();
            savedLogs.Should().HaveCount(3);
            savedLogs.Select(x => x.Sku).Should().BeEquivalentTo(new[] { "SKU-1", "SKU-2", "SKU-3" });
        }

        [Fact]
        public async Task BatchUpdateSentStatus_ValidIds_UpdatesAllSuccessfully()
        {
            // Arrange
            var logs = new[]
            {
                CreateTestLog("SKU-1"),
                CreateTestLog("SKU-2"),
                CreateTestLog("SKU-3")
            };
            _context.ItemMasterSourceLogs.AddRange(logs);
            await _context.SaveChangesAsync();

            var ids = logs.Select(x => x.Id).ToList();

            // Act
            await _repository.BatchUpdateSentStatusAsync(ids, true, "Batch error");
            await _context.SaveChangesAsync();

            // Assert
            var updatedLogs = await _context.ItemMasterSourceLogs.ToListAsync();
            updatedLogs.Should().HaveCount(3);
            updatedLogs.Should().OnlyContain(x => x.IsSentToSqs && x.Errors == "Batch error");
        }

        [Fact]
        public async Task BatchUpdateSentStatus_MixedValidAndInvalidIds_UpdatesOnlyValid()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            var ids = new List<int> { log.Id, 999 }; // One valid, one invalid

            // Act
            await _repository.BatchUpdateSentStatusAsync(ids, true, "Test error");
            await _context.SaveChangesAsync();

            // Assert
            var updatedLogs = await _context.ItemMasterSourceLogs.ToListAsync();
            updatedLogs.Should().HaveCount(1);
            updatedLogs[0].IsSentToSqs.Should().BeTrue();
            updatedLogs[0].Errors.Should().Be("Test error");
        }

        [Fact]
        public async Task Update_ModifiesExistingEntity()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            log.ValidationStatus = "Updated";
            _repository.Update(log);
            await _context.SaveChangesAsync();

            // Assert
            var updatedLog = await _context.ItemMasterSourceLogs.FindAsync(log.Id);
            updatedLog.Should().NotBeNull();
            updatedLog!.ValidationStatus.Should().Be("Updated");
        }

        [Fact]
        public async Task Delete_RemovesEntity()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            _repository.Delete(log);
            await _context.SaveChangesAsync();

            // Assert
            var deletedLog = await _context.ItemMasterSourceLogs.FindAsync(log.Id);
            deletedLog.Should().BeNull();
        }

        [Fact]
        public async Task Create_WithInvalidData_ThrowsException()
        {
            // Arrange
            var log = new ItemMasterSourceLog(); // Missing required fields

            // Act & Assert
            _repository.Create(log);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        [Fact]
        public async Task BatchUpdateSentStatus_WithEmptyIdList_DoesNothing()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            await _repository.BatchUpdateSentStatusAsync(new List<int>(), true, "Test error");
            await _context.SaveChangesAsync();

            // Assert
            var unchangedLog = await _context.ItemMasterSourceLogs.FindAsync(log.Id);
            unchangedLog.Should().NotBeNull();
            unchangedLog!.IsSentToSqs.Should().BeFalse();
            unchangedLog.Errors.Should().BeNull();
        }

        [Fact]
        public async Task FindByCondition_WithTracking_ReturnsTrackedEntities()
        {
            // Arrange
            var log = CreateTestLog();
            _context.ItemMasterSourceLogs.Add(log);
            await _context.SaveChangesAsync();

            // Act
            var result = _repository.FindByCondition(x => x.Id == log.Id, true).First();
            result.ValidationStatus = "Changed";

            // Assert
            var entry = _context.Entry(result);
            entry.State.Should().Be(EntityState.Modified);
        }
    }
}