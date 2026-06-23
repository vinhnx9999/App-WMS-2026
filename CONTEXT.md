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

