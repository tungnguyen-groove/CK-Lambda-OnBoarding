using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.Builders
{
    public class UnifiedItemModelBuilder
    {
        private readonly UnifiedItemModel _item;

        public UnifiedItemModelBuilder()
        {
            _item = new UnifiedItemModel();
        }

        public UnifiedItemModelBuilder WithBasicInfo(ItemPetal source)
        {
            _item.Sku = source.Sku;
            _item.Name = source.ProductTitle;
            _item.Description = source.Description;
            _item.ChinaHtsCode = source.ChinaHts;
            _item.CountryOfOriginCode = source.CountryOfOrigin?.Length == 2 ? source.CountryOfOrigin : null;

            return this;
        }

        public UnifiedItemModelBuilder WithHts(ItemPetal source)
        {
            if (source.Hts?.Length == TransformationConstants.HtsCode.TariffCodeLength)
            {
                _item.HtsTariffCode = source.Hts;
            }
            else if (source.Hts?.Length >= TransformationConstants.HtsCode.CommodityCodeLength)
            {
                _item.HsCommodityCode = source.Hts.Substring(0, TransformationConstants.HtsCode.CommodityCodeLength);
            }

            return this;
        }

        public UnifiedItemModelBuilder WithBarcode(ItemPetal source)
        {
            if (!string.IsNullOrWhiteSpace(source.Barcode)
                && source.LatestPoCreatedDate.HasValue
                && source.LatestPoCreatedDate.Value >= new DateTime(2024, 1, 1))
            {
                _item.Gs1Barcode = source.Barcode;
            }

            if (!string.IsNullOrWhiteSpace(source.SecondaryBarcode)
                && source.LatestPoCreatedDate.HasValue
                && source.LatestPoCreatedDate.Value < new DateTime(2024, 1, 1))
            {
                _item.AlternateBarcodes = new List<AlternateBarcode>
                {
                    new AlternateBarcode
                    {
                        Type = TransformationConstants.Barcode.AlternateBarcodeType,
                        Value = source.SecondaryBarcode,
                    }
                };
            }

            return this;
        }

        public UnifiedItemModelBuilder WithPrices(ItemPetal source)
        {
            var prices = new List<Price>();

            if (decimal.TryParse(source.Price?.ToString(), out var price))
            {
                prices.Add(new Price
                {
                    Value = price,
                    Type = TransformationConstants.Price.ListType,
                    Currency = TransformationConstants.Price.DefaultCurrency
                });
            }

            if (decimal.TryParse(source.Cost?.ToString(), out var cost))
            {
                prices.Add(new Price
                {
                    Value = cost,
                    Type = TransformationConstants.Price.UnitType,
                    Currency = TransformationConstants.Price.DefaultCurrency
                });
            }

            if (decimal.TryParse(source.LandedCost?.ToString(), out var landedCost))
            {
                prices.Add(new Price
                {
                    Value = landedCost,
                    Type = TransformationConstants.Price.LandedType,
                    Currency = TransformationConstants.Price.DefaultCurrency
                });
            }

            _item.Prices = prices;
            return this;
        }

        public UnifiedItemModelBuilder WithCosts(ItemPetal source)
        {
            var costs = new List<Cost>();

            if (decimal.TryParse(source.Cost?.ToString(), out var cost))
            {
                costs.Add(new Cost
                {
                    Value = cost,
                    Type = TransformationConstants.Price.UnitType,
                    Currency = TransformationConstants.Price.DefaultCurrency
                });
            }

            if (decimal.TryParse(source.LandedCost?.ToString(), out var landedCost))
            {
                costs.Add(new Cost
                {
                    Value = landedCost,
                    Type = TransformationConstants.Price.LandedType,
                    Currency = TransformationConstants.Price.DefaultCurrency
                });
            }

            _item.Costs = costs;
            return this;
        }

        public UnifiedItemModelBuilder WithCategories(ItemPetal source)
        {
            var categories = new List<Category>();

            if (!string.IsNullOrEmpty(source.ProductType))
            {
                categories.Add(new Category
                {
                    Path = source.ProductType,
                    Source = TransformationConstants.Category.BrandSource
                });
            }

            if (!string.IsNullOrEmpty(source.Category))
            {
                categories.Add(new Category
                {
                    Path = source.Category,
                    Source = TransformationConstants.Category.AkaSource
                });
            }

            _item.Categories = categories;
            return this;
        }

        public UnifiedItemModelBuilder WithAttributes(ItemPetal source)
        {
            var attributeMappings = new List<(string sourceValue, string targetId)>
            {
                (source.Size, TransformationConstants.Attribute.SizeId),
                (source.Color, TransformationConstants.Attribute.ColorId),
                (source.Brand, TransformationConstants.Attribute.BrandNameId),
                (source.FabricContent, TransformationConstants.Attribute.FabricContentId),
                (source.FabricComposition, TransformationConstants.Attribute.FabricCompositionId),
                (source.Gender, TransformationConstants.Attribute.GenderId),
                (source.VelocityCode, TransformationConstants.Attribute.VelocityCodeId),
                (source.FastMover, TransformationConstants.Attribute.FastMoverId),
                (source.Brand, TransformationConstants.Attribute.BrandEntityId),
                (source.InventorySyncFlag, TransformationConstants.Attribute.InventorySyncEnabledId)
            };

            _item.Attributes = attributeMappings
                .Where(x => !string.IsNullOrWhiteSpace(x.sourceValue))
                .Select(x => new Models.Attribute
                {
                    Id = x.targetId,
                    Value = x.sourceValue
                })
                .ToList();

            return this;
        }

        public UnifiedItemModelBuilder WithLinks(ItemPetal source)
        {
            var links = new List<Link>();

            if (!string.IsNullOrWhiteSpace(source.ProductImageUrl))
            {
                links.Add(new Link
                {
                    Source = TransformationConstants.Link.ShopifyUsSource,
                    Url = source.ProductImageUrl
                });
            }

            _item.Links = links;
            return this;
        }

        public UnifiedItemModelBuilder WithImages(ItemPetal source)
        {
            var images = new List<Image>();
            var imageUrls = new[]
            {
                source.ProductImageUrlPos1,
                source.ProductImageUrlPos2,
                source.ProductImageUrlPos3
            };

            foreach (var imageUrl in imageUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                images.Add(new Image
                {
                    SizeType = TransformationConstants.Image.OriginalSizeType,
                    Url = imageUrl
                });
            }

            _item.Images = images;
            return this;
        }

        public UnifiedItemModelBuilder WithDates(ItemPetal source)
        {
            var dates = new List<DateInfo>();

            if (source.CreatedAtShopify.HasValue)
            {
                dates.Add(new DateInfo
                {
                    System = TransformationConstants.DateSystem.Shopify,
                    CreatedAt = source.CreatedAtShopify
                });
            }

            if (source.CreatedAtSnowflake.HasValue)
            {
                dates.Add(new DateInfo
                {
                    System = TransformationConstants.DateSystem.Snowflake,
                    CreatedAt = source.CreatedAtSnowflake
                });
            }

            if (source.UpdatedAtSnowflake.HasValue)
            {
                dates.Add(new DateInfo
                {
                    System = TransformationConstants.DateSystem.Snowflake,
                    LastUpdatedAt = source.UpdatedAtSnowflake
                });
            }

            _item.Dates = dates;
            return this;
        }

        public UnifiedItemModel Build()
        {
            return _item;
        }
    }
}