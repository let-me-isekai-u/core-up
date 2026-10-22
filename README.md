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
(chung register/login); khi nhận 429 hãy đợi trước khi thử lại. Phase 1 chưa có OTP,
Google, quên/đổi mật khẩu hoặc xóa tài khoản.

Ngoài Development, cấu hình `Jwt__Key`
riêng tối thiểu 32 byte; không dùng khóa mẫu hoặc commit `.env`.

Kiểm thử auth native bằng API trỏ **database thử riêng** (không dùng DB development):

```bash
cd mobile
flutter test integration_test/username_auth_flow_test.dart -d macos --dart-define=API_BASE_URL=http://127.0.0.1:5049 --dart-define=AUTH_PHASE1_TEST_API=true
```

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
