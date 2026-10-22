# Core-Up

Ứng dụng fitness cho đồ án tốt nghiệp: ASP.NET Core/.NET 10, EF Core,
PostgreSQL 17 và Flutter/Provider/Dio.

## Cấu trúc

```text
CoreUp.sln
src/          # Api, Application, Domain, Infrastructure
tests/        # Backend tests
mobile/       # Flutter source, assets và platform projects
docker-compose.yml
```

## Backend

Yêu cầu .NET SDK 10 và PostgreSQL 17; Docker Desktop nếu dùng Compose.
Chạy từ root repository:

```bash
cp -n .env.example .env
# Đặt POSTGRES_PASSWORD riêng trong .env.
docker compose up -d postgres # chỉ khi cần database cục bộ
dotnet tool restore
dotnet restore CoreUp.sln
# Chỉ áp dụng migration vào database đã chọn:
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef database update --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
dotnet run --project src/CoreUp.Api --launch-profile http
```

API: `http://localhost:5027`; Swagger: `http://localhost:5027/swagger`;
health: `http://localhost:5027/health`.
Đăng ký bằng `POST /api/auth/register` với `username`, `email`, `password`;
đăng nhập bằng `POST /api/auth/login` với `username`, `password` để lấy Bearer token.
Username dài 3–24 ký tự ASCII Latin/số/`_`, unique không phân biệt hoa/thường;
email được trim/lowercase và unique. Password đăng ký dài 8–128 ký tự,
Confirm Password chỉ đối chiếu trên Flutter, không gửi API/lưu DB.
Migration `AddUsernames` cấp tên `u_1`, `u_2`, ... theo thứ tự GUID cho tài khoản cũ,
giữ nguyên GUID, password hash và các quan hệ dữ liệu. Để tra tên được cấp trong
môi trường development, chạy query read-only bằng công cụ PostgreSQL đã cấu hình:

```sql
SELECT "Id", "Email", "Username" FROM users ORDER BY "Id";
```

Không đưa kết quả query chứa email vào log công khai. Auth có giới hạn 30 request/phút/IP
(chung register/login); khi nhận 429 hãy đợi trước khi thử lại. Phase 2 có OTP email và quên/đổi mật khẩu; Google và xóa tài khoản ở phase sau.

Ngoài Development, cấu hình `Jwt__Key`
riêng tối thiểu 32 byte; không dùng khóa mẫu hoặc commit `.env`.

### Email và bảo mật Phase 2 (local)

Chạy `docker compose up -d mailpit` để nhận email nội bộ. Mailpit UI:
`http://127.0.0.1:8025`, SMTP `localhost:1025`; chỉ development/test, không giao email
ra Internet. PostgreSQL vẫn dùng volume hiện có. Trước migration database đang có
dữ liệu, tạo backup ngoài repo; không reset DB.

Đặt `Otp__Key` ngẫu nhiên ít nhất 32 byte trong `.env` bị ignore hoặc user secrets
(có thể sinh bằng `openssl rand -hex 32`); không dùng placeholder `.env.example` làm
secret thực. HMAC-SHA256 bảo vệ OTP 6 chữ số, TTL 5 phút, tối đa 5 lần sai,
resend 60 giây, tối đa 5 email/user/purpose/giờ; IP dùng quota auth chung 30/phút.
Purpose verification và reset độc lập. Resend thay thế mã/grant cũ. Reset grant ngẫu
nhiên 256 bit có TTL 5 phút và chỉ sử dụng một lần. OTP/grant chỉ lưu hash, không log.

- `GET /api/auth/me`: trạng thái server cho restore (JWT).
- `GET /api/auth/email-verification/status`: deadline/cooldown từ server (JWT).
- `POST /api/auth/email-verification/send`: gửi/resend OTP (JWT).
- `POST /api/auth/email-verification/verify`: `{code}` (JWT).
- `POST /api/auth/forgot-password`: `{email}`, luôn 202/generic cho địa chỉ hợp lệ.
- `POST /api/auth/reset-password/verify`: `{email,code}` → resetToken một lần.
- `POST /api/auth/reset-password`: `{email,resetToken,newPassword,confirmPassword}`.
- `POST /api/auth/change-password`: `{currentPassword,newPassword,confirmPassword}` (JWT).

Tài khoản mới nhận JWT giới hạn cho xác minh/me, chưa vào profile/workout trước khi
verified. Legacy accounts không bị tự đánh dấu verified và vẫn truy cập được;
Flutter gợi ý xác minh khi quay lại, có **Để sau**. Guest không có yêu cầu OTP.
Reset/change password tăng TokenVersion: mọi token cũ, gồm phiên hiện tại, bị backend
từ chối; Flutter xóa secure token và yêu cầu login lại. JWT Phase 1 thiếu version được
xem là version 0, giữ hợp lệ cho đến lần đổi/reset mật khẩu đầu tiên.

Có thể dùng SMTP provider tùy chọn bằng `Smtp__Host`, `Smtp__Port`, `Smtp__From`,
`Smtp__EnableSsl`, `Smtp__Username`, `Smtp__Password` trong environment/user secrets;
không bắt buộc Gmail. Chưa kiểm thử SMTP ngoài Mailpit hoặc cấu hình production.
Queue gửi email giới hạn trong RAM, retry 3 lần, không lưu plaintext OTP vào DB.
Restart có thể mất email đang chờ; user có thể resend sau cooldown. Mailpit không
phải durable mail delivery hoặc production email service.

Kiểm thử auth native bằng API trỏ **database thử riêng** (không dùng DB development):

```bash
cd mobile
flutter test integration_test/username_auth_flow_test.dart -d macos --dart-define=API_BASE_URL=http://127.0.0.1:5049 --dart-define=AUTH_PHASE1_TEST_API=true
```

Native test Phase 2 cần Mailpit local đang chạy; nó dùng địa chỉ test trên DB thử,
đọc OTP qua Mailpit API và chạy verify/change/reset end-to-end.
Test dùng khóa secure storage riêng và preference trong bộ nhớ. macOS dùng native login
Keychain qua FlutterSecureStorage; iOS/Android giữ cấu hình secure storage hiện có.

## Flutter

Môi trường đã dùng: Flutter 3.41.5 / Dart 3.11.3.

```bash
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5027
```

iOS Simulator dùng `127.0.0.1`; Android Emulator dùng `10.0.2.2`.
Thiết bị thật cần địa chỉ server và cấu hình HTTP/HTTPS phù hợp.

## Kiểm tra

Từ root:

```bash
dotnet build CoreUp.sln
dotnet test CoreUp.sln
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
cd mobile
flutter analyze
flutter test
```
