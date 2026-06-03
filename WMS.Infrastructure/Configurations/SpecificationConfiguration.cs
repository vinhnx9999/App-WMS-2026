using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class SpecificationConfiguration : BaseEntityConfiguration<SkuAttribute>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SkuAttribute> builder)
    {
        builder.ToTable("specifications");


    }
}
