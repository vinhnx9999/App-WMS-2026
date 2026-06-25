using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class SkuSpecificationConfiguration : BaseEntityConfiguration<SkuAttributeValue>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuAttributeValue> builder)
    {
        builder.ToTable("sku_specifications");


    }
}
