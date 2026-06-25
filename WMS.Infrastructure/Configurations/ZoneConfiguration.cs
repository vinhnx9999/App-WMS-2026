using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class ZoneConfiguration : BaseEntityConfiguration<Zone>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Zone> builder)
    {
        builder.Property(b => b.Name).IsRequired();
        builder.HasIndex(x => x.ZoneCode).IsUnique();

        builder.ToTable("zones");
    }
}