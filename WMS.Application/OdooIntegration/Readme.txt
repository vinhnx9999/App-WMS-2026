
Odoo ERP
Protocol: JSON-RPC 2.0 + XML-RPC
Auth: Session (cookie), API Key, OAuth2
Data Format: JSON (preferred), XML
Transaction: ORM write() + action_done()
Complexity: Thấp — pure HTTP/JSON
Real-time: JSON-RPC call, webhook (custom)
Cost: Community free, Enterprise license


  ┌─────────────────────────────────────┐
  │          Odoo ERP (v16/v17)         │
  │                                     │
  │  ┌──────────┐  ┌──────────┐         │
  │  │ stock    │  │ purchase │         │
  │  │ picking  │  │ order    │         │
  │  │ quant    │  │ sale     │         │
  │  └────┬─────┘  └────┬─────┘         │
  │       │             │               │
  │  ┌────┴─────────────┴────┐          │
  │  │   Odoo ORM Engine     │          │
  │  └──────────┬────────────┘          │
  │             │                       │
  │  ┌──────────┴────────────┐          │
  │  │  /jsonrpc  endpoint   │          │
  │  │  /xmlrpc/2/object     │          │
  │  │  /xmlrpc/2/common     │          │
  │  └──────────┬────────────┘          │
  └─────────────┼───────────────────────┘
                │
                │  HTTPS / JSON-RPC 2.0
                │
  ┌─────────────┼───────────────────────┐
  │  WMS Integration Layer (.NET 8/10)  │
  │  ┌──────────┴──────────────┐        │
  │  │                         │        │
  │  │  ┌───────────────────┐  │        │
  │  │  │   IErpAdapter     │  │        │
  │  │  │  ┌────────────┐   │  │        │
  │  │  │  │SapAdapter  │   │  │        │
  │  │  │  ├────────────┤   │  │        │
  │  │  │  │OdooAdapter │   │  │        │
  │  │  │  └────────────┘   │  │        │
  │  │  └───────────────────┘  │        │
  │  │                         │        │
  │  │  ┌───────────────────┐  │        │
  │  │  │ OdooJsonRpcClient │  │        │
  │  │  │ OdooSyncService   │  │        │
  │  │  │ Background Jobs   │  │        │
  │  │  └───────────────────┘  │        │
  │  └─────────────────────────┘        │
  │                                     │
  │  ┌───────────────────────┐          │
  │  │ PostgreSQL + Redis    │          │
  │  └───────────────────────┘          │
  └─────────────────────────────────────┘

Protocol	                Endpoint	        Dùng khi	                            Ưu điểm

JSON-RPC 2.0 Recommended	/jsonrpc	        Tất cả CRUD operations	                JSON native, dễ debug, single endpoint
XML-RPC	                    /xmlrpc/2/object	Legacy, fallback	                    Widely supported, stable
External API (OAuth2)	    /api/v1/...	        Enterprise only, REST-like	            RESTful, rate limiting, OAuth2
Webhook	                    Custom module	    Real-time notifications từ Odoo	        Event-driven, no polling


POST /jsonrpc
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "call",
  "params": {
    "service": "common",
    "method": "authenticate",
    "args": [
      "wms_database",           // database name
      "wms_api@company.com",    // username
      "SecurePass123!",         // password
      {}                        // user agent context
    ]
  },
  "id": 1
}

// Response — returns uid (user ID)
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": 42                  // user_id = 42
}


POST /jsonrpc
Content-Type: application/json
Cookie: session_id=abc123...   // optional — for session-based

{
  "jsonrpc": "2.0",
  "method": "call",
  "params": {
    "service": "object",
    "method": "execute_kw",
    "args": [
      "wms_database",   // db
      42,                // uid
      "SecurePass123!", // password
      "product.product", // model
      "search_read",     // method
      [                    // args
        [                  // domain filter
          ["type", "=", "product"],
          ["active", "=", true]
        ]
      ],
      {                    // kwargs
        "fields": ["id", "name", "default_code",
                   "barcode", "list_price",
                   "categ_id", "uom_id"],
        "limit": 100,
        "offset": 0,
        "order": "name asc"
      }
    ]
  },
  "id": 2
}



# Health check
curl https://wms-api.company.com/health

# Manual trigger — sync products
curl -X POST https://wms-api.company.com/v1/odoo/sync/products \
  -H "Authorization: Bearer {token}"

# Manual trigger — sync inbound pickings
curl -X POST https://wms-api.company.com/v1/odoo/sync/inbound \
  -H "Authorization: Bearer {token}"

# Check Odoo stock for specific SKU
curl https://wms-api.company.com/v1/odoo/stock/SAM-S24U \
  -H "Authorization: Bearer {token}"
