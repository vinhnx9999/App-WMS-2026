using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Entities.ErpSync;
using WMS.Domain.Entities.InboundOrderAggregateRoot;
using WMS.Domain.Entities.InventoryAggregateRoot;
using WMS.Domain.Entities.Master;
using WMS.Domain.Entities.Outbound;
using WMS.Domain.Entities.PalletAggregateRoot;
using WMS.Domain.Entities.ProductAggregateRoot;
using WMS.Domain.Entities.RuleAggregateRoot;
using WMS.Domain.Entities.Security;
using WMS.Domain.Entities.SkuAggregateRoot;
using WMS.Domain.Entities.WarehouseAggregateRoot;
using WMS.Domain.Interfaces;
using WMS.Domain.Entities.InboundWorkflowConfigAggregateRoot;
using WMS.Domain.Entities.InboundReceiptAggregateRoot;
using WMS.Domain.Entities.QcInspectionAggregateRoot;
using WMS.Domain.Entities.PutawayTaskAggregateRoot;
using WMS.Domain.Entities.GoodsReceiptNoteAggregateRoot;
using WMS.Domain.Entities.InboundOrderHistoryAggregateRoot;

namespace WMS.Infrastructure.Persistence;

public class WmsDbContext(

    DbContextOptions<WmsDbContext> options,
    ICurrentUser currentUser,
    IMediator mediator) :
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
    public DbSet<Pallet> Pallets => Set<Pallet>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<SkuAttribute> Specifications => Set<SkuAttribute>();
    public DbSet<SkuAttributeValue> SkuSpecifications => Set<SkuAttributeValue>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<SkuUnitOfMeasure> SkuUnitOfMeasures => Set<SkuUnitOfMeasure>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseArea> WarehouseAreas => Set<WarehouseArea>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<LocationEntity> Locations => Set<LocationEntity>();
    public DbSet<WarehouseRuleSetting> WarehouseRuleSettings => Set<WarehouseRuleSetting>();
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
    public DbSet<InboundWorkflowConfig> InboundWorkflowConfigs => Set<InboundWorkflowConfig>();
    public DbSet<InboundWorkflowStep> InboundWorkflowSteps => Set<InboundWorkflowStep>();
    public DbSet<InboundReceipt> InboundReceipts => Set<InboundReceipt>();
    public DbSet<InboundReceiptItem> InboundReceiptItems => Set<InboundReceiptItem>();
    public DbSet<QcInspection> QcInspections => Set<QcInspection>();
    public DbSet<QcInspectionItem> QcInspectionItems => Set<QcInspectionItem>();
    public DbSet<PutawayTask> PutawayTasks => Set<PutawayTask>();
    public DbSet<PutawayTaskItem> PutawayTaskItems => Set<PutawayTaskItem>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    public DbSet<GoodsReceiptNoteItem> GoodsReceiptNoteItems => Set<GoodsReceiptNoteItem>();
    public DbSet<InboundOrderHistory> InboundOrderHistories => Set<InboundOrderHistory>();


    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Ignore<DomainEvent>();
        mb.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
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

        // Dispatch domain events before saving to database.
        // We use a loop because event handlers might enqueue new domain events in the tracker.
        while (true)
        {
            var domainEntities = ChangeTracker.Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents.Any())
                .ToList();

            if (!domainEntities.Any())
            {
                break;
            }

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            // Clear events immediately to prevent infinite loops if handlers trigger more updates
            foreach (var entity in domainEntities)
            {
                entity.Entity.ClearEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent, ct);
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}
