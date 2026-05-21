using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class CategoryConfiguration : BaseEntityConfiguration<Category>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Slug)
            .HasMaxLength(250);

        /// Can't have 2 slug with same tenant
        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique();

        builder.Ignore(e => e.Items);
    }
}
