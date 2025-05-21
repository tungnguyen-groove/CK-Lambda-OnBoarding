using FluentAssertions;
using SnowflakeItemMaster.Application.Builders;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.Tests.Builders
{
    public class UnifiedItemModelBuilderTests
    {
        private readonly UnifiedItemModelBuilder _builder;
        private readonly ItemPetal _sampleItemPetal;

        public UnifiedItemModelBuilderTests()
        {
            _builder = new UnifiedItemModelBuilder();
            _sampleItemPetal = new ItemPetal
            {
                Sku = "TEST-SKU-001",
                ProductTitle = "Test Product",
                Description = "Test Description",
                ChinaHts = "1234.56.78",
                CountryOfOrigin = "US",
                Barcode = "123456789",
                SecondaryBarcode = "987654321",
                Hts = "1234567890",
                Price = 99.99,
                Cost = 50.00,
                LandedCost = 60.00,
                ProductType = "Test Type",
                Category = "Test Category",
                Size = "Large",
                Color = "Blue",
                Brand = "Test Brand",
                FabricContent = "Cotton",
                FabricComposition = "100% Cotton",
                Gender = "Unisex",
                VelocityCode = "Fast",
                FastMover = "Yes",
                InventorySyncFlag = "Enabled",
                ProductImageUrl = "http://test.com/image.jpg",
                ProductImageUrlPos1 = "http://test.com/image1.jpg",
                ProductImageUrlPos2 = "http://test.com/image2.jpg",
                ProductImageUrlPos3 = "http://test.com/image3.jpg",
                CreatedAtShopify = DateTime.UtcNow.AddDays(-10),
                CreatedAtSnowflake = DateTime.UtcNow.AddDays(-5),
                UpdatedAtSnowflake = DateTime.UtcNow,
                LatestPoCreatedDate = DateTime.UtcNow
            };
        }

        [Fact]
        public void WithBasicInfo_SetsBasicProperties()
        {
            // Act
            var result = _builder.WithBasicInfo(_sampleItemPetal).Build();

            // Assert
            result.Sku.Should().Be(_sampleItemPetal.Sku);
            result.Name.Should().Be(_sampleItemPetal.ProductTitle);
            result.Description.Should().Be(_sampleItemPetal.Description);
            result.ChinaHtsCode.Should().Be(_sampleItemPetal.ChinaHts);
            result.CountryOfOriginCode.Should().Be(_sampleItemPetal.CountryOfOrigin);
        }

        [Theory]
        [InlineData("US")]
        [InlineData("CN")]
        public void WithBasicInfo_ValidCountryCode_SetsCountryCode(string countryCode)
        {
            // Arrange
            _sampleItemPetal.CountryOfOrigin = countryCode;

            // Act
            var result = _builder.WithBasicInfo(_sampleItemPetal).Build();

            // Assert
            result.CountryOfOriginCode.Should().Be(countryCode);
        }

        [Theory]
        [InlineData("USA")]
        [InlineData("CHN")]
        [InlineData(null)]
        [InlineData("")]
        public void WithBasicInfo_InvalidCountryCode_SetsNull(string countryCode)
        {
            // Arrange
            _sampleItemPetal.CountryOfOrigin = countryCode;

            // Act
            var result = _builder.WithBasicInfo(_sampleItemPetal).Build();

            // Assert
            result.CountryOfOriginCode.Should().BeNull();
        }

        [Fact]
        public void WithHts_ValidTariffCode_SetsHtsTariffCode()
        {
            // Arrange
            var htsCode = new string('1', TransformationConstants.HtsCode.TariffCodeLength);
            _sampleItemPetal.Hts = htsCode;

            // Act
            var result = _builder.WithHts(_sampleItemPetal).Build();

            // Assert
            result.HtsTariffCode.Should().Be(htsCode);
            result.HsCommodityCode.Should().BeNull();
        }

        [Fact]
        public void WithHts_ValidCommodityCode_SetsHsCommodityCode()
        {
            // Arrange
            var fullCode = new string('1', TransformationConstants.HtsCode.CommodityCodeLength + 2);
            _sampleItemPetal.Hts = fullCode;

            // Act
            var result = _builder.WithHts(_sampleItemPetal).Build();

            // Assert
            result.HsCommodityCode.Should().Be(fullCode.Substring(0, TransformationConstants.HtsCode.CommodityCodeLength));
            result.HtsTariffCode.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12345")]  // Too short
        public void WithHts_InvalidCode_SetsNeither(string htsCode)
        {
            // Arrange
            _sampleItemPetal.Hts = htsCode;

            // Act
            var result = _builder.WithHts(_sampleItemPetal).Build();

            // Assert
            result.HtsTariffCode.Should().BeNull();
            result.HsCommodityCode.Should().BeNull();
        }

        [Fact]
        public void WithBarcode_NewPO_SetsGs1Barcode()
        {
            // Arrange
            _sampleItemPetal.LatestPoCreatedDate = DateTime.Parse("2024-02-01");

            // Act
            var result = _builder.WithBarcode(_sampleItemPetal).Build();

            // Assert
            result.Gs1Barcode.Should().Be(_sampleItemPetal.Barcode);
            result.AlternateBarcodes.Should().BeNull();
        }

        [Fact]
        public void WithBarcode_OldPO_SetsAlternateBarcode()
        {
            // Arrange
            _sampleItemPetal.LatestPoCreatedDate = DateTime.Parse("2023-12-31");

            // Act
            var result = _builder.WithBarcode(_sampleItemPetal).Build();

            // Assert
            result.Gs1Barcode.Should().BeNull();
            result.AlternateBarcodes.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new AlternateBarcode
                {
                    Type = TransformationConstants.Barcode.AlternateBarcodeType,
                    Value = _sampleItemPetal.SecondaryBarcode
                });
        }

        [Fact]
        public void WithPrices_ValidPrices_SetsAllPrices()
        {
            // Act
            var result = _builder.WithPrices(_sampleItemPetal).Build();

            // Assert
            result.Prices.Should().HaveCount(3);
            result.Prices.Should().Contain(p => p.Type == TransformationConstants.Price.ListType && p.Value == (decimal)_sampleItemPetal.Price);
            result.Prices.Should().Contain(p => p.Type == TransformationConstants.Price.UnitType && p.Value == (decimal)_sampleItemPetal.Cost);
            result.Prices.Should().Contain(p => p.Type == TransformationConstants.Price.LandedType && p.Value == (decimal)_sampleItemPetal.LandedCost);
        }

        [Fact]
        public void WithCosts_ValidCosts_SetsAllCosts()
        {
            // Act
            var result = _builder.WithCosts(_sampleItemPetal).Build();

            // Assert
            result.Costs.Should().HaveCount(2);
            result.Costs.Should().Contain(c => c.Type == TransformationConstants.Price.UnitType && c.Value == (decimal)_sampleItemPetal.Cost);
            result.Costs.Should().Contain(c => c.Type == TransformationConstants.Price.LandedType && c.Value == (decimal)_sampleItemPetal.LandedCost);
        }

        [Fact]
        public void WithCategories_ValidCategories_SetsAllCategories()
        {
            // Act
            var result = _builder.WithCategories(_sampleItemPetal).Build();

            // Assert
            result.Categories.Should().HaveCount(2);
            result.Categories.Should().Contain(c => c.Source == TransformationConstants.Category.BrandSource && c.Path == _sampleItemPetal.ProductType);
            result.Categories.Should().Contain(c => c.Source == TransformationConstants.Category.AkaSource && c.Path == _sampleItemPetal.Category);
        }

        [Fact]
        public void WithAttributes_ValidAttributes_SetsAllAttributes()
        {
            // Act
            var result = _builder.WithAttributes(_sampleItemPetal).Build();

            // Assert
            result.Attributes.Should().HaveCount(10);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.SizeId && (string)a.Value == _sampleItemPetal.Size);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.ColorId && (string)a.Value == _sampleItemPetal.Color);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.BrandNameId && (string)a.Value == _sampleItemPetal.Brand);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.FabricContentId && (string)a.Value == _sampleItemPetal.FabricContent);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.FabricCompositionId && (string)a.Value == _sampleItemPetal.FabricComposition);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.GenderId && (string)a.Value == _sampleItemPetal.Gender);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.VelocityCodeId && (string)a.Value == _sampleItemPetal.VelocityCode);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.FastMoverId && (string)a.Value == _sampleItemPetal.FastMover);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.BrandEntityId && (string)a.Value == _sampleItemPetal.Brand);
            result.Attributes.Should().Contain(a => a.Id == TransformationConstants.Attribute.InventorySyncEnabledId && (string)a.Value == _sampleItemPetal.InventorySyncFlag);
        }

        [Fact]
        public void WithLinks_ValidLinks_SetsAllLinks()
        {
            // Act
            var result = _builder.WithLinks(_sampleItemPetal).Build();

            // Assert
            result.Links.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new Link
                {
                    Source = TransformationConstants.Link.ShopifyUsSource,
                    Url = _sampleItemPetal.ProductImageUrl
                });
        }

        [Fact]
        public void WithImages_ValidImages_SetsAllImages()
        {
            // Act
            var result = _builder.WithImages(_sampleItemPetal).Build();

            // Assert
            result.Images.Should().HaveCount(3);
            result.Images.Should().Contain(i => i.SizeType == TransformationConstants.Image.OriginalSizeType && i.Url == _sampleItemPetal.ProductImageUrlPos1);
            result.Images.Should().Contain(i => i.SizeType == TransformationConstants.Image.OriginalSizeType && i.Url == _sampleItemPetal.ProductImageUrlPos2);
            result.Images.Should().Contain(i => i.SizeType == TransformationConstants.Image.OriginalSizeType && i.Url == _sampleItemPetal.ProductImageUrlPos3);
        }

        [Fact]
        public void WithDates_ValidDates_SetsAllDates()
        {
            // Act
            var result = _builder.WithDates(_sampleItemPetal).Build();

            // Assert
            result.Dates.Should().HaveCount(3);
            result.Dates.Should().Contain(d => d.System == TransformationConstants.DateSystem.Shopify && ((DateTimeOffset?)d.CreatedAt).Equals(_sampleItemPetal.CreatedAtShopify));
            result.Dates.Should().Contain(d => d.System == TransformationConstants.DateSystem.Snowflake && ((DateTimeOffset?)d.CreatedAt).Equals(_sampleItemPetal.CreatedAtSnowflake));
            result.Dates.Should().Contain(d => d.System == TransformationConstants.DateSystem.Snowflake && ((DateTimeOffset?)d.LastUpdatedAt).Equals(_sampleItemPetal.UpdatedAtSnowflake));
        }

        [Fact]
        public void Build_WithAllProperties_CreatesCompleteModel()
        {
            // Act
            var result = _builder
                .WithBasicInfo(_sampleItemPetal)
                .WithHts(_sampleItemPetal)
                .WithBarcode(_sampleItemPetal)
                .WithPrices(_sampleItemPetal)
                .WithCosts(_sampleItemPetal)
                .WithCategories(_sampleItemPetal)
                .WithAttributes(_sampleItemPetal)
                .WithLinks(_sampleItemPetal)
                .WithImages(_sampleItemPetal)
                .WithDates(_sampleItemPetal)
                .Build();

            // Assert
            result.Should().NotBeNull();
            result.Sku.Should().Be(_sampleItemPetal.Sku);
            result.Name.Should().Be(_sampleItemPetal.ProductTitle);
            result.Description.Should().Be(_sampleItemPetal.Description);
            result.Prices.Should().NotBeEmpty();
            result.Costs.Should().NotBeEmpty();
            result.Categories.Should().NotBeEmpty();
            result.Attributes.Should().NotBeEmpty();
            result.Links.Should().NotBeEmpty();
            result.Images.Should().NotBeEmpty();
            result.Dates.Should().NotBeEmpty();
        }

        [Fact]
        public void Build_WithNoProperties_CreatesEmptyModel()
        {
            // Act
            var result = new UnifiedItemModelBuilder().Build();

            // Assert
            result.Should().NotBeNull();
            result.Sku.Should().BeNull();
            result.Name.Should().BeNull();
            result.Description.Should().BeNull();
            result.Prices.Should().BeNull();
            result.Costs.Should().BeNull();
            result.Categories.Should().BeNull();
            result.Attributes.Should().BeNull();
            result.Links.Should().BeNull();
            result.Images.Should().BeNull();
            result.Dates.Should().BeNull();
        }
    }
}