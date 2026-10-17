# Sơ đồ hiện trạng

Nguồn ngày 17/10/2026: Entities.cs, CoreUpDbContext, InitialMvp + snapshot,
controllers/application services và Flutter providers/screens. EF đã xác nhận không model drift.

- `use-case-current.puml`: PlantUML, cả API và mobile, đánh dấu nơi chỉ có API hoặc UI chưa nối.
  Guest là chế độ local của mobile, không phải role backend. Không có Administrator.
- `erd-current.mmd`: Mermaid ER, 8 bảng, 9 FK. Tên thuộc tính theo C# / cột migration;
  enum lưu string trong PostgreSQL; nullable ghi trong comment thuộc tính.
- `domain-classes.mmd`: Mermaid class, thuộc tính scalar và cardinality hiện tại.
- `architecture-current.mmd`: Mermaid flowchart; đường implements là dependency abstraction.
  Không có external authentication provider; JWT được ký cục bộ, không phải Google OAuth.

Plan–Exercise và Session–Exercise là many-to-many thông qua entity nối với Id riêng.
Quan hệ User–Profile cho phép user chưa có profile. Session có thể không còn plan;
FK WorkoutPlanId nullable, delete SET NULL. Các FK Exercise RESTRICT; sở hữu còn lại CASCADE.
Một plan/session có thể có 0 bài ở cấp schema; API thường tạo với bài nhưng DB không ép min=1.

Không tạo diagram proposed vì chưa chốt thiết kế lịch ngày. Không cài renderer mới;
CLI `plantuml` và `mmdc` không có trong PATH. Source được rà cấu trúc/cross-reference;
chưa có preview hay xác nhận bằng renderer trong lần audit này.
