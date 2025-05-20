using FluentAssertions;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.UseCases;
using SnowflakeItemMaster.Domain.Providers;
using System.Text.Json;

namespace SnowflakeItemMaster.Application.Tests
{
    public class TransformServiceTests
    {
        private readonly TransformService _sut;

        public TransformServiceTests()
        {
            _sut = new TransformService();
        }

        [Fact]
        public void TransformItemPetalToItemMasterSourceLog_WhenAllRequiredFieldsPresent_ShouldReturnValidSourceLog()
        {
            // Arrange
            var itemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-001",
                Barcode = "123456789",
                ProductTitle = "Test Product",
                Hts = "1234567890",
                CountryOfOrigin = "US",
                Size = "M",
                Color = "Blue",
                Brand = "TestBrand",
                FabricContent = "Cotton",
                FabricComposition = "100% Cotton",
                Gender = "Male",
                InventorySyncFlag = "Y",
                Price = 99.99,
                LandedCost = 50.00
            };

            // Act
            var result = _sut.TransformItemPetalToItemMasterSourceLog(itemPetal);

            // Assert
            result.Should().NotBeNull();
            result.Sku.Should().Be(itemPetal.Sku);
            result.ValidationStatus.Should().Be("valid");
            result.Errors.Should().BeNull();
            result.IsSentToSqs.Should().BeFalse();
            result.SourceModel.Should().NotBeNull();

            var deserializedSourceModel = JsonSerializer.Deserialize<ItemPetal>(result.SourceModel);
            deserializedSourceModel.Should().BeEquivalentTo(itemPetal);
        }

        [Fact]
        public void TransformItemPetalToItemMasterSourceLog_WhenMissingRequiredFields_ShouldReturnInvalidSourceLog()
        {
            // Arrange
            var itemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-002",
                ProductTitle = "Test Product",
                Price = 99.99
            };

            // Act
            var result = _sut.TransformItemPetalToItemMasterSourceLog(itemPetal);

            // Assert
            result.Should().NotBeNull();
            result.Sku.Should().Be(itemPetal.Sku);
            result.ValidationStatus.Should().Be(Status.Invalid); // Sử dụng constant
            result.Errors.Should().NotBeNull();
            result.Errors.Should().Contain("Missing required fields");
            result.IsSentToSqs.Should().BeFalse();
            result.SourceModel.Should().NotBeNull();

