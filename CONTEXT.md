# Warehouse Management System

This context describes warehouse concepts used to identify, describe, and protect goods handled by the system.

## Language

**Tenant**:
A tenant is the business ownership boundary for product catalog data. Product, SKU, category, specification, and unit-of-measure codes are unique within a tenant, not across the whole system.
_Avoid_: Global catalog

**Product**:
A product is an aggregate root that represents shared catalog identity and information. A product may have one or more SKUs, but it does not own the SKU lifecycle. A product can only be deleted if all its associated SKUs are deleted first. A product can be created without a category, but once it has been assigned a category, it cannot be uncategorized (removed from all categories).
_Avoid_: Item, goods

**Product Code**:
A product code is the stable business identifier used to group imported SKUs under a product. When users do not provide one, the system generates a sequence-based code. Product codes are immutable after creation, unique within a tenant, and soft-deleted products do not release their codes for reuse.
_Avoid_: SKU code, item code

**Category**:
A category is a separate aggregate root representing a reusable business grouping assigned to a product. It has a flat structure, supports a name and an optional description, and does not maintain a direct navigation relationship with inventory items.
_Avoid_: SKU category, hierarchical category

**SKU**:
A SKU is an aggregate root and the user-facing stockable warehouse unit that operations can receive, store, count, pick, and ship. A SKU must reference one product, must have a SKU code, may have a display name, may be assigned allowed units of measure, and may carry a reference price.
_Avoid_: Product code, item code

**Unit of Measure**:
A unit of measure is a warehouse handling unit that may be allowed for a SKU. Unit conversion is not part of the product language.
_Avoid_: Product unit

**SKU Attribute**:
A SKU attribute describes a characteristic that distinguishes one SKU from another under the same product.
_Avoid_: SKU specification

**Supplier**:
A supplier is an independent master-data aggregate root representing a goods provider to the warehouse. A supplier may be soft-deleted freely regardless of related inbound orders; each inbound order retains its own supplier reference as historical data. A deleted supplier may be restored.
_Avoid_: Vendor, Provider

**Supplier Code**:
A supplier code is the stable business identifier for a supplier. Supplier codes are immutable after creation, unique within a tenant, normalized to uppercase, and soft-deleted suppliers do not release their codes for reuse.
_Avoid_: Supplier ID, partner code

**Customer**:
A customer is an independent master-data aggregate root representing a goods receiver from the warehouse. A customer may be soft-deleted freely regardless of related outbound orders; each outbound order retains its own customer reference (via CustomerId) as historical data. A deleted customer may be restored.
_Avoid_: Client, Partner, Receiver

**Customer Code**:
A customer code is the stable business identifier for a customer. Customer codes are immutable after creation, unique within a tenant, normalized to uppercase, and soft-deleted customers do not release their codes for reuse.
_Avoid_: Customer ID, partner code

## Warehouse Physical Structure

**Warehouse**:
The mandatory top-level physical boundary of storage operations. A warehouse has a unique code and address. It may optionally be subdivided into Areas and Blocks, but must always contain at least one Location.
_Avoid_: Storage facility, depot

**WarehouseArea**:
A physical subdivision of a Warehouse representing a named region such as a building wing or aisle group. Every warehouse always has at least one WarehouseArea — a system-provisioned Default Area is created automatically when the first Location without an explicit Area is registered. User-created areas are distinct from the Default Area.
_Avoid_: Section, sector, region

**Block**:
A physical subdivision of a WarehouseArea, typically representing a rack, shelf unit, or floor bay. Every warehouse always has at least one Block — a system-provisioned Default Block is created automatically alongside the Default Area. Every Block requires a parent WarehouseArea.
_Avoid_: Rack, shelf (as domain terms)

**Default Area**:
A system-provisioned WarehouseArea created automatically (IsDefault=true) when the first Location without an explicit Area is registered in a Warehouse. At most one Default Area exists per Warehouse. It is not visible in Area management UI and cannot be targeted by WarehouseRuleSetting. Users interact with it only implicitly via ungrouped Locations.
_Avoid_: System area, implicit area, general area

**Default Block**:
A system-provisioned Block created automatically alongside the Default Area (IsDefault=true). At most one Default Block exists per Warehouse. It always resides in the Default Area. Locations created without an explicit Block are assigned to the Default Block. It is not visible in Block management UI and cannot be targeted by WarehouseRuleSetting.
_Avoid_: System block, implicit block, general block

**Location**:
The mandatory, discrete storage position within a warehouse. A Location always belongs to exactly one Warehouse (WarehouseId), exactly one Block (BlockId — may be the Default Block), and exactly one WarehouseArea (AreaId — may be the Default Area). A Location carries physical coordinates (floor level, row, bay, bin) as scalar attributes. A Location belongs to at most one Zone at a time.
_Avoid_: Bin, slot, cell, position (as standalone domain terms)

**Location Coordinates**:
Physical position attributes on a Location: floor level, row, bay, and bin. These are scalar values stored directly on the Location entity, not separate domain entities or foreign keys.
_Avoid_: Rack coordinates, location address

## Warehouse Logical Structure

**Zone**:
A tenant-scoped master-data logical grouping of Locations representing a business area (e.g. "Cold Storage", "Receiving", "Hazardous"). Zone has no warehouse affiliation — it is reusable across warehouses. A Zone's location count is computed dynamically from Location records; it is never stored on the Zone entity itself.
_Avoid_: Physical zone, warehouse zone (implies containment), area (use WarehouseArea instead)

## Warehouse Rules

**WarehouseRuleSetting**:
A picking-strategy rule scoped to a target (Location, Zone, Block, or Area within a Warehouse) with optional SKU and Supplier filters. A rule carries its own WHERE context (WarehouseId required; LocationId, ZoneId, BlockId, AreaId nullable) rather than being referenced by its target entities. No two rules may share the same combination of (WarehouseId, LocationId, ZoneId, BlockId, AreaId, SkuId, SupplierId).
_Avoid_: Picking policy, warehouse configuration, rule policy

**Picking Strategy**:
The inventory rotation strategy applied when selecting stock for an outbound operation. Values: FIFO (First In First Out), LIFO (Last In First Out), FEFO (First Expired First Out). Defaults to FIFO when no matching rule exists.
_Avoid_: Rotation rule, inventory strategy

**Rule Specificity**:
The computed precedence of a WarehouseRuleSetting, determined by how many dimensions it constrains. Spatial tier precedence is: Location > Zone > Block > Area > Warehouse. Within the same spatial tier, constraining SkuId and SupplierId adds additional specificity. The most specific matching rule wins. Explicit priority integers are not used.
_Avoid_: Rule priority, rule weight

