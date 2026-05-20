┌────────────┬───────────────────────┬───────────┬──────┬──────┬──────────┬───────────┐
│ Provider   │ Email                 │ GoogleId  │ FbId │ XId  │ MsId     │ LiId      │
├────────────┼───────────────────────┼───────────┼──────┼──────┼──────────┼───────────┤
│ local      │ admin@wms.vn          │ —         │ —    │ —    │ —        │ —         │
│ google     │ user@gmail.com        │ 112233... │ —    │ —    │ —        │ —         │
│ facebook   │ user@fb.com           │ —         │ 998… │ —    │ —        │ —         │
│ x          │ x_15848@x.wms.local   │ —         │ —    │ 15…  │ —        │ —         │
│ microsoft  │ user@outlook.com      │ —         │ —    │ —    │ abc123…  │ —         │
│ microsoft  │ user@company.com      │ —         │ —    │ —    │ abc123…  │ —         │
│ linkedin   │ user@example.com      │ —         │ —    │ —    │ —        │ uniklzI…  │
└────────────┴───────────────────────┴───────────┴──────┴──────┴──────────┴───────────┘

Account linking: same email → auto link across all providers
  • Cùng email → auto link (local ↔ google ↔ facebook ↔ microsoft ↔ linkedin)
  • X dùng XId (không có email) → link theo XId
  • User có thể login bằng bất kỳ linked method → cùng 1 account

=====Google Cloud Console Setup======

Bước 1: Tạo OAuth credentials
	Vào https://console.cloud.google.com
	Tạo project mới hoặc chọn project hiện có
	APIs & Services → Credentials → Create Credentials → OAuth client ID
	Application type: Web application
	Tên: WMS Production
	Authorized redirect URIs thêm:
	https://api.wms.vn/v1/auth/google/callback (production)
	http://localhost:5182/v1/auth/google/callback (dev)
	Copy Client ID và Client Secret
Bước 2: Cấu hình OAuth consent screen
	OAuth consent screen → External (hoặc Internal nếu dùng Workspace)
	App name: WMS Warehouse Management
	Scopes: email, profile, openid
	Test users: thêm email Gmail của bạn khi chưa publish


┌─────────┐         ┌──────────┐          ┌──────────┐
│ Browser │         │ WMS API  │          │  Google  │
│ (User)  │         │ (.NET)   │          │  OAuth   │
└────┬────┘         └────┬─────┘          └────┬─────┘
     │                   │                     │
     │  1. Click "Đăng nhập bằng Google"       │
     │ ──────────────────────────────────────▶ │
     │    redirect to Google consent screen    │
     │                    │                    │
     │  2. User approves, Google redirect back │
     │ ◀─────────────────────────────────────  │
     │    GET /v1/auth/google/callback         │
     │    ?code=4/0Axx...&state=xxx            │
     │                    │                    │
     │  3. API exchange code for tokens        │
     │                    │ ────────────────▶  │
     │                    │  POST /token       │
     │                    │ ◀────────────────  │
     │                    │  access_token,     │
     │                    │  id_token          │
     │                    │                    │
     │  4. API get user info                   │
     │                    │ ────────────────▶  │
     │                    │  GET /userinfo     │
     │                    │ ◀────────────────  │
     │                    │  email, name,      │
     │                    │  picture           │
     │                    │                    │
     │  5. Find or create WMS user             │
     │  6. Generate WMS JWT                    │
     │                    │                    │
     │  7. Redirect to frontend with JWT       │
     │ ◀────────────────────────────────────   │
     │    302 → https://app.wms.vn/auth/       │
     │    callback?token=eyJhbG...             │
     │                    │                    │

Flow B — Google ID Token (simpler, for SPA)

     │  1. Frontend load Google Sign-In SDK    │
     │  2. User click → Google popup           │
     │  3. Google returns id_token (JWT)       │
     │  4. Frontend POST id_token to API       │
     │ ──────────────────────────────────────▶ │
     │    POST /v1/auth/google                 │
     │    { "idToken": "eyJhbG..." }           │
     │                    │                    │
     │  5. API verify token with Google        │
     │  6. Find/create user → return WMS JWT   │
     │ ◀────────────────────────────────────── │
     │    { "accessToken": "eyJhbG..." }       │



=====Facebook App Setup======

