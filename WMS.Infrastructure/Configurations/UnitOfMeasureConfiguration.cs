using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Configurations;

public class UnitOfMeasureConfiguration : BaseEntityConfiguration<UnitOfMeasure>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measures");


    }
}
