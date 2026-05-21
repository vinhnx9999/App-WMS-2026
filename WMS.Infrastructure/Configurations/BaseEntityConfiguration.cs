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
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.DeletedAt);

        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.IsDeleted);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
