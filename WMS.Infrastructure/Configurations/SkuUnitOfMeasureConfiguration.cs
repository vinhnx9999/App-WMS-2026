using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.SkuAggregateRoot;

namespace WMS.Infrastructure.Configurations;

public class SkuUnitOfMeasureConfiguration : BaseEntityConfiguration<SkuUnitOfMeasure>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuUnitOfMeasure> builder)
    {
        builder.ToTable("sku_unit_of_measures");

    }
}
