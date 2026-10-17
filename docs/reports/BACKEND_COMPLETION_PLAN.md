# Kế hoạch hoàn thiện backend

Ngày 17/10/2026. Đây là checklist đề xuất, chưa triển khai nghiệp vụ trong lần migration.
Thứ tự bên dưới ưu tiên backend trước khi mở rộng frontend. Mỗi bước chỉ coi hoàn thành khi
acceptance criteria có bằng chứng tự động hoặc kết quả chạy được lưu lại.

| Ưu tiên / task | Hiện trạng | Phần thiếu và module | Phụ thuộc | Tiêu chí nghiệm thu |
|---|---|---|---|---|
| [ ] P0.0 Khôi phục môi trường DB và test tích hợp | BLOCKED | PostgreSQL localhost:5432; tests/CoreUp.Tests chưa có integration fixture | DB test riêng, cấu hình riêng; không dùng DB cá nhân | Restore/build/tests pass; apply InitialMvp vào DB rỗng; health 200; seed 25 bài; model no drift; DB test được dọn riêng |
| [ ] P0.1 Email auth và bảo vệ tài khoản | IMPLEMENTED | AuthService, JwtTokenService, Contracts, controllers: test đăng ký trùng, login sai, token hết hạn/giả, ownership; xử lý race email | P0.0; Jwt__Key ngoài Development | 200/409/401 đúng; không lộ hash; user A không đọc/sửa plan/session/profile của B; request burst có chính sách giới hạn được chốt |
| [ ] P0.2 Hồ sơ nhất quán | PARTIAL | ProfileService: transaction tạo profile/weight/generated plan; validate enum/ngày sinh; semantics PUT | P0.0–1; seed hợp lệ | Lỗi generate rollback mọi dữ liệu; retry tạo được; PUT không tạo hồ sơ thứ hai; weight/BMI đồng bộ; kiểm thử min/max |
| [ ] P1.1 Chuẩn hóa catalog và import tài liệu bài tập | IMPLEMENTED | ExerciseSeed, Exercise entity/mappings, ExerciseService; thiếu pipeline import và mapping tài liệu | Tài liệu nguồn thật; quyền dùng ảnh/video; quyết định nhóm cơ và độ khó | Dry-run chỉ rõ số hợp lệ/trùng/lỗi; import idempotent; slug unique; dữ liệu phù hợp Repetition/Duration; API list/detail đúng |
| [ ] P1.2 Hợp đồng catalog cho mobile | PARTIAL | ExercisesController/Contracts; phân trang/tìm kiếm nếu dataset lớn; màn danh mục chưa truy cập từ shell | P1.1; chốt API contract | Contract tests cho filter/empty/invalid enum/inactive/404; JSON fixture enum strings và nullable fields được duyệt |
| [ ] P2.1 Bất biến kế hoạch active | PARTIAL | WorkoutPlanService, CoreUpDbContext + migration mới; chưa unique active per user | P0.0–2 | Hai request generate đồng thời không làm GetActive lỗi; kết quả tối đa một active; rollback migration được test |
| [ ] P2.2 Hoàn thiện generator và lịch tập | PARTIAL | Generator/Service/Contracts; chưa có entity/route lịch ngày | P1.1; chốt quy tắc ngày nghỉ, timezone, goal, duration | Generator có test các level/BMI/goal; thiếu slug trả lỗi rõ; lịch ngày/tuần đúng TrainingDaysPerWeek; ownership; không đổi migration cũ |
| [ ] P3.1 Ghi nhận buổi tập | IMPLEMENTED | WorkoutSessionService/Contracts: chốt total reps, sets, partial complete, cancel/resume, duration pause | P2; thống nhất semantics mobile | Start→update→complete/history và cancel chạy DB; không update sau close; retry không nhân đôi; duration/reps đúng quy ước |
| [ ] P3.2 Bảo toàn lịch sử | PARTIAL | SessionExercise giữ target nhưng vẫn đọc tên exercise hiện tại; chính sách cập nhật catalog chưa chốt | P1/P3.1 | Đổi tên/deactivate bài không làm hỏng lịch sử; nhiều request hoàn tất/cập nhật đồng thời cho kết quả nhất quán |
| [ ] P4.1 Cân nặng/BMI và progress | IMPLEMENTED | WeightProgressService, ProfileService, BmiCalculator; test tổng hợp và thứ tự bản ghi | P0/P3; timezone đã chốt | BMI boundaries và dữ liệu nhập sai pass; latest weight đúng; profile và history đồng bộ; streak/tuần đúng ranh giới múi giờ |
| [ ] P4.2 Chỉ số cơ thể ngoài cân nặng | PLANNED | Chưa có vòng eo/ngực…; không tự thêm bảng nếu chưa cần thesis MVP | Xác nhận danh sách chỉ số bắt buộc | Nếu cần: đơn vị, bounds, timestamp và API rõ; nếu không: ghi ngoài MVP trong đặc tả |
| [ ] P5.1 Thống kê/API readiness | PARTIAL | WeightProgressService tải toàn bộ; Swagger, exception responses và pagination | P3/P4 | Test user chưa có dữ liệu, tuần mới, streak đứt, pagination; tổng hợp đúng và truy vấn có giới hạn phù hợp |
| [ ] P5.2 Contract/E2E cho frontend | BLOCKED | DB chưa online; thiếu authenticated tests | P0–P5.1 | Register→profile→catalog→plan→session→weight→summary pass; enum/camelCase/GUID khớp Flutter; fixture demo không chứa secret |