Bước 1: Tạo Facebook App
    Vào https://developers.facebook.com
    Click Create App → Consumer (hoặc Business)
    App name: WMS Warehouse
    App ID và App Secret → copy lại
Bước 2: Cấu hình Facebook Login
    Dashboard → Add Product → Facebook Login → Setup
    Chọn Web
    Site URL: https://wms.vn
    Settings → Facebook Login → Valid OAuth Redirect URIs:
    https://api.wms.vn/v1/auth/facebook/callback (production)
    http://localhost:5182/v1/auth/facebook/callback (dev)
Bước 3: App Review
    App Review → Permissions and Features
    Request permissions:
    email — lấy email user
    public_profile — lấy name, avatar (mặc định có)
    Ở chế độ Development: chỉ admin/developer/tester của app mới login được

Để mở cho tất cả: Switch App Mode → Live


Flow A — Client-side (SPA, recommended)

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│    SPA       │         │  WMS API     │         │  Facebook    │
│  Browser     │         │  (.NET 10)   │         │  Graph API   │
└──────┬───────┘         └──────┬───────┘         └──────┬───────┘
       │                        │                        │
       │  1. FB.login() popup   │                        │
       │ ──────────────────────────────────────────────▶ │
       │                        │                        │
       │  2. User approves      │                        │
       │ ◀────────────────────────────────────────────── │
       │    FB returns          │                        │
       │    accessToken         │                        │
       │                        │                        │
       │  3. POST /v1/auth/facebook                      │
       │ ─────────────────────▶ │                        │
       │    { "accessToken":    │                        │
       │      "EAA..." }        │                        │
       │                        │                        │
       │                        │  4. Verify token       │
       │                        │  GET /me?fields=       │
       │                        │    id,name,email,      │
       │                        │    picture             │
       │                        │ ─────────────────────▶ │
       │                        │ ◀────────────────────  │
       │                        │    { id, name, email } │
       │                        │                        │
       │                        │  5. Find/Create user   │
       │                        │  6. Generate WMS JWT   │
       │                        │                        │
       │  7. Response           │                        │
       │ ◀────────────────────  │                        │
       │    { accessToken,      │                        │
       │      user }            │                        │
       │                        │                        │

Flow B — Server-side (Redirect)

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│  Browser     │         │  WMS API     │         │  Facebook    │
└──────┬───────┘         └──────┬───────┘         └──────┬───────┘
       │                        │                        │
       │ 1. GET /auth/fb/redirect                        │
       │ ─────────────────────▶ │                        │
       │ ◀────────────────────  │                        │
       │   302 → Facebook       │                        │
       │                        │                        │
       │ 2. User approves       │                        │
       │ ──────────────────────────────────────────────▶ │
       │ ◀────────────────────────────────────────────── │
       │   redirect to callback │                        │
       │   ?code=AQBx...        │                        │
       │                        │                        │
       │ 3. GET /auth/fb/callback                        │
       │ ─────────────────────▶ │                        │
       │                        │  4. Exchange code      │
       │                        │ ─────────────────────▶ │
       │                        │ ◀────────────────────  │
       │                        │    access_token        │
       │                        │                        │
       │                        │  5. GET /me (verify)   │
       │                        │ ─────────────────────▶ │
       │                        │ ◀────────────────────  │
       │                        │                        │
       │ 6. 302 → frontend      │                        │
       │    ?token=eyJhbG...    │                        │
       │ ◀────────────────────  │                        │

===== X-Twitter Structure ======

X Developer Portal Setup

Bước 1: Tạo X Developer Account
    Vào https://developer.twitter.com/en/portal/dashboard
    Apply cho Free tier (hoặc Basic/Pro nếu cần higher rate limits)
    Verify email + phone number
Bước 2: Tạo Project + App
    Create Project → Name: WMS Warehouse → Use case: Making requests on behalf of users
    Create App → Name: WMS Web
    Copy Client ID (không phải API Key/Secret — dùng OAuth 2.0 Client ID)
    App Settings → User authentication settings → Edit:
    Type: Web App, Automated App or Bot
    App info → Callback URL: https://api.wms.vn/v1/auth/x/callback
    App info → Website URL: https://wms.vn
    Permissions: Read (default)
    Request email from users: Checked (may or may not work)
    Save → Copy Client ID và Client Secret

