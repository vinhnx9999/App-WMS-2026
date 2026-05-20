Master Data Sync

1	Odoo Product → WMS InventoryItem
	Pull product.product (type=product, active=true). Map default_code → SKU, barcode → Barcode, list_price → UnitPrice. Sync mỗi giờ.
2	Odoo Partner (supplier_rank > 0) → WMS Supplier
	Pull partners có supplier_rank > 0. Map name, phone, email, address.
3	Odoo Partner (customer_rank > 0) → WMS Partner
	Pull partners có customer_rank > 0. Dùng cho outbound delivery.
4	Odoo Stock Location → WMS Zone
	Pull stock.location (usage=internal). Map location → Zone. Hierarchical: WH/Stock → Zone mapping.


========

Purchase Order — Inbound Flow
Flow: Odoo PO → WMS Receive → Odoo GR

1	Odoo tạo Purchase Order (state=purchase)
	PO confirmed trong Odoo Purchase module. purchase.order state chuyển sang purchase (đã confirm). Odoo tự tạo stock.picking (incoming).
2	WMS pull pickings (state=assigned, picking_type_code=incoming)
	Scheduled job query stock.picking với filter: [('picking_type_code','=','incoming'), ('state','=','assigned')]. Tạo InboundOrder trong WMS.
3	WMS: Keeper nhận hàng, quét barcode, đếm qty
	Thủ kho thực tế nhận hàng. Ghi nhận received quantities trong WMS. InboundOrder → Completed.
4	WMS → Odoo: Update move lines + button_validate()
	a) write() lên stock.move.line — set qty_done cho từng line. 
	b) Call button_validate() trên stock.picking. Odoo tạo stock.move + cập nhật stock.quant + tạo account.move (nếu có Accounting).

========

Sales / Delivery — Outbound Flow
Flow: Odoo SO → WMS Pick/Ship → Odoo Delivery

1	Odoo Sales Order confirmed → tạo Delivery (stock.picking)
	sale.order confirm → Odoo tự tạo stock.picking (outgoing) + stock.move lines.
2	WMS pull outgoing pickings
	Query stock.picking: [('picking_type_code','=','outgoing'), ('state','=','assigned')]. Tạo OutboundOrder trong WMS.
3	WMS: Pick, Pack, Ship
	Keeper picking theo location. Manager confirm ship. OutboundOrder → Shipped.
4	WMS → Odoo: Update move lines + button_validate()
	Set qty_done trên stock.move.line, rồi call button_validate(). Odoo trừ stock, tạo invoice (nếu auto-invoice enabled).


========


Stock Synchronization

Scenario				Direction			Frequency			Method
Stock quant check		Odoo → WMS			Every 15 min		stock.quant.search_read
GR confirmation			WMS → Odoo			Real-time			picking.button_validate()
Delivery confirmation	WMS → Odoo			Real-time			picking.button_validate()
Inventory adjustment	WMS → Odoo			On demand			stock.quant.write({inventory_quantity: x}) → action_apply_inventory()
Stock reconciliation	Bidirectional		Daily (night)		Batch compare WMS qty vs Odoo quant

========

# Step 1: Find the quantity
{
  "method": "execute_kw",
  "args": ["db", uid, pwd, "stock.quant", "search_read",
    [[
      ["product_id.default_code", "=", "SAM-S24U"],
      ["location_id", "=", 8]  // internal location id
    ]],
    {"fields": ["id", "quantity", "reserved_quantity"]}
  ]
}

# Step 2: Set inventory_quantity (the count you want)
{
  "method": "execute_kw",
  "args": ["db", uid, pwd, "stock.quant", "write",
    [[42],  // quant id
    {"inventory_quantity": 500}]  // new count
  ]
}

# Step 3: Apply the adjustment
{
  "method": "execute_kw",
  "args": ["db", uid, pwd, "stock.quant", "action_apply_inventory",
    [[42]]  // quant id
  ]
}

