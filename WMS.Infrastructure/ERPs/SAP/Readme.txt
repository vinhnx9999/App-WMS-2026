Master Data Sync

1	Material Master → WMS Inventory
	SAP Material Master (MM03) sync sang WMS InventoryItem. Chạy scheduled job mỗi giờ hoặc trigger khi SAP thay đổi.
2	Vendor Master → WMS Supplier
	SAP Vendor (XK03) → WMS Supplier. Bao gồm tên, địa chỉ, điều kiện thanh toán.
3	Customer Master → WMS Partner
	SAP Customer (XD03) → WMS Partner. Dùng cho outbound delivery.
4	Plant / Storage Location → WMS Zone
	SAP Plant + SLoc → WMS Zone mapping. Cấu hình trước khi sync.


========

Flow: SAP PO → WMS Receive → SAP GR

1	SAP tạo Purchase Order (ME21N)
	PO được tạo trong SAP MM. Status: Released.
2	WMS pull PO từ SAP (OData / IDoc)
	Scheduled job hoặc webhook pull PO mới. Tạo InboundOrder trong WMS với status Pending.
3	WMS: Receive hàng (Keeper thao tác)
	Keeper quét barcode, đếm số lượng, ghi nhận received quantity. WMS InboundOrder → Completed.
4	WMS post Goods Receipt → SAP (BAPI)
	Gọi BAPI_GOODSMVT_CREATE với move type 101 (GR for PO). SAP tạo Material Document.
5	WMS cập nhật SAP Material Doc Number
	Lưu SAP MaterialDocument vào WMS InboundOrder. Audit log ghi nhận sync status.



========


Flow: SAP Outbound Delivery → WMS Pick/Ship → SAP Goods Issue (SAP GI)

1	SAP tạo Outbound Delivery (VL01N)
	Delivery document được tạo từ Sales Order trong SAP SD.
2	WMS pull Delivery từ SAP
	Tạo OutboundOrder trong WMS. Map Delivery items → OutboundItems.
3	WMS: Pick, Pack, Ship
	Keeper thực hiện picking, packing. Manager confirm ship. OutboundOrder → Shipped.
4	WMS post Goods Issue → SAP (BAPI)
	Gọi BAPI_GOODSMVT_CREATE với move type 201 (GI) hoặc BAPI_DELIVERY_CHANGE để post GI cho delivery.

========

Sync Strategy

Scenario				Direction			Frequency			Method

Stock balance check		SAP → WMS			Every 15 min		OData poll
GR posting				WMS → SAP			Real-time			BAPI call
GI posting				WMS → SAP			Real-time			BAPI call
Stock reconciliation	Bidirectional		Daily (night)		Batch compare
Inventory adjustment	WMS → SAP			On demand			BAPI (mvt type 551/552)

========

SAP Type		Description						WMS Action

101				GR for Purchase Order			Inbound Receive
102				GR Reversal for PO				Inbound Cancel
201				GI for Cost Center				Outbound Ship (internal)
261				GI for Production Order			Outbound Ship (production)
601				GI for Delivery					Outbound Ship (sales)
551				Scrap — Withdrawal				Stock Adjustment (decrease)
552				Scrap — Reversal				Stock Adjustment (increase)
301				Transfer Posting — Plant		Inter-zone transfer
311				Transfer Posting — SLoc			Intra-zone transfer
