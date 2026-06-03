using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SkuConfiguration : BaseEntityConfiguration<Sku>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");

    }
}
