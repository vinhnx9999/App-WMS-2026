
Technology Stack
Layer					Tech							Version

Runtime					.NET 10							10.0.x
Database				PostgreSQL 17					EF Core 10
Cache					Redis 7							StackExchange.Redis
Auth					JWT Bearer						Microsoft.AspNetCore.Authentication.JwtBearer 10
Validation				FluentValidation				11.x
Mapping					Mapster							7.x
Logging					Serilog							8.x
API Docs				Swashbuckle / Scalar			latest
Password				BCrypt.Net-Next					4.x
Testing					xUnit + Moq + FluentAssertions	latest
Container	Docker + docker-compose	—
ERP	SAP NCo + Odoo JSON-RPC	—


Account linking flow:
  1. User có account local (admin@wms.vn)
  2. User login Google với cùng email → auto link GoogleId
  3. User login Facebook với cùng email → auto link FacebookId
  4. Giờ user login bằng bất kỳ cách nào → cùng 1 account