            var deserializedSourceModel = JsonSerializer.Deserialize<ItemPetal>(result.SourceModel);
            deserializedSourceModel.Should().BeEquivalentTo(itemPetal);
        }

        [Fact]
        public void MapToUnifiedItemMaster_ShouldMapAllFieldsCorrectly()
        {
            // Arrange
            var itemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-003",
                ProductTitle = "Test Product",
                Description = "Test Description",
                ChinaHts = "CN1234",
                CountryOfOrigin = "US",
                Hts = "1234567890",
                Barcode = "123456789",
                SecondaryBarcode = "987654321",
                LatestPoCreatedDate = DateTimeOffset.UtcNow,
                Price = 99.99,
                Cost = 40.00,
                LandedCost = 50.00,
                ProductType = "Apparel",
                Category = "Shirts",
                Size = "L",
                Color = "Red",
                Brand = "TestBrand",
                FabricContent = "Cotton",
                FabricComposition = "100% Cotton",
                Gender = "Female",
                VelocityCode = "Fast",
                FastMover = "Y",
                InventorySyncFlag = "Y",
                ProductImageUrl = "http://test.com/image.jpg",
                ProductImageUrlPos1 = "http://test.com/image1.jpg",
                ProductImageUrlPos2 = "http://test.com/image2.jpg",
                ProductImageUrlPos3 = "http://test.com/image3.jpg",
                CreatedAtShopify = DateTimeOffset.UtcNow.AddDays(-10),
                CreatedAtSnowflake = DateTimeOffset.UtcNow.AddDays(-5),
                UpdatedAtSnowflake = DateTimeOffset.UtcNow
            };

            // Act
            var result = _sut.MapToUnifiedItemMaster(itemPetal);

            // Assert
            result.Should().NotBeNull();
            result.Sku.Should().Be(itemPetal.Sku);
            result.Name.Should().Be(itemPetal.ProductTitle);
            result.Description.Should().Be(itemPetal.Description);
            result.ChinaHtsCode.Should().Be(itemPetal.ChinaHts);
            result.CountryOfOriginCode.Should().Be(itemPetal.CountryOfOrigin);
            result.HtsTariffCode.Should().Be(itemPetal.Hts);

            // Verify Barcode
            result.Gs1Barcode.Should().Be(itemPetal.Barcode);
            if (itemPetal.LatestPoCreatedDate < new DateTime(2024, 1, 1))
            {
                result.AlternateBarcodes.Should().ContainSingle(b =>
                    b.Type == TransformationConstants.Barcode.AlternateBarcodeType &&
                    b.Value == itemPetal.SecondaryBarcode);
            }

            // Verify Prices
            result.Prices.Should().HaveCount(3);
            result.Prices.Should().Contain(p =>
                p.Type == TransformationConstants.Price.ListType &&
                p.Value == (decimal)itemPetal.Price.Value &&
                p.Currency == TransformationConstants.Price.DefaultCurrency);
            result.Prices.Should().Contain(p =>
                p.Type == TransformationConstants.Price.UnitType &&
                p.Value == (decimal)itemPetal.Cost.Value &&
                p.Currency == TransformationConstants.Price.DefaultCurrency);
            result.Prices.Should().Contain(p =>
                p.Type == TransformationConstants.Price.LandedType &&
                p.Value == (decimal)itemPetal.LandedCost.Value &&
                p.Currency == TransformationConstants.Price.DefaultCurrency);

            // Verify Costs
            result.Costs.Should().HaveCount(2);
            result.Costs.Should().Contain(c =>
                c.Type == TransformationConstants.Price.UnitType &&
                c.Value == (decimal)itemPetal.Cost.Value &&
                c.Currency == TransformationConstants.Price.DefaultCurrency);
            result.Costs.Should().Contain(c =>
                c.Type == TransformationConstants.Price.LandedType &&
                c.Value == (decimal)itemPetal.LandedCost.Value &&
                c.Currency == TransformationConstants.Price.DefaultCurrency);

            // Verify Categories
            result.Categories.Should().HaveCount(2);
            result.Categories.Should().Contain(c =>
                c.Source == TransformationConstants.Category.BrandSource &&
                c.Path == itemPetal.ProductType);
            result.Categories.Should().Contain(c =>
                c.Source == TransformationConstants.Category.AkaSource &&
                c.Path == itemPetal.Category);

            // Verify Attributes
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.SizeId &&
                a.Value == itemPetal.Size);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.ColorId &&
                a.Value == itemPetal.Color);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.BrandNameId &&
                a.Value == itemPetal.Brand);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.FabricContentId &&
                a.Value == itemPetal.FabricContent);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.FabricCompositionId &&
                a.Value == itemPetal.FabricComposition);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.GenderId &&
                a.Value == itemPetal.Gender);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.VelocityCodeId &&
                a.Value == itemPetal.VelocityCode);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.FastMoverId &&
                a.Value == itemPetal.FastMover);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.BrandEntityId &&
                a.Value == itemPetal.Brand);
            result.Attributes.Should().Contain(a =>
                a.Id == TransformationConstants.Attribute.InventorySyncEnabledId &&
                a.Value == itemPetal.InventorySyncFlag);

            // Verify Links
            result.Links.Should().ContainSingle(l =>
                l.Source == TransformationConstants.Link.ShopifyUsSource &&
                l.Url == itemPetal.ProductImageUrl);

            // Verify Images
            result.Images.Should().HaveCount(3);
            result.Images.Should().OnlyContain(i =>
                i.SizeType == TransformationConstants.Image.OriginalSizeType);
            result.Images.Should().Contain(i => i.Url == itemPetal.ProductImageUrlPos1);
            result.Images.Should().Contain(i => i.Url == itemPetal.ProductImageUrlPos2);
            result.Images.Should().Contain(i => i.Url == itemPetal.ProductImageUrlPos3);

            // Verify Dates
            result.Dates.Should().Contain(d =>
                d.System == "Shopify" && d.CreatedAt.Equals(itemPetal.CreatedAtShopify));
            result.Dates.Should().Contain(d =>
                d.System == "snowflake" && d.CreatedAt.Equals(itemPetal.CreatedAtSnowflake));
        }

        [Fact]
        public void MapToUnifiedItemMaster_WithPartialHtsCode_ShouldMapToCommodityCode()
        {
            // Arrange
            var itemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-004",
                Hts = "123456", // 6-digit commodity code
                // Other required fields...
            };

            // Act
            var result = _sut.MapToUnifiedItemMaster(itemPetal);

            // Assert
            result.Should().NotBeNull();
            result.HsCommodityCode.Should().Be("123456");
            result.HtsTariffCode.Should().BeNull();
        }

        [Fact]
        public void MapToUnifiedItemMaster_WithInvalidCountryCode_ShouldNotSetCountryCode()
        {
            // Arrange
            var itemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-005",
                CountryOfOrigin = "USA", // Invalid 3-letter code
                // Other required fields...
            };

            // Act
            var result = _sut.MapToUnifiedItemMaster(itemPetal);

            // Assert
            result.Should().NotBeNull();
            result.CountryOfOriginCode.Should().BeNull();
        }
    }
}