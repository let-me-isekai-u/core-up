# API routes hiện tại

Nguồn: Swagger từ host Development cục bộ ngày 17/10/2026. `/health` là minimal endpoint; Swagger không phải kiểm thử nghiệp vụ.

| Method | Route |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| GET | `/health` |
| GET | `/api/exercises` |
| GET | `/api/exercises/{id}` |
| GET | `/api/profile` |
| POST | `/api/profile` |
| PUT | `/api/profile` |
| GET | `/api/progress/summary` |
| GET | `/api/weight-records` |
| POST | `/api/weight-records` |
| GET | `/api/workout-plans` |
| GET | `/api/workout-plans/active` |
| GET | `/api/workout-plans/{id}` |
| POST | `/api/workout-plans/generate` |
| POST | `/api/workout-sessions/start` |
| GET | `/api/workout-sessions/{id}` |
| PUT | `/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}` |
| POST | `/api/workout-sessions/{id}/complete` |
| POST | `/api/workout-sessions/{id}/cancel` |
| GET | `/api/workout-sessions/history` |