X (Twitter) yêu cầu OAuth 2.0 với PKCE cho TẤT CẢ app types
— kể cả server-side web apps.

PKCE = Proof Key for Code Exchange
Bảo vệ authorization code khỏi bị intercept.

┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  ┌──────────┐         ┌──────────┐         ┌─────────────┐   │
│  │ Browser  │         │ WMS API  │         │ X (Twitter) │   │
│  │ (WMS)    │         │ (.NET)   │         │ OAuth 2.0   │   │
│  └────┬─────┘         └────┬─────┘         └──────┬──────┘   │
│       │                    │                      │          │
│       │  1. GET /auth/x/redirect                  │          │
│       │ ─────────────────▶│                       │          │
│       │                   │ Generate:             │          │
│       │                   │  code_verifier        │          │
│       │                   │  code_challenge       │          │
│       │                   │  state                │          │
│       │                   │                       │          │
│       │  2. 302 → X consent + code_challenge      │          │
│       │ ◀────────────────│ ──────────────────────▶│          │
│       │                   │                       │          │
│       │  3. User approves │                       │          │
│       │ ◀─────────────────────────────────────────│          │
│       │    ?code=xxx&state=yyy                    │          │
│       │                   │                       │          │
│       │  4. GET /auth/x/callback                  │          │
│       │ ─────────────────▶│                       │          │
│       │                   │  5. Exchange code     │          │
│       │                   │  + code_verifier      │          │
│       │                   │ ────────────────────▶ │          │
│       │                   │ ◀──────────────────── │          │
│       │                   │  access_token         │          │
│       │                   │                       │          │
│       │                   │  6. GET /2/users/me   │          │
│       │                   │ ────────────────────▶ │          │
│       │                   │ ◀──────────────────── │          │
│       │                   │  id, name, username,  │          │
│       │                   │  profile_image_url    │          │
│       │                   │                       │          │
│       │                   │  7. Find/Create user  │          │
│       │                   │  8. Generate WMS JWT  │          │
│       │                   │                       │          │
│       │  9. 302 → frontend?token=eyJhbG...        │          │
│       │ ◀─────────────────│                       │          │
│       │                   │                       │          │
└──────────────────────────────────────────────────────────────┘

PKCE Flow cho WMS:

Server generates (cho mỗi login attempt):

  code_verifier  = random 32-byte string, base64url encoded
                   e.g. "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"

  code_challenge = BASE64URL(SHA256(code_verifier))
                   e.g. "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"

  state          = random string (CSRF protection)

Flow:

  1. Browser → X consent screen
     URL includes: code_challenge + code_challenge_method=S256

  2. X redirects back with: authorization code + state

  3. Server → X token endpoint
     POST includes: code + code_verifier (original, not hashed)

  4. X verifies: SHA256(code_verifier) == code_challenge from step 1
     If match → returns access_token

Why secure:

  - Attacker intercepts authorization code (step 2)
  - Cannot exchange for token without code_verifier
  - code_verifier never leaves server
  - code_challenge (hash) is useless without original verifier

Server-side storage:

  code_verifier + state → temporarily stored in:
    Option A: Redis (key=state, value=verifier, TTL=5min)
    Option B: Encrypted cookie
    Option C: Session cache

  WMS uses: Redis (consistent with existing infrastructure)

====Microsoft Azure AD=====

Đăng nhập WMS bằng Microsoft (Outlook, Hotmail, Live, Azure AD). Dùng OpenID Connect — verify id_token cục bộ giống Google.

Azure AD App Registration
Bước 1: Đăng ký App
    Vào https://portal.azure.com → Azure Active Directory → App registrations → New registration
    Name: WMS Warehouse Management
    Supported account types (xem phần Tenants bên dưới)
    Redirect URI:
    Platform: Web
    URI: https://api.wms.vn/v1/auth/microsoft/callback
    Dev: http://localhost:5182/v1/auth/microsoft/callback
    Click Register
Bước 2: Lấy credentials
    Overview → Copy Application (client) ID
    Certificates & secrets → New client secret
    Description: WMS API
    Expires: 24 months (hoặc custom)
    Copy Value (chỉ hiện 1 lần!)
