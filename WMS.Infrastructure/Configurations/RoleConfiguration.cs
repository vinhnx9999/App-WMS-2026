using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using WMS.Domain.Entities.Security;

namespace WMS.Infrastructure.Configurations;

public class RoleConfiguration : BaseEntityConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.Property(b => b.Name).IsRequired();

        // ── Role.Permissions stored as JSON ──
        builder.Property(x => x.Permissions)
          .HasConversion(
              v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
              v => JsonSerializer.Deserialize<Dictionary<string, bool>>(v,
                  (JsonSerializerOptions)null!)!
          );

        builder.ToTable("roles");
    }
}