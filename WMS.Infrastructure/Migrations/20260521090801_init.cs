using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: true),
                    ExpectedDelivery = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkuCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    TenantInfo = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TaxNumber = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ZoneCode = table.Column<string>(type: "text", nullable: false),
                    TotalLocations = table.Column<int>(type: "integer", nullable: false),
                    UsedLocations = table.Column<int>(type: "integer", nullable: false),
                    ZoneType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthProvider = table.Column<string>(type: "text", nullable: false),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    FacebookId = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    XId = table.Column<string>(type: "text", nullable: true),
                    XUsername = table.Column<string>(type: "text", nullable: true),
                    MicrosoftId = table.Column<string>(type: "text", nullable: true),
                    MicrosoftTenantId = table.Column<string>(type: "text", nullable: true),
                    LinkedInId = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_users_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ZoneCode = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationEntity_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    KeyValues = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: false),
                    NewValues = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inbound_orders_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inbound_orders_users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Jti = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "text", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    SkuCode = table.Column<string>(type: "text", nullable: true),
                    CategoryName = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ZoneName = table.Column<string>(type: "text", nullable: true),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationName = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_items_LocationEntity_LocationId",
                        column: x => x.LocationId,
                        principalTable: "LocationEntity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_inventory_items_skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "skus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_inventory_items_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "inbound_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inbound_items_inbound_orders_InboundOrderId",
                        column: x => x.InboundOrderId,
                        principalTable: "inbound_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inbound_items_inventory_items_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbound_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboundOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PickedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outbound_items_inventory_items_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_outbound_items_outbound_orders_OutboundOrderId",
                        column: x => x.OutboundOrderId,
                        principalTable: "outbound_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_DeletedAt",
                table: "audit_logs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_TenantId",
                table: "audit_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_DeletedAt",
                table: "categories",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_categories_TenantId",
                table: "categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_TenantId_Slug",
                table: "categories",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_DeletedAt",
                table: "customers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_customers_TenantId",
                table: "customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_items_DeletedAt",
                table: "inbound_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_items_InboundOrderId",
                table: "inbound_items",
                column: "InboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_items_InventoryItemId",
                table: "inbound_items",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_items_TenantId",
                table: "inbound_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_CreatorId",
                table: "inbound_orders",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_DeletedAt",
                table: "inbound_orders",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_OrderNumber",
                table: "inbound_orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_SupplierId",
                table: "inbound_orders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_TenantId",
                table: "inbound_orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_DeletedAt",
                table: "inventory_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_LocationId",
                table: "inventory_items",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_SkuId",
                table: "inventory_items",
                column: "SkuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_Status",
                table: "inventory_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_TenantId",
                table: "inventory_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_ZoneId",
                table: "inventory_items",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationEntity_ZoneId",
                table: "LocationEntity",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_items_DeletedAt",
                table: "outbound_items",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_items_InventoryItemId",
                table: "outbound_items",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_items_OutboundOrderId",
                table: "outbound_items",
                column: "OutboundOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_items_TenantId",
                table: "outbound_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_orders_DeletedAt",
                table: "outbound_orders",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_orders_ShipmentNumber",
                table: "outbound_orders",
                column: "ShipmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbound_orders_TenantId",
                table: "outbound_orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_DeletedAt",
                table: "outbox_messages",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_TenantId",
                table: "outbox_messages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_DeletedAt",
                table: "refresh_tokens",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Jti",
                table: "refresh_tokens",
                column: "Jti");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TenantId",
                table: "refresh_tokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_DeletedAt",
                table: "roles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_roles_TenantId",
                table: "roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_skus_DeletedAt",
                table: "skus",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_skus_TenantId",
                table: "skus",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_skus_TenantId_CategoryId",
                table: "skus",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_skus_TenantId_SkuCode",
                table: "skus",
                columns: new[] { "TenantId", "SkuCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_DeletedAt",
                table: "suppliers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_TenantId",
                table: "suppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_DeletedAt",
                table: "tenants",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_TenantId",
                table: "tenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_users_AuthProvider",
                table: "users",
                column: "AuthProvider");

            migrationBuilder.CreateIndex(
                name: "IX_users_DeletedAt",
                table: "users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email_AuthProvider",
                table: "users",
                columns: new[] { "Email", "AuthProvider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_FacebookId",
                table: "users",
                column: "FacebookId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_GoogleId",
                table: "users",
                column: "GoogleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_LinkedInId",
                table: "users",
                column: "LinkedInId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_MicrosoftId",
                table: "users",
                column: "MicrosoftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_MicrosoftTenantId",
                table: "users",
                column: "MicrosoftTenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                table: "users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId",
                table: "users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_users_XId",
                table: "users",
                column: "XId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zones_DeletedAt",
                table: "zones",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_zones_TenantId",
                table: "zones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_zones_ZoneCode",
                table: "zones",
                column: "ZoneCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "inbound_items");

            migrationBuilder.DropTable(
                name: "outbound_items");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "inbound_orders");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "outbound_orders");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "LocationEntity");

            migrationBuilder.DropTable(
                name: "skus");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "zones");
        }
    }
}