Bước 3: API Permissions
    API permissions → Add a permission → Microsoft Graph → Delegated permissions
    Thêm:
    openid — sign in (bắt buộc, auto-added)
    email — read user email (bắt buộc, auto-added)
    profile — read basic profile (bắt buộc, auto-added)
    User.Read — read signed-in user profile (default)
    Click Grant admin consent (nếu là Azure AD org)


┌──────────────┐         ┌──────────────┐         ┌──────────────────────┐
│    SPA       │         │  WMS API     │         │  Microsoft Identity  │
│  Browser     │         │  (.NET 10)   │         │  Platform            │
└──────┬───────┘         └──────┬───────┘         └──────────┬───────────┘
       │                        │                            │
       │  1. GET /auth/ms/redirect                           │
       │ ─────────────────────▶│                             │
       │  2. 302 → Microsoft consent                         │
       │ ◀──────────────────── │ ──────────────────────────▶ │
       │                       │                             │
       │  3. User approves     │                             │
       │ ◀─────────────────────────────────────────────────  │
       │    ?code=xxx&state=yyy│                             │
       │                       │                             │
       │  4. GET /auth/ms/callback                           │
       │ ─────────────────────▶│                             │
       │                       │  5. Exchange code           │
       │                       │ ──────────────────────────▶ │
       │                       │ ◀────────────────────────── │
       │                       │    id_token + access_token  │
       │                       │                             │
       │                       │  6. Verify id_token         │
       │                       │    (local OIDC validation)  │
       │                       │                             │
       │                       │  7. Find/Create user        │
       │                       │  8. Generate WMS JWT        │
       │                       │                             │
       │  9. 302 → frontend    │                             │
       │     ?token=eyJhbG...  │                             │
       │ ◀──────────────────── │                             │

Microsoft returns email (always) → no placeholder needed!
Supported accounts: @outlook.com, @hotmail.com, @live.com, Azure AD (@company.com)

====LinkedIn====

Developer Portal Setup
Bước 1: Tạo App
    Vào https://www.linkedin.com/developers/apps → Create app
    App name: WMS Warehouse Management
    LinkedIn Page: Chọn company page (hoặc tạo personal page nếu chưa có)
    App logo: Upload logo WMS
    Agree to terms → Create app
Bước 2: Products
    Vào tab Products
    Request Sign In with LinkedIn using OpenID Connect
    Usually auto-approved (instant access)
    Gives scopes: openid, profile, email
Bước 3: Auth Settings
    Tab Auth
    Copy Client ID
    Copy Client Secret (click "Show" → copy)
    OAuth 2.0 settings → Authorized redirect URLs:
    http://localhost:5182/v1/auth/linkedin/callback (dev)
    https://api.wms.vn/v1/auth/linkedin/callback (prod)

┌──────────────┐         ┌──────────────┐         ┌──────────────────────┐
│   SPA        │         │  WMS API     │         │  LinkedIn OAuth 2.0  │
│  Browser     │         │  (.NET 10)   │         │  + OpenID Connect    │
└──────┬───────┘         └──────┬───────┘         └──────────┬───────────┘
       │                        │                            │
       │  1. GET /auth/linkedin/redirect                     │
       │ ─────────────────────▶ │                            │
       │  2. 302 → LinkedIn consent                          │
       │ ◀────────────────────  │ ─────────────────────────▶ │
       │                        │                            │
       │  3. User approves      │                            │
       │ ◀────────────────────────────────────────────────── │
       │    ?code=xxx&state=yyy│                             │
       │                       │                             │
       │  4. GET /auth/linkedin/callback                     │
       │ ─────────────────────▶│                             │
       │                       │  5. Exchange code → token   │
       │                       │ ──────────────────────────▶ │
       │                       │ ◀────────────────────────── │
       │                       │    access_token             │
       │                       │                             │
       │                       │  6. GET /v2/userinfo (OIDC) │
       │                       │ ──────────────────────────▶ │
       │                       │ ◀────────────────────────── │
       │                       │   sub, name, email, picture │
       │                       │                             │
       │                       │  7. Find/Create user        │
       │                       │  8. Generate WMS JWT        │
       │                       │                             │
       │  9. 302 → frontend    │                             │
       │     ?token=eyJhbG...  │                             │
       │ ◀──────────────────── │                             │

LinkedIn OIDC returns email (if user grants permission)
Similar to Microsoft — redirect flow, no SPA SDK needed

