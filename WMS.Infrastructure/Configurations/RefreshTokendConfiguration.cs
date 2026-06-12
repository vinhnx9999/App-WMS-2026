using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Security;

namespace WMS.Infrastructure.Configurations;

public class RefreshTokendConfiguration : BaseEntityConfiguration<RefreshToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(b => b.Token).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => x.Jti);

        builder
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("refresh_tokens");
    }
}