# Core-Up

Ứng dụng fitness cho đồ án tốt nghiệp: ASP.NET Core/.NET 10, EF Core,
PostgreSQL 17 và Flutter/Provider/Dio.

```text
core-up/
├── CoreUp.sln
├── src/                    # Api, Application, Domain, Infrastructure
├── tests/CoreUp.Tests/
├── docker-compose.yml      # chỉ PostgreSQL
├── mobile/                 # Flutter client và assets gốc
└── docs/
    ├── diagrams/
    └── reports/
```

Backend giữ ở root để bảo toàn solution, script và cách nạp `.env` hiện có.
Không có lịch tập theo ngày hoặc Google authentication; xem [audit](docs/reports/PROJECT_AUDIT.md).

## Backend

Yêu cầu .NET SDK 10; PostgreSQL 17 đang chạy hoặc Docker Desktop nếu dùng Compose.
Chạy từ root repository:

```bash
cp .env.example .env
# Chọn POSTGRES_PASSWORD riêng trong .env; không commit file này.
docker compose up -d postgres # chỉ khi cần PostgreSQL cục bộ
# Cần thiết lập Jwt__Key riêng tối thiểu 32 byte ngoài Development.
dotnet tool restore
dotnet restore CoreUp.sln
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef database update --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
dotnet run --project src/CoreUp.Api --launch-profile http
```

API: `http://localhost:5027`; Swagger: `http://localhost:5027/swagger`;
health: `http://localhost:5027/health` (kiểm tra kết nối DB).
Đăng ký/đăng nhập lấy `accessToken`, dùng Bearer token. Migration InitialMvp chứa 25 bài tập.
Lệnh database update ghi schema: chỉ chạy với database bạn đã chọn.
Khóa JWT trong appsettings.Development.json chỉ dành cho development.

```bash
dotnet build CoreUp.sln
dotnet test CoreUp.sln
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
```

## Mobile

Nguồn nhập từ `core_up_mobile` cục bộ; tên package vẫn là `core_up_mobile`.
Môi trường đã kiểm tra: Flutter 3.41.5 / Dart 3.11.3.

```bash
cd mobile
flutter pub get
flutter analyze
flutter test
flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5027
```

`127.0.0.1` phù hợp iOS Simulator trên cùng máy. Android Emulator thường cần
`http://10.0.2.2:5027`; thiết bị thật cần địa chỉ LAN và cấu hình HTTP/HTTPS phù hợp.
iOS hiện chỉ có ngoại lệ HTTP development cho 127.0.0.1; Android release thiếu
INTERNET permission trong main manifest. Chưa xác nhận build release/native.
Chế độ khách chạy cục bộ, không lưu kết quả lên backend.

## Tài liệu

- [Đánh giá hiện trạng](docs/reports/PROJECT_AUDIT.md)
- [Ưu tiên hoàn thiện backend](docs/reports/BACKEND_COMPLETION_PLAN.md)
- [Migration và rollback](docs/reports/MONOREPO_MIGRATION.md)
- [API routes từ Swagger](docs/reports/API_ROUTES.md)
- [Use cases](docs/diagrams/use-case-current.puml), [ERD](docs/diagrams/erd-current.mmd),
  [kiến trúc](docs/diagrams/architecture-current.mmd), [domain classes](docs/diagrams/domain-classes.mmd)
- [Kết quả kiểm tra](docs/reports/validation/), [nhật ký lệnh](docs/reports/COMMAND_LOG.md)

`docker_command.txt` là ghi chú cũ đã được đọc khi audit; chứa cả lệnh thay đổi/xóa DB,
không phải checklist tự động. Các script và sản phẩm báo cáo cũ trong máy được giữ nguyên.
