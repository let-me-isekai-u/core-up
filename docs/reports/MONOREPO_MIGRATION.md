# Hợp nhất repository — 17/10/2026

## Nguồn và điểm an toàn

- Main: `/Users/nhatanhly/core-up`, origin `https://github.com/let-me-isekai-u/core-up.git`.
- Flutter: `/Users/nhatanhly/core_up_mobile`, package core_up_mobile, không có Git metadata.
- HEAD trước migration: `5b11a77`; tag local `coreup/pre-monorepo-20261017` trỏ commit này.
- Nhánh thực hiện: `codex/monorepo-audit`, từ main hiện tại. Không force-push/push/xóa repo.
- Ban đầu không có thay đổi tracked. Hai generator untracked, các thư mục .coreup-*/output
  đã có sẵn: giữ nguyên trên đĩa; không stage/commit. .env gốc giữ nguyên và không đọc/in secret.

## Cấu trúc cuối

```text
core-up/
├── CoreUp.sln
├── src/{CoreUp.Api,CoreUp.Application,CoreUp.Domain,CoreUp.Infrastructure}/
├── tests/CoreUp.Tests/
├── docker-compose.yml
├── dotnet-tools.json
├── .env.example
├── mobile/{lib,assets,android,ios,linux,macos,web,windows,test}/
├── mobile/pubspec.yaml
├── mobile/pubspec.lock
├── docs/diagrams/
├── docs/reports/
├── README.md
└── .gitignore
```

Giữ backend ở root theo phương án được cho phép. Lý do: solution/project references,
README/script docker_command, EnvFile và tooling đều dựa vào root; chuyển đồng thời trong lúc
DB không thể xác minh sẽ tăng diện tích rủi ro mà không cần thiết cho import mobile.
Không đổi API contracts, migration, package versions hoặc source nghiệp vụ.

## Chiến lược và thay đổi

1. Kiểm tra main/remotes/history/status và mobile trước sửa; mobile không có Git nên subtree
   không có lịch sử nguồn để merge. Không init Git giả hay tạo repository mobile thay thế.
2. Tạo tag rollback và nhánh riêng. Lịch sử backend còn nguyên ancestry, không rewrite.
3. Rsync bản sao mobile, nguồn gốc không bị thay đổi. Loại cache/build/.git/Pods/ephemeral,
   IDE metadata, local.properties, generated local configs, signing key/keystore.
4. `.gitignore` root thêm Flutter/cache/secret patterns và output báo cáo cục bộ.
   Nested platform gitignore vẫn giữ. Generated plugin registrants tiêu chuẩn được Flutter
   tái tạo có thể được track theo template nền tảng; không có dependency cache trong commit.
5. Sửa mobile README bỏ đường dẫn máy cá nhân, viết lại root README từ nội dung đã đọc,
   thêm hướng dẫn riêng backend/mobile, sơ đồ hiện trạng, ba báo cáo và inventory API runtime.
6. Commit import: `7e8c6a1` — chore: import CoreUp Flutter source into mobile workspace.
   Commit kế tiếp giữ Android debug manifest bị rule .NET `[Dd]ebug/` che;
   thêm ngoại lệ ignore cho source này. Commit tài liệu ở bước sau; hash xem `git log -3 --oneline`.

Đã đối chiếu byte 34 file trong lib/test/assets (loại .DS_Store): tất cả trùng nguồn.
Pubspec.lock không đổi sau pub get. Không có nested .git trong mobile.
Các source platform và assets gốc được nhập, không sao chép .env, local DB hay khóa ký.
Flutter tạo cache mới khi validate; cache bị ignore, không thuộc snapshot Git.

## Validation

| Kiểm tra | Kết quả |
|---|---|
| dotnet restore | PASS |
| dotnet build --no-restore | PASS, 0 warnings/errors |
| dotnet test --no-build | PASS, 8/8 |
| dotnet tool restore | PASS |
| EF model consistency | PASS, no pending changes |
| EF migrations list | Có InitialMvp; DB BLOCKED, không xác định applied status dù exit 0 |
| API smoke cổng 15027 | Swagger 200; 3 protected GET 401; health 503 |
| flutter pub get | PASS, lock giữ nguyên |
| flutter analyze | PASS, no issues |
| flutter test | PASS, 4/4 |
| Native/release builds, authenticated E2E, apply DB | NOT RUN |
| Diagram rendering | NOT RUN, không thấy CLI có sẵn trong PATH |

Không launch Docker; không cần dừng container nào. Chỉ dừng host API do script audit khởi tạo.
Logs trong `validation/`, toàn bộ command transcript ở COMMAND_LOG.md.

## Rủi ro còn lại

PostgreSQL chưa kết nối; chưa kiểm chứng runtime ghi DB. Mobile không có lịch sử Git trước
migration; commit import chỉ ghi nhận snapshot nguồn hiện có. Chưa build Android/iOS release;
Android main thiếu INTERNET và iOS HTTP exception giới hạn localhost. Không thay đổi các lỗi
nghiệp vụ có sẵn. Tài liệu docker_command.txt cũ có lệnh nguy hiểm cho DB; README mới là entrypoint.

## Rollback không mất dữ liệu

- Xem snapshot cũ bằng `git show coreup/pre-monorepo-20261017:README.md`.
- Khi cần chạy backend gốc mà không đụng working tree: tạo checkout riêng với
  `git worktree add --detach ../core-up-before-monorepo coreup/pre-monorepo-20261017`.
  Worktree này không sao chép .env hay báo cáo untracked; cấp cấu hình riêng nếu cần chạy.
- Để quay main tại checkout hiện tại: kiểm tra `git status`, cất/commit mọi thay đổi mới trước;
  sau đó `git switch main`. Không dùng reset --hard hoặc git clean. Thư mục cache ignored có
  thể còn trên đĩa; không tự xóa. Flutter gốc `/Users/nhatanhly/core_up_mobile` vẫn nguyên vẹn.
- Nếu đã đưa migration lên nhánh dùng chung sau này, revert các commit migration theo thứ tự
  ngược và xem diff trước; không rewrite history. Không có thay đổi schema DB trong lần này.
- Các báo cáo/script untracked ban đầu nằm nguyên ở workspace ban đầu, không nằm trong tag;
  rollback Git không phải bản sao lưu các file đó. Không xóa workspace nguồn.
