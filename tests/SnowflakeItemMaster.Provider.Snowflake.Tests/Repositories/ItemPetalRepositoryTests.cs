using System.Data;
using Dapper;
using FluentAssertions;
using Moq;
using Moq.Dapper;
using SnowflakeItemMaster.Application.Contracts.DatabaseConnection;
using SnowflakeItemMaster.Domain.Providers;
using SnowflakeItemMaster.Provider.Snowflake.Constants;
using SnowflakeItemMaster.Provider.Snowflake.Repositories;

namespace SnowflakeItemMaster.Provider.Snowflake.Tests
{
    public class ItemPetalRepositoryTests
    {
        private readonly Mock<IDatabaseConnectionFactory> _mockConnectionFactory;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly ItemPetalRepository _repository;

        public ItemPetalRepositoryTests()
        {
            _mockConnectionFactory = new Mock<IDatabaseConnectionFactory>();
            _mockConnection = new Mock<IDbConnection>();
            _repository = new ItemPetalRepository(_mockConnectionFactory.Object);

            _mockConnectionFactory.Setup(x => x.CreateConnectionAsync())
                .ReturnsAsync(_mockConnection.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new ItemPetalRepository(null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("connectionFactory");
        }

        [Fact]
        public void Constructor_WithValidConnectionFactory_ShouldCreateInstance()
        {
            // Act
            var repository = new ItemPetalRepository(_mockConnectionFactory.Object);

            // Assert
            repository.Should().NotBeNull();
        }

        #endregion Constructor Tests

        #region GetItemsBySkusAsync Tests

        [Fact]
        public async Task GetItemsBySkusAsync_WithEmptySkuList_ReturnsEmptyList()
        {
            // Arrange
            var emptySkus = new List<string>();

            // Act
            var result = await _repository.GetItemsBySkusAsync(emptySkus);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockConnectionFactory.Verify(x => x.CreateConnectionAsync(), Times.Never);
        }

        [Fact]
        public async Task GetItemsBySkusAsync_WithNullSkuList_ReturnsEmptyList()
        {
            // Arrange
            IEnumerable<string> nullSkus = null;

            // Act
            var result = await _repository.GetItemsBySkusAsync(nullSkus);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockConnectionFactory.Verify(x => x.CreateConnectionAsync(), Times.Never);
        }

        [Fact]
        public async Task GetItemsBySkusAsync_WithValidSkus_ReturnsExpectedItems()
        {
            // Arrange
            var skus = new List<string> { "SKU001", "SKU002", "SKU003" };
            var expectedItems = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU001", Description = "Item 1" },
                new ItemPetal { Sku = "SKU002", Description = "Item 2" },
                new ItemPetal { Sku = "SKU003", Description = "Item 3" }
            };

            var expectedQuery = string.Format(SqlConstants.GetItemsBySkusQuery, ":sku0,:sku1,:sku2");

            _mockConnection.SetupDapperAsync(c => c.QueryAsync<ItemPetal>(
                It.Is<string>(sql => sql == expectedQuery),
                It.IsAny<DynamicParameters>(),
                null, null, null))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _repository.GetItemsBySkusAsync(skus);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(expectedItems);

            _mockConnectionFactory.Verify(x => x.CreateConnectionAsync(), Times.Once);
            _mockConnection.Verify();
        }

        [Fact]
        public async Task GetItemsBySkusAsync_WithSingleSku_ReturnsExpectedItems()
        {
            // Arrange
            var skus = new List<string> { "SKU001" };
            var expectedItems = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU001", Description = "Item 1" }
            };

            var expectedQuery = string.Format(SqlConstants.GetItemsBySkusQuery, ":sku0");

            _mockConnection.SetupDapperAsync(c => c.QueryAsync<ItemPetal>(
                    It.Is<string>(sql => sql == expectedQuery),
                    It.IsAny<DynamicParameters>(),
                    null, null, null))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _repository.GetItemsBySkusAsync(skus);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Sku.Should().Be("SKU001");
        }

