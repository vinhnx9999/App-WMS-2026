using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.Inbound;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.Product;
using WMS.Domain.Entities.Security;
using WMS.Domain.Entities.Warehouses;
using WMS.Domain.Interfaces;

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
    // public DbSet<User> Users { get; set; } = null!;
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
    public DbSet<CodeSequence> CodeSequences => Set<CodeSequence>();
    public DbSet<SkuImportSession> SkuImportSessions => Set<SkuImportSession>();
    public DbSet<SkuImportRow> SkuImportRows => Set<SkuImportRow>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Ignore<DomainEvent>();
        mb.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var user = _currentUser.Email;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(BaseEntity.CreatedBy)).CurrentValue = user;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(BaseEntity.UpdatedBy)).CurrentValue = user;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
