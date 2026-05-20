SAP Modules tích hợp

SAP Module	                Component	            Direction	                Data

MM — Materials Mgmt	        Material Master	        SAP → WMS	        Material, UOM, descriptions
MM — Materials Mgmt	        Purchase Order	        SAP → WMS	        PO number, items, quantities
MM — Materials Mgmt	        Goods Receipt	        WMS → SAP	        GR posting after receive
MM — Materials Mgmt	        Goods Issue	            WMS → SAP	        GI posting after ship
MM — Materials Mgmt         Stock Balance	        Bidirectional	    Real-time stock levels
SD — Sales & Dist	        Delivery	            SAP → WMS	        Outbound delivery orders
WM/EWM — Warehouse	        Transfer Order	        Bidirectional	    Putaway, picking confirmations
FI — Finance	            Material Doc	        WMS → SAP	        Valuated goods movements

Kiến trúc tích hợp
  ┌──────────────────────────────────┐
  │         SAP S/4HANA              │
  │  ┌─────┐ ┌─────┐ ┌─────┐         │
  │  │ MM  │ │ SD  │ │ WM  │         │
  │  └──┬──┘ └──┬──┘ └──┬──┘         │
  │     │       │       │            │
  │  ┌──┴───────┴───────┴──┐         │
  │  │   SAP OData / RFC   │         │
  │  │   / BAPI / iDoc     │         │
  │  └──────────┬──────────┘         │
  └─────────────┼────────────────────┘
                │
                │  HTTPS / RFC
                │
  ┌─────────────┼────────────────────┐
  │   SAP Integration Middleware     │
  │  ┌──────────┴───────────┐        │
  │  │  ┌────────────────┐  │        │
  │  │  │ SAP NCo 3.1    │  │        │
  │  │  │ (RFC/BAPI)     │  │        │
  │  │  └────────────────┘  │        │
  │  │  ┌────────────────┐  │        │
  │  │  │ OData Client   │  │        │
  │  │  │ (REST/OData v2)│  │        │
  │  │  └────────────────┘  │        │
  │  │  ┌────────────────┐  │        │
  │  │  │ IDoc Parser    │  │        │
  │  │  │ (File/HTTP)    │  │        │
  │  │  └────────────────┘  │        │
  │  └──────────────────────┘        │
  └──────────────────────────────────┘
                │
                │  Internal API
                │
  ┌─────────────┼────────────────────┐
  │         WMS API (.NET 8/10)      │
  │  ┌──────────┴───────────┐        │
  │  │  Controllers         │        │
  │  │  → SapSyncService    │        │
  │  │  → Background Jobs   │        │
  │  └──────────┬───────────┘        │
  │             │                    │
  │  ┌──────────┴───────────┐        │
  │  │  PostgreSQL + Redis  │        │
  │  └──────────────────────┘        │
  └──────────────────────────────────┘



Protocol	    Khi nào dùng	                        Ưu điểm	                                Nhược điểm
SAP OData	    S/4HANA Cloud, Fiori apps	        REST standard, dễ implement, JSON	        Không cover hết BAPI, cần custom CDS views
BAPI (RFC)	    ECC 6.0+, S/4HANA on-premise	    Đầy đủ business logic, transactional	    Cần SAP NCo library, binary protocol
IDoc	        Async batch processing	            Reliable, retry tự động, batch	            Latency cao, cần PI/PO middleware
SAP REST API	Custom endpoints trên SAP	        Flexible, custom logic	                    Phải develop trên SAP side



# GET Materials from SAP
GET /sap/opu/odata/sap/API_MATERIAL_STOCK_SRV/A_MaterialStock
Authorization: Basic base64(user:password)
Accept: application/json
x-csrf-token: Fetch

# Response 200
{
  "d": {
    "results": [
      {
        "Material": "000000000000001000",
        "Plant": "1000",
        "StorageLocation": "0001",
        "MatlWrhsStQtyInMatlBaseUnit": "342.000",
        "MaterialBaseUnit": "EA"
      }
    ]
  }
}


// BAPI: BAPI_GOODSMVT_CREATE
// Purpose: Post Goods Receipt in SAP after WMS receive

IMPORT:
  GOODSMVT_HEADER
    ├── PSTNG_DATE    = "20250115"      // Posting date
    ├── DOC_DATE      = "20250115"      // Document date
    └── REF_DOC_NO    = "PO-2024-0892"  // WMS order number

  GOODSMVT_ITEM[]:
    [0]
    ├── MATERIAL      = "000000000000001000"
    ├── PLANT         = "1000"
    ├── STGE_LOC      = "0001"
    ├── ENTRY_QNT     = 150
    ├── ENTRY_UOM     = "EA"
    ├── MOVE_TYPE     = "101"          // GR for PO
    └── PO_NUMBER     = "4500001234"   // SAP PO number

EXPORT:
  MATERIALDOCUMENT  = "5000001234"    // SAP Material Doc #
  MATDOCUMENTYEAR   = "2025"

TABLES:
  RETURN[] → [{ TYPE: "S", MESSAGE: "Document posted" }]


// IDoc Type: WMMBXY01 (Goods Movements)
// Direction: WMS → SAP (Outbound from WMS perspective)
// Transfer: File, HTTP, or SAP PI/PO

IDoc Header (EDIDC):
  MESTYP = "WMMBXY"
  IDOCTP = "WMMBXY01"
  SNDPRT = "WMS"
  RCVPRT = "LS"
  RCVPRN = "SAPCLNT100"

Data Records (EDID4):
  Segment E1WMMBXY (Header):
    BLDAT = "20250115"
    BUDAT = "20250115"
    XBLNR = "SHP-4521"
    TCODE = "MB1A"

  Segment E1WMMBXY_ITEM (Line items):
    MATNR = "000000000000001000"
    WERKS = "1000"
    LGORT = "0001"
    BWART = "201"       // GI for cost center
    ERFMG = "45.000"
    ERFME = "EA"
