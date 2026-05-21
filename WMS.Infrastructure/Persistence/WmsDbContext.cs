using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Security;
using WMS.Infrastructure.Configurations;

namespace WMS.Infrastructure.Persistence;

public class WmsDbContext(DbContextOptions<WmsDbContext> options) :
    IdentityDbContext<IdentityUser>(options)
{
    //, AuditInterceptor auditInterceptor
    //private readonly AuditInterceptor _auditInterceptor = auditInterceptor;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.AddInterceptors(_auditInterceptor);
    }
    public DbSet<Tenant> Tenants => Set<Tenant>();
    //public DbSet<User> Users { get; set; } = null!;
    //public DbSet<Role> Roles => Set<Role>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundItem> InboundItems => Set<InboundItem>();
    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundItem> OutboundItems => Set<OutboundItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        new TenantConfiguration().Configure(mb.Entity<Tenant>());
        new UserConfiguration().Configure(mb.Entity<User>());
        new RoleConfiguration().Configure(mb.Entity<Role>());
        new ZoneConfiguration().Configure(mb.Entity<Zone>());
        new InventoryConfiguration().Configure(mb.Entity<InventoryItem>());
        new InboundConfiguration().Configure(mb.Entity<InboundOrder>());
        new OutboundConfiguration().Configure(mb.Entity<OutboundOrder>());
        new RefreshTokendConfiguration().Configure(mb.Entity<RefreshToken>());
        new AuditLogConfiguration().Configure(mb.Entity<AuditLog>());
        new OutboxMessageConfiguration().Configure(mb.Entity<OutboxMessage>());
        //   new SkuConfiguration().Configure(mb.Entity<Sku>());
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
