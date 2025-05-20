using SnowflakeItemMaster.Application.Builders;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Domain.Entities;
using SnowflakeItemMaster.Domain.Providers;
using System.Text.Json;

namespace SnowflakeItemMaster.Application.UseCases
{
    public class TransformService : ITransformService
    {
        public ItemMasterSourceLog TransformItemPetalToItemMasterSourceLog(ItemPetal itemPetal)
        {
            var isValid = ValidateItemPetal(itemPetal, out var errors);

            return CreateItemMasterSourceLog(itemPetal, isValid, errors);
        }

        private bool ValidateItemPetal(ItemPetal itemPetal, out string errors)
        {
            var requiredStringFields = new Dictionary<string, string>
                {
                { nameof(itemPetal.Barcode), itemPetal.Barcode },
                { nameof(itemPetal.ProductTitle), itemPetal.ProductTitle },
                { nameof(itemPetal.Sku), itemPetal.Sku },
                { nameof(itemPetal.Hts), itemPetal.Hts },
                { nameof(itemPetal.CountryOfOrigin), itemPetal.CountryOfOrigin },
                { nameof(itemPetal.Size), itemPetal.Size },
                { nameof(itemPetal.Color), itemPetal.Color },
                { nameof(itemPetal.Brand), itemPetal.Brand },
                { nameof(itemPetal.FabricContent), itemPetal.FabricContent },
                { nameof(itemPetal.FabricComposition), itemPetal.FabricComposition },
                { nameof(itemPetal.Gender), itemPetal.Gender },
                { nameof(itemPetal.InventorySyncFlag), itemPetal.InventorySyncFlag }
            };

            var missingStringFields = requiredStringFields
                .Where(field => string.IsNullOrWhiteSpace(field.Value))
                .Select(field => field.Key)
                .ToList();

            var requiredNumericFields = new Dictionary<string, double?>
                                 {
                                    { nameof(itemPetal.Price), itemPetal.Price },
                                    { nameof(itemPetal.LandedCost), itemPetal.LandedCost }
                                };

            var missingNumericFields = requiredNumericFields
                .Where(field => !field.Value.HasValue)
                .Select(field => field.Key)
                .ToList();

            var missingFields = missingStringFields.Concat(missingNumericFields).ToList();

            errors = missingFields.Any() ? $"Missing required fields: {string.Join(", ", missingFields)}" : null;

            return !missingFields.Any();
        }

        private ItemMasterSourceLog CreateItemMasterSourceLog(ItemPetal itemPetal, bool isValid, string errors)
        {
            return new ItemMasterSourceLog
            {
                Sku = itemPetal.Sku,
                SourceModel = JsonSerializer.Serialize(itemPetal),
                ValidationStatus = isValid ? Status.Valid : Status.Invalid,
                Errors = errors,
                IsSentToSqs = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        public UnifiedItemModel MapToUnifiedItemMaster(ItemPetal itemPetal)
        {
            return new UnifiedItemModelBuilder()
                .WithBasicInfo(itemPetal)
                .WithHts(itemPetal)
                .WithBarcode(itemPetal)
                .WithPrices(itemPetal)
                .WithCosts(itemPetal)
                .WithCategories(itemPetal)
                .WithAttributes(itemPetal)
                .WithLinks(itemPetal)
                .WithImages(itemPetal)
                .WithDates(itemPetal)
                .Build();
        }
    }
}