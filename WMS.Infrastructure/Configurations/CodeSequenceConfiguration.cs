using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations
{
    public class CodeSequenceConfiguration : BaseEntityConfiguration<CodeSequence>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CodeSequence> builder)
        {
            builder.ToTable("code_sequences");

            builder.Property(x => x.RowVersion).IsConcurrencyToken();

            builder.HasIndex(x => new { x.TenantId, x.CodeType }).IsUnique();

            builder.HasIndex(x => x.Prefix);
        }
    }
}
