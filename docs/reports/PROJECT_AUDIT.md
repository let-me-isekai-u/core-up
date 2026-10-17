# Đánh giá Core-Up — 17/10/2026

## Phạm vi và bằng chứng

Backend gốc: `/Users/nhatanhly/core-up`, origin `https://github.com/let-me-isekai-u/core-up.git`.
HEAD ban đầu `5b11a77` (feat: implement CoreUp backend MVP), trước đó `aa3e69e`.
Ban đầu chỉ có main và origin/main; không có thay đổi tracked, có các báo cáo/script untracked.
Mobile xác định qua package `core_up_mobile`, README CoreUp, providers và routes trùng API:
`/Users/nhatanhly/core_up_mobile`. Không có `.git`, nên status/branch/log/remotes của mobile
không khả dụng. URL suy đoán `core-up-mobile.git` trả Repository not found: không kết luận
repository không tồn tại (có thể private/thiếu quyền). Không thay thế bằng dự án Flutter khác.

Đã đọc README, toàn bộ mã C#/Dart, csproj/pubspec, mappings, migration/snapshot,
controllers, providers, screens và tests. Tài liệu cũ không được dùng làm bằng chứng pass.
Không tính phần trăm hoàn thành. VERIFIED chỉ áp dụng chính xác phạm vi test đã chạy;
IMPLEMENTED nghĩa là có code, chưa xác minh nghiệp vụ với DB.
PARTIAL: thiếu phần quan trọng; PLANNED: chưa có; BLOCKED: thiếu phụ thuộc;
NOT APPLICABLE: ngoài MVP.

## Kiến trúc thực tế

Backend có bốn project: Api → Application + Infrastructure; Application → Domain và EF Core;
Infrastructure → Application + Domain. Đây là phân lớp Clean Architecture thực dụng:
Application dùng trực tiếp DbSet/EF truy vấn qua IApplicationDbContext, không có repository riêng.
Api dùng DataAnnotations, JWT bearer, ProblemDetails middleware, Swagger/OpenAPI trong Development.
Infrastructure dùng Npgsql, PasswordHasher được đăng ký ở Application; không có Google SDK.

Flutter: MaterialApp/Navigator, nhiều screen ghép bằng Dart `part` trong core_up_screens.dart;
MultiProvider và ChangeNotifier (Auth/Profile/Workout), Dio nằm trong AuthProvider,
FlutterSecureStorage lưu token, SharedPreferences lưu onboarding/session mode. Phần lớn DTO
là Map động, chỉ AuthResponse có model riêng. Chưa có domain/repository/use-case độc lập:
không thể gọi mobile là Clean Architecture đầy đủ. Guest dùng 8 bài mẫu hardcode và phiên tập RAM.

## Inventory backend

| Chức năng | Trạng thái | Bằng chứng và giới hạn |
|---|---|---|
| Cấu hình PostgreSQL | IMPLEMENTED | DependencyInjection, Compose postgres:17, EnvFile duyệt lên tìm .env |
| Kết nối DB/health | BLOCKED | GET /health = 503; localhost:5432 không kết nối được |
| Entities và mappings | IMPLEMENTED | 8 entity, 9 FK; CoreUpDbContext và InitialMvp |
| Migration/model consistency | VERIFIED | EF has-pending-model-changes pass; không có model drift |
| Migration đã áp dụng vào DB | BLOCKED | migrations list có cảnh báo mất kết nối dù exit 0; không xác định applied status |
| Email register/login, JWT | IMPLEMENTED | AuthService, PasswordHasher, JwtTokenService; chưa chạy request ghi DB |
| Chặn request anonymous | VERIFIED | /profile, /exercises, /workout-plans/active đều trả 401 |
| Ownership/authorization dữ liệu | IMPLEMENTED | Services lọc UserId; chưa có integration test chống truy cập chéo |
| Google login | PLANNED | Không có route/service/config; tùy chọn sau MVP email |
| Hồ sơ người dùng | IMPLEMENTED | GET/POST/PUT /api/profile; POST lưu profile + weight rồi sinh plan |
| Danh mục bài tập | IMPLEMENTED | GET list/detail, lọc difficulty/muscleGroup; 25 bản ghi seed |
| Quản trị/import bài tập | PLANNED | Không có write/import route; chưa có role Administrator |
| Nhóm và độ khó | IMPLEMENTED | MuscleGroup string, FitnessLevel enum; không có bảng Category |
| Sinh và đọc kế hoạch | IMPLEMENTED | WorkoutPlanService; rule-based theo level/BMI/duration |
| Quy tắc sinh plan được test | VERIFIED | 1 test beginner-overweight tránh burpee, sets=2; không bao phủ mọi cấu hình |
| Lịch tập theo ngày | PLANNED | Không entity/route ngày tập; TrainingDaysPerWeek chỉ lưu profile |
| Bắt đầu/ghi nhận/hoàn tất/hủy buổi tập | IMPLEMENTED | WorkoutSessionService + controller; chưa test DB hoặc concurrency |
| Lịch sử completed sessions | IMPLEMENTED | HistoryAsync, phân trang tối đa 100; không trả in-progress/cancelled |
| Cân nặng và BMI | IMPLEMENTED | ProfileService, WeightProgressService; không đo vòng cơ thể |
| BMI tính toán/phân loại | VERIFIED | 7 ca xUnit (1 Fact + 6 boundary cases) pass |
| Thống kê | IMPLEMENTED | Tổng buổi/phút, tuần, streak, weight history; ngày theo UTC |
| Validation/exception/security | PARTIAL | Có ranges/ProblemDetails; thiếu rate limit, refresh/revocation, enum validation chặt, concurrency tests |
| API documentation | VERIFIED | Swagger JSON HTTP 200; inventory runtime trong API_ROUTES.md |
| Nutrition/premium | NOT APPLICABLE | Không code; ngoài MVP đã nêu |

