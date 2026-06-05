# Warehouse Management System

This context describes warehouse concepts used to identify, describe, and protect goods handled by the system.

## Language

**Tenant**:
A tenant is the business ownership boundary for product catalog data. Product, SKU, category, specification, and unit-of-measure codes are unique within a tenant, not across the whole system.
_Avoid_: Global catalog

**Product**:
A product is an aggregate root that represents shared catalog identity and information. A product may have one or more SKUs, but it does not own the SKU lifecycle.
_Avoid_: Item, goods

**Product Code**:
A product code is the stable business identifier used to group imported SKUs under a product. When users do not provide one, the system generates a sequence-based code.
_Avoid_: SKU code, item code

**Category**:
A category is a reusable business grouping assigned to a product.
_Avoid_: SKU category

**SKU**:
A SKU is an aggregate root and the user-facing stockable warehouse unit that operations can receive, store, count, pick, and ship. A SKU must reference one product, must have a SKU code, may have a display name, may be assigned allowed units of measure, and may carry a reference price.
_Avoid_: Product code, item code

**Unit of Measure**:
A unit of measure is a warehouse handling unit that may be allowed for a SKU. Unit conversion is not part of the product language.
_Avoid_: Product unit

**SKU Attribute**:
A SKU attribute describes a characteristic that distinguishes one SKU from another under the same product.
_Avoid_: SKU specification
