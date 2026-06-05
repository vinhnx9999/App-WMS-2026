using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Common;

namespace WMS.Infrastructure.Configurations;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ConfigureBase(builder);
        ConfigureEntity(builder);
    }

    protected virtual void ConfigureBase(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.CreatedAt);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy).HasMaxLength(100);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.DeletedAt);
        builder.HasIndex(e => e.IsDeleted);

        builder.Ignore(e => e.DomainEvents);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
