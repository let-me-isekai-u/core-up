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
Đăng ký/đăng nhập để lấy Bearer token. Ngoài Development, cấu hình `Jwt__Key`
riêng tối thiểu 32 byte; không dùng khóa mẫu hoặc commit `.env`.

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