## Inventory mobile

| Chức năng | Trạng thái | Bằng chứng và giới hạn |
|---|---|---|
| Kiến trúc và Provider | IMPLEMENTED | core_up_screens.dart, 3 ChangeNotifier; coupling Dio/Auth và Map động |
| Navigation/screens | PARTIAL | Splash → onboarding/auth → setup/shell; shell chỉ Home/Progress/Profile |
| Onboarding ban đầu | VERIFIED | Widget test thông điệp đầu tiên pass |
| Email auth và secure session | PARTIAL | AuthProvider gọi backend; restoreSession xóa token với mọi lỗi /profile, kể cả 404/offline |
| Google login | PLANNED | Không có implementation |
| Guest exercise/workout | VERIFIED | Test provider và player hướng dẫn sets/reps pass; không chứng minh toàn bộ luồng UI |
| State management | PARTIAL | ProxyProvider tái dùng instance; không reset profile/plan/history khi đổi tài khoản |
| API integration | IMPLEMENTED | Paths/payload/enum strings đối chiếu khớp cho lời gọi hiện có; E2E bị chặn DB |
| Home | IMPLEMENTED | Đọc active plan + summary + history; chưa kiểm thử với tài khoản thật |
| Exercise browsing/detail | PARTIAL | Screens và lọc có code nhưng không tìm được route mở từ shell/home |
| Lịch tập | PLANNED | Nhãn “hôm nay” chỉ hiển thị active plan; không có calendar/schedule |
| Workout authenticated | PARTIAL | start/update/complete có gọi API; thoát không gọi cancel, pause chỉ ở client |
| Progress/weight | PARTIAL | Có history và thêm cân nặng; current weight dùng weights.first sai thứ tự |
| Profile | PARTIAL | Tạo/đọc; nút Sửa rỗng, provider chỉ POST, chưa gọi PUT |
| Loading/error | PARTIAL | Có loading widget/minimum duration, error message; complete/end không catch và có thể kẹt saving |
| Static analysis | VERIFIED | flutter analyze: no issues |
| Native/release/web compatibility | BLOCKED | Chưa build; Android main thiếu INTERNET; dart:io pid không tương thích web |

## Lỗi và nợ kỹ thuật cần xử lý

1. `ProfileService.CreateAsync`: SaveChanges profile/weight trước GenerateAsync. Seed thiếu hoặc generate lỗi
   có thể trả lỗi nhưng profile đã tồn tại; retry POST nhận 409. Cần transaction + test rollback.
2. `WorkoutPlanService.GenerateAsync`: index UserId/IsActive không unique. Hai request song song có thể tạo
   nhiều active plans; GetActive dùng SingleOrDefault sẽ lỗi. Cần invariant DB và concurrency test.
3. `WorkoutPlanGenerator`: không sử dụng TrainingDaysPerWeek; Goal chỉ mô tả, không đổi selection.
   Thiếu slug sẽ âm thầm giảm số bài, EstimatedDurationMinutes chỉ chép sở thích, không tính tổng thực.
4. `WorkoutSessionService`: cho complete dù chưa hoàn thành bài nào; có thể tạo nhiều in-progress;
   số set/reps không ràng buộc target và không có idempotency. Cần thống nhất quy tắc trước khi sửa.
