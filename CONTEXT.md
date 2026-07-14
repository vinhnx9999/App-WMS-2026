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

## Inventory

**InventoryItem**:
An independent aggregate root identifying a specific quantity of physical goods in the warehouse. It is uniquely identified by the combination of seven attributes: `SkuId` (Product SKU), `LocationId` (Physical Location), `SupplierId` (Supplier), `SerialNumber` (Serial Number), `PalletId` (Pallet), `ExpiryDate` (Expiry Date), and `LotNumber` (Lot Number). For serial-tracked items, the quantity is always 1, and duplicate active serial numbers within the warehouse are prohibited.
_Avoid_: Stock line, inventory row

**Pallet**:
A physical handling unit representing a pallet in the warehouse. An inventory line can belong to a specific Pallet (via PalletId) or contain loose items (PalletId = null). A pallet is identified by a unique PalletCode (also known as PLN) and carries optional attributes such as material (Wood, Plastic, Steel), weight, dimensions (length, width, height), and maximum load capacity. Pallets can restrict SKU mixing (`IsMixSku`) and enforce maximum quantity constraints (`MaxQtyInPallet` configured at the SKU level).
_Avoid_: LPN, pallet unit

**Available Quantity**:
The actual quantity of goods available for outbound operations or stock transfers. When the status is `Available`, it is computed as: `Quantity` (physical quantity) - `AllocatedQuantity` (reserved quantity). If the status is `Hold` or `OutOfStock`, the available quantity is always 0.
_Avoid_: Free stock

**Allocated Quantity**:
The quantity of goods in the inventory line that has been reserved for pending outbound orders.
_Avoid_: Reserved quantity

**Putaway Date**:
The timestamp when a physical stock line was registered at its location. Used for FIFO/LIFO picking strategy calculations.
_Avoid_: Inward date, receive timestamp

**Expiry Date**:
The date after which the goods should not be sold or shipped. Used for FEFO picking strategy calculations.
_Avoid_: Expiration date, best before date

## Inbound Workflow

**InboundOrder**:
An aggregate root representing an order placed for goods to be received into the warehouse (commonly referred to as a PO). It encapsulates items, totals, dates, and status transitions. Its status lifecycle is simplified to: `Approved` (initial default state for both ERP-synced and manually created POs), `Receiving` (active receiving in progress), `Completed` (fully received or manually closed), and `Cancelled`. To enforce domain integrity, its properties cannot be modified directly from outside, and its constructor is private, exposing creation through static factory methods. Supplier association is handled at the item level rather than at the order level.
_Avoid_: Purchase Order, PO, Inbound Shipment, Pending status (obsolete)

**InboundItem**:
A child entity owned and managed exclusively by the `InboundOrder` aggregate root. It represents a specific SKU quantity to be received, along with its optional `SupplierId`, allowing an order to consist of items from different suppliers. It cannot be instantiated or modified directly from outside the aggregate boundary.
_Avoid_: Order item, receipt line

**InboundWorkflowConfig**:
An aggregate root configuring the sequence of inbound steps for a given warehouse, supplier, or category combination, resolved using a priority fallback hierarchy.
_Avoid_: Inbound routing config, step mapping

**InboundWorkflowStep**:
A child entity representing a specific stage in the inbound workflow sequence. Values: `PO` (Purchase Order planning), `Receive` (Gate/dock receiving), `QC` (Quality inspection), and `Putaway` (Physical stock placement).
_Avoid_: Inbound phase, workflow stage

**InboundReceipt**:
An aggregate root recording the physical receipt of goods at the loading dock, supporting partial shipments and enforcing over-receiving policies based on the resolved workflow config. Its status lifecycle consists only of `Receiving` (active counting session) and `Completed` (finalized and sent for downstream steps).
_Avoid_: Gate receipt, receiving report, Draft status (obsolete)

**QcInspection**:
An aggregate root representing a quality inspection session, tracking `PassedQuantity` and `FailedQuantity` for SKU lines.
_Avoid_: QC check, inspection form

**PutawayTask**:
An aggregate root representing the assignment to move received or inspected items to storage locations.
_Avoid_: Stock movement task, placement order

**GoodsReceiptNote (GRN)**:
An aggregate root representing the finalized receipt record created automatically upon completing a putaway task. Generating a GRN updates inventory stock lines and triggers ERP sync. In physical operations, this represents the official, legally binding "Phiếu nhập kho" (Goods Receipt Note) indicating inventory is on the shelf and available for use, whereas InboundReceipt is only a temporary gate-dock receipt.
_Avoid_: Receiving voucher, completed receipt record

**InboundOrderHistory**:
A timeline audit log tracking individual workflow milestones, including the user, timestamp, state transitions, and item quantities.
_Avoid_: Order logs, workflow audit trail

## WCS Integration (Obsolete)

