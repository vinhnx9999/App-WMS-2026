using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Product;
using WMS.Domain.Entities.Security;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Configurations;

namespace WMS.Infrastructure.Persistence;

public class WmsDbContext(DbContextOptions<WmsDbContext> options, ICurrentUser currentUser) :
    IdentityDbContext<IdentityUser>(options)
{
    //, AuditInterceptor auditInterceptor
    //private readonly AuditInterceptor _auditInterceptor = auditInterceptor;
    private readonly ICurrentUser _currentUser = currentUser;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.AddInterceptors(_auditInterceptor);
    }
    public DbSet<Tenant> Tenants => Set<Tenant>();
    //public DbSet<User> Users { get; set; } = null!;
    //public DbSet<Role> Roles => Set<Role>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<SkuAttribute> Specifications => Set<SkuAttribute>();
    public DbSet<SkuAttributeValue> SkuSpecifications => Set<SkuAttributeValue>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<SkuUnitOfMeasure> SkuUnitOfMeasures => Set<SkuUnitOfMeasure>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundItem> InboundItems => Set<InboundItem>();
    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundItem> OutboundItems => Set<OutboundItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<ErpSyncLog> ErpSyncLogs => Set<ErpSyncLog>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Ignore<DomainEvent>();
        // mb.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
        new TenantConfiguration().Configure(mb.Entity<Tenant>());
        new UserConfiguration().Configure(mb.Entity<User>());
        new RoleConfiguration().Configure(mb.Entity<Role>());
        new ZoneConfiguration().Configure(mb.Entity<Zone>());
        new CategoryConfiguration().Configure(mb.Entity<Category>());
        new SkuConfiguration().Configure(mb.Entity<Sku>());
        new SpecificationConfiguration().Configure(mb.Entity<SkuAttribute>());
        new SkuSpecificationConfiguration().Configure(mb.Entity<SkuAttributeValue>());
        new UnitOfMeasureConfiguration().Configure(mb.Entity<UnitOfMeasure>());
        new SkuUnitOfMeasureConfiguration().Configure(mb.Entity<SkuUnitOfMeasure>());
        new InventoryConfiguration().Configure(mb.Entity<InventoryItem>());
        new SupplierConfiguration().Configure(mb.Entity<Supplier>());
        new CustomerConfiguration().Configure(mb.Entity<Customer>());
        new InboundConfiguration().Configure(mb.Entity<InboundOrder>());
        new InboundItemConfiguration().Configure(mb.Entity<InboundItem>());
        new OutboundConfiguration().Configure(mb.Entity<OutboundOrder>());
        new OutboundItemConfiguration().Configure(mb.Entity<OutboundItem>());
        new RefreshTokendConfiguration().Configure(mb.Entity<RefreshToken>());
        new AuditLogConfiguration().Configure(mb.Entity<AuditLog>());
        new OutboxMessageConfiguration().Configure(mb.Entity<OutboxMessage>());
        new WebhookEventConfiguration().Configure(mb.Entity<WebhookEvent>());
        new ErpSyncLogConfiguration().Configure(mb.Entity<ErpSyncLog>());
        new ProductConfiguration().Configure(mb.Entity<Product>());
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
                entry.Property(nameof(BaseEntity.UpdatedBy)).CurrentValue = _currentUser.Email;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