Thứ tự khuyến nghị: P0.0 → P0.1 → P0.2 → P1 → P2 → P3 → P4 → P5.
Chưa cần xây admin role/UI để import: script/CLI được kiểm tra có thể đáp ứng thesis MVP.
Google login, refresh token/reset password/email verification là các quyết định mở rộng cần chốt
sau email auth cốt lõi; Google chưa được thực hiện. Nutrition và premium: NOT APPLICABLE cho MVP.

## Đầu vào import bài tập cần thu thập

Nguồn hiện có: 25 record trong ExerciseSeed và 7 PNG cho giao diện Flutter. Chưa thấy dataset
ngoài với schema rõ trong phạm vi tìm kiếm; không tạo thêm record mẫu để thay cho tài liệu người dùng.

- Nhận file CSV/JSON hoặc bảng gốc cùng encoding, nguồn/giấy phép, ngôn ngữ, phiên bản và số lượng.
- Mapping tối thiểu: Name (≤150), Slug unique (≤160), MuscleGroup (≤80), Difficulty
  (Beginner/Intermediate/Advanced), ExerciseType (Repetition/Duration), Instructions/Description.
- Sets/reps/duration/rest dùng int và đơn vị rõ; Repetition cần reps, Duration cần seconds;
  quy định null và giới hạn dương trước khi ghi DB. ImageUrl ≤1000, có thể null; video chưa có field.
- Giữ slug mà generator tham chiếu hoặc sửa mapping generator có kiểm thử. Không dùng ID g1…g8
  của guest làm GUID backend; chuẩn hóa khác biệt reverse-lunge/forward-lunge và muscle/difficulty.
- Ảnh chibi/hero hiện tại không chứng minh tương ứng với kỹ thuật từng bài. Kiểm tra đường dẫn,
  media type, kích thước và quyền phân phối; không dùng đường dẫn tuyệt đối của máy phát triển.
- Import cần dry-run, báo lỗi theo dòng, transaction, chính sách upsert/ID ổn định và tránh ghi đè
  lịch sử. Seed migration cũ giữ nguyên; dữ liệu mới qua migration mới hoặc pipeline được kiểm thử.

## Những việc frontend sẽ cần sau backend

Reset providers khi logout/đổi user; phân biệt 404 profile với 401; thêm PUT profile và entrypoint
catalog; dùng latest weight; cancel/resume session; chốt total reps và pause duration;
hiển thị lỗi complete/end; cấu hình API cho emulator/device; Android release INTERNET/HTTPS.
Không thực hiện các thay đổi đó trong audit/migration này.
