using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Security;

namespace WMS.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(b => b.Email).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.TenantId).IsUnique();

        builder.HasIndex(x => x.FacebookId).IsUnique();
        builder.HasIndex(x => x.GoogleId).IsUnique();
        builder.HasIndex(x => x.XId).IsUnique();
        builder.HasIndex(x => x.MicrosoftId).IsUnique();
        builder.HasIndex(x => x.MicrosoftTenantId).IsUnique();
        builder.HasIndex(x => x.LinkedInId).IsUnique();

        builder.HasIndex(x => x.AuthProvider);

        // Composite unique cho (email, auth_provider) — tránh conflict
        // khi user có cả local + social account cùng email
        builder.HasIndex(x => new { x.Email, x.AuthProvider }).IsUnique();

        builder.ToTable("users");
    }
}