The WCS robot confirmation wait state (`SentToWcs`) and deferred inventory updates have been abandoned. The system now updates inventory immediately upon putaway task confirmation. The entities `WcsTask`, `WcsSubTask`, and `WcsSubTaskHistory` are no longer utilized.

## Inbound UI & Workflow Decisions

**Independent Pages & Routing**:
The inbound module is strictly separated into independent pages via nested routes (e.g., `/inbound/po`, `/inbound/po/:id`, `/inbound/receive`, `/inbound/receive/:id`).
_Avoid_: Single-page stepper navigations where all steps share the same URL. Navigation between modules should happen via a Sidebar menu, which dynamically renders links based on enabled workflow steps. Steppers are only used as contextual read-only indicators or in the main `/inbound` Dashboard.

**Workflow Auto-Generation & Creation**:
* A step can only manually trigger "Create" (e.g., creating a new PO, new Receipt, new Putaway Task) if it has no preceding enabled steps in the configuration. 
* Data flows forward automatically: completing a Receipt auto-generates a `QcInspection` (if QC is enabled) or a `PutawayTask`. Completing a QC step auto-generates a `PutawayTask` for passed items.
_Avoid_: "Direct Mode" terminology. Instead, use explicit checks for preceding enabled steps to conditionally show creation UI.

**Layout Architecture (List -> Detail)**:
* Each inbound step follows a strict Master-Detail pattern. The List view uses AG Grid to display entities.
* Selecting an entity transitions to a dedicated Detail route (e.g., `/inbound/po/:id`) which utilizes the full screen for an AG Grid data entry interface.
* Read-only metrics (like PO receiving progress) are shown directly on the PO Detail page.
_Avoid_: Sliding side panels (Sheets) for detail views. All detail views should be full pages to maximize AG Grid real estate.

**Data Persistence & Drafts**:
* Heavy data-entry screens (Receive Detail, QC Detail) DO NOT utilize background auto-saving or draft mechanisms. 
* Operators must complete the counting session in one go. An "Unsaved Changes" browser warning is applied to prevent accidental navigation or refresh.
_Avoid_: Complex draft state management APIs or local storage syncing for counting sessions.

**Putaway Map Integration**:
* The Putaway Detail page utilizes AG Grid as its primary interface for locations. The visual warehouse map (Konva canvas) is secondary and accessed via a modal or drawer when the operator needs spatial assistance.

**Pallet Code & Consolidation**:
* Support auto-generating or assigning pallet codes optionally during Receive/QC.
* Assigning to an existing physical Pallet Code in Putaway auto-fills and locks the Target Location to the pallet's current shelf location, validating backend constraints (`IsMixSku`, `MaxQtyInPallet`).

**Partial & Over-Receiving**:
* PO supports multiple receiving shipments over time. PO Detail displays PO Qty, Previously Received, and Remaining Qty.
* Over-receiving is checked against `overReceiveTolerancePercentage`. Managers can use "Force Complete" to manually close a short-shipped PO.

## Create InboundOrder (PO) UI Decisions

**InboundOrder Create Form Fields**:
The Create PO form contains: `expectedDate` (optional), `notes` (optional), and a list of `InboundItem` rows. Each item row contains `skuId` (required), `quantity` (required, integer > 0), and `supplierId` (optional, per-item). Fields such as `expiryDate`, `lotNumber`, and `serialNumber` are NOT collected at PO creation time — they belong to the Receive step when physical goods arrive.
_Avoid_: Collecting lot/expiry/serial at PO creation stage.

**InboundItem Supplier Scope**:
Each `InboundItem` within an `InboundOrder` holds its own `supplierId`, allowing a single PO to reference items from multiple different suppliers. Supplier is NOT a header-level attribute on the InboundOrder itself.
_Avoid_: Single supplier per PO, header-level supplierId on InboundOrder.

**InboundItem Duplicate Rule**:
Within a single `InboundOrder`, duplicate line items are only blocked when both `skuId` AND `supplierId` match. An item with the same SKU but a different Supplier is considered a distinct line and is always permitted.
_Avoid_: Blocking duplicates by `skuId` alone.

**Create PO Post-Submit Navigation**:
After successfully creating an `InboundOrder`, the UI navigates directly to the PO Detail page (`/inbound/po/:id`) using the `id` returned in `CreatePoResponse`. This lets operators immediately verify content and trigger receiving.
_Avoid_: Staying on the create form or navigating to the list after successful creation.

**Create PO Dirty-State Guard**:
The Create PO form uses `react-hook-form`'s `formState.isDirty` to guard accidental navigation. The Back button shows a confirmation `AlertDialog` only when the form has been modified. If the form is untouched (clean state), navigation proceeds immediately.
_Avoid_: Always-on confirmation dialogs or custom dirty-state tracking.