5. Flutter `WorkoutProvider.complete` gửi targetReps một hiệp dù completedSets nhiều hiệp;
   backend cộng CompletedReps thành TotalReps. Cần chốt ý nghĩa tổng reps và test contract.
6. Flutter restoreSession coi profile 404/network error như token không hợp lệ; tài khoản chưa setup
   có thể mất phiên. Logout không dọn dữ liệu providers: rủi ro dữ liệu người dùng trước còn trên UI.
7. ProgressScreen hiển thị weights.first trong khi API OrderBy RecordedAt tăng dần; cần latest/summary.
8. Workout UI pause không được trừ khỏi duration server; exit bỏ session InProgress, thiếu resume/cancel UI.
9. Exercise screens chưa có entrypoint; detail Start của user chỉ pop, guest luôn chạy mẫu sáu bài.
10. Không refresh token/logout server/reset password/email verification/rate limit. Development JWT key
    nằm trong source là khóa mẫu công khai; production cần Jwt__Key riêng. Không ghi lại secret .env vào audit.
11. Các enum request chưa có EnumDataType; JSON converter mặc định có thể nhận integer ngoài enum.
    Các lỗi unique-constraint/race hiện về 500 chung; danh sách weights/catalog không phân trang.
12. Progress tải toàn bộ sessions/weights để tính tổng; báo cáo ngày/tuần theo UTC có thể lệch ngày Việt Nam.

Không thấy TODO/FIXME/NotImplemented trong mã nghiệp vụ; nút Sửa rỗng là bằng chứng chưa hoàn thành.
Các output DOCX/slide cũ và hai generator script untracked được giữ nguyên, không đưa vào migration commit.
Một số ảnh hero chưa thấy được dùng; không xóa vì là tài sản nguồn. Không coi template platform là code chết.

## Database và dữ liệu bài tập

8 bảng: users, user_profiles, exercises, workout_plans, workout_plan_exercises,
workout_sessions, workout_session_exercises, weight_records. Mỗi bảng dùng Guid Id PK.
User–Profile là 1–0..1 (unique UserId); User–Plans/Sessions/Weights là 1–n.
Plan–Exercise và Session–Exercise là n–n qua hai entity nối có Id riêng và thuộc tính tập luyện.
Session.WorkoutPlanId nullable, xóa plan SET NULL; FK Exercise RESTRICT;
các quan hệ sở hữu khác CASCADE. Unique email, slug và thứ tự bài trong mỗi plan/session.
Không có role/category/schedule/nutrition/premium table.

InitialMvp, designer và snapshot hiện đồng bộ với model (EF command pass).
Chưa xác minh database thực có schema/seed hay không vì mất kết nối.
Có 25 exercise seed; 24 Beginner, Burpee Intermediate, không bài Advanced, ImageUrl null.
Mobile có 7 PNG UI/chibi/hero; không phải dataset kỹ thuật bài tập có thể import tự động.
Tìm theo tên exercise/workout/fitness trong home sâu 3 không thấy CSV/JSON/ZIP riêng;
phạm vi tìm kiếm hữu hạn, không khẳng định đã kiểm kê mọi ổ đĩa. Yêu cầu import chi tiết ở completion plan.

## API integration và kiểm tra

Các lời gọi Flutter hiện có khớp route backend; auth response camelCase, enum dạng string,
GUID authenticated, guest dùng g1…g8 cục bộ không gửi lên API. Các API có nhưng Flutter chưa dùng:
PUT profile; GET exercise by id (detail dùng object list), GET plans/all/by id;
GET session by id; POST cancel. Chưa có endpoint schedule/import/refresh/Google.

PASS: restore .NET, build 0 warning/error, 8 xUnit tests; tool restore; model consistency;
Flutter pub get (lock không đổi), analyze, 4 tests; Swagger 200; 3 anonymous requests 401.
FAIL kiểm tra môi trường: /health 503; database connection failed. EF list exit 0 không có nghĩa DB pass.
Một lần EF command dùng `--environment` bị từ chối, đã chạy lại bằng ASPNETCORE_ENVIRONMENT và pass.
NOT RUN: authenticated E2E, migration apply, integration DB, Flutter native/release builds, device walkthrough.
Không khởi động Docker, không ghi dữ liệu DB. Host smoke riêng cổng 15027 được dừng sau kiểm tra.
Xem `validation/` và COMMAND_LOG.md. Blocker chính: PostgreSQL chưa truy cập được; thiếu test
integration; không có lịch sử Git mobile để bảo toàn. Diagram source được kiểm tra cấu trúc,
chưa render vì không thấy PlantUML/Mermaid CLI trong PATH.
