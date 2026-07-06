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

## PO List UI & Workflow Decisions

**PO List UI**:
The frontend SPA implements the Purchase Order (PO) list page using AG Grid Community, configured with the `infinite` row model for server-side pagination, search, and sorting. It maps UI search keywords (debounced) and column sorting (`params.sortModel`) to the backend `SearchInboundOrdersQuery`.
_Avoid_: Client-side sorting/filtering for large datasets.

**PO Detail Panel**:
Row details for an Inbound Order are displayed in a sliding side panel (Sheet component from shadcn UI). When opened, it fetches detailed items by calling the Get Inbound By ID endpoint (`/inbound/{id}`), returning `InboundItemDetailDto` details with nullable `SkuCode`, `SkuName`, and `SupplierName` (retrieved via ID dictionaries in the Query Handler). The sheet displays items in an optimized vertical card-like listing with loading skeletons and error retry support.
_Avoid_: Direct database joins between separate aggregate roots in writing commands; use ID-based query joins in the read-model Query Handler. Also avoid AG Grid Enterprise nested grid features (due to Community edition limits).

**Inbound Workflow Navigation**:
The parent `InboundPage.tsx` component maintains the active workflow step and active receipt contexts. Selecting a PO for receiving in Step 1 (PO Step) calls the API to create a new `InboundReceipt` in `Receiving` status and automatically transitions to the `RECEIVE` step, loading the `ReceiveWork` (Full-Screen Detail Work View) for that receipt.
_Avoid_: Requiring manual stepper clicks to advance after selecting a PO, or directly displaying PO details in the Receive step.

## Inbound UI & Workflow Decisions

**Inbound Workspaces**:
* Inbound screens are designed as independent pages (PO, Receive, QC, Putaway) to support specific warehouse roles (Purchasing, Dock Unloaders, QC Inspectors, Forklift Operators).
* Operators can handle multiple POs or batches simultaneously without workflow locks.

**Full-Screen Work View Layout**:
* *Read-only Views*: Detail sheets like PO details are displayed in a sliding side panel (shadcn UI Sheet) to keep navigation clean.
* *Data-entry Views*: Active operational steps (Receive/QC/Putaway details) transition to a **Full-Screen Work View** utilizing the full width of the screen. This optimizes AG Grid spacing for inline row-level data entry.
* *Receive step division*: `ReceiveStep` is a Master Grid showing `InboundReceipt` entities (both `Receiving` and `Completed`). Clicking a `Receiving` receipt navigates to the full-screen `ReceiveWork` detail workspace for inline counting and barcode scanning.

**Pallet Code & Consolidation**:
* Support scanning pre-printed pallet labels or auto-generating pallet codes on the UI. Gaining pallet codes is optional during Receive/QC (allowing loose count) and can be assigned during Putaway.
* Scanning an existing physical Pallet Code in Putaway auto-fills and locks the Target Location to the pallet's current shelf location (consolidation), validating backend constraints (`IsMixSku`, `MaxQtyInPallet`).

**No Draft Table (Immediate Putaway Confirmation)**:
* Removed the Draft Table concept from Putaway. Confirming a row directly commits it to the database, updating inventory and generating the GRN instantly, avoiding data loss if the device disconnects mid-session.

**Partial & Over-Receiving**:
* A PO supports multiple receiving shipments over time. The `ReceiveWork` workspace displays PO Qty, Previously Received, and Current Received.
* Supports over-receiving checks against the `overReceiveTolerancePercentage` configuration.
* Provides a "Force Complete" action for managers to manually close a PO when a supplier short-ships and will not deliver the remainder.