        #endregion GetItemsBySkusAsync Tests

        #region GetItemsWithLimitAsync (with date range) Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task GetItemsWithLimitAsync_WithInvalidLimit_ThrowsArgumentException(int invalidLimit)
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddHours(-1);
            var toDate = DateTime.UtcNow;

            // Act & Assert
            var action = () => _repository.GetItemsWithLimitAsync(fromDate, toDate, invalidLimit);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("limit")
                .WithMessage("Limit must be greater than zero*");
        }

        [Fact]
        public async Task GetItemsWithLimitAsync_WithFromDateAfterToDate_ThrowsArgumentException()
        {
            // Arrange
            var fromDate = DateTime.UtcNow;
            var toDate = DateTime.UtcNow.AddHours(-1);
            var limit = 100;

            // Act & Assert
            var action = () => _repository.GetItemsWithLimitAsync(fromDate, toDate, limit);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("FromDate must be less than ToDate");
        }

        [Fact]
        public async Task GetItemsWithLimitAsync_WithFromDateEqualToDate_ThrowsArgumentException()
        {
            // Arrange
            var date = DateTime.UtcNow;
            var limit = 100;

            // Act & Assert
            var action = () => _repository.GetItemsWithLimitAsync(date, date, limit);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("FromDate must be less than ToDate");
        }

        [Fact]
        public async Task GetItemsWithLimitAsync_WithValidParameters_ReturnsExpectedItems()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddHours(-2);
            var toDate = DateTime.UtcNow;
            var limit = 50;

            var expectedItems = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU001", Description = "Item 1", UpdatedAtSnowflake = DateTime.UtcNow.AddMinutes(-30) },
                new ItemPetal { Sku = "SKU002", Description = "Item 2", UpdatedAtSnowflake = DateTime.UtcNow.AddMinutes(-45) }
            };

            _mockConnection.SetupDapperAsync(c => c.QueryAsync<ItemPetal>(
                    SqlConstants.GetItemsWithLimitQuery,
                    It.IsAny<DynamicParameters>(),
                    null, null, null))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _repository.GetItemsWithLimitAsync(fromDate, toDate, limit);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedItems);

            _mockConnectionFactory.Verify(x => x.CreateConnectionAsync(), Times.Once);
            _mockConnection.Verify();
        }

        [Fact]
        public async Task GetItemsWithLimitAsync_WithDefaultLimit_Uses100AsDefault()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddHours(-1);
            var toDate = DateTime.UtcNow;
            var expectedItems = new List<ItemPetal>();

            _mockConnection.SetupDapperAsync(c => c.QueryAsync<ItemPetal>(
                    SqlConstants.GetItemsWithLimitQuery,
                    It.Is<DynamicParameters>(p => GetParameterValue<int>(p, "limit") == 100),
                    null, null, null))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _repository.GetItemsWithLimitAsync(fromDate, toDate);

            // Assert
            result.Should().NotBeNull();
            _mockConnection.Verify();
        }

        #endregion GetItemsWithLimitAsync (with date range) Tests

        #region GetItemsWithLimitAsync (overload with hours) Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task GetItemsWithLimitAsync_Overload_WithInvalidLimit_ThrowsArgumentException(int invalidLimit)
        {
            // Act & Assert
            var action = () => _repository.GetItemsWithLimitAsync(invalidLimit);
            await action.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("limit")
                .WithMessage("Limit must be greater than zero*");
        }

        #endregion GetItemsWithLimitAsync (overload with hours) Tests

        #region Helper Methods

        private static T GetParameterValue<T>(DynamicParameters parameters, string name)
        {
            var parameterNames = parameters.ParameterNames;
            return parameterNames.Contains(name) ? default(T) : default(T);
        }

        #endregion Helper Methods
    }
}