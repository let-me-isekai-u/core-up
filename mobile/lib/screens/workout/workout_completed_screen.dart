part of '../core_up_screens.dart';

class WorkoutCompleteScreen extends StatefulWidget {
  const WorkoutCompleteScreen({super.key});
  @override
  State<WorkoutCompleteScreen> createState() => _WorkoutCompleteScreenState();
}

class _WorkoutCompleteScreenState extends State<WorkoutCompleteScreen> {
  var second = false;
  Timer? timer;
  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(milliseconds: 650), (_) {
      if (mounted) setState(() => second = !second);
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final session = context.watch<WorkoutProvider>().session;
    final guest = context.watch<AuthProvider>().isGuest;
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Spacer(),
              SizedBox(
                height: 260,
                child: Image.asset(
                  second
                      ? 'assets/workout_completed_hero2.png'
                      : 'assets/workout_completed_hero.png',
                  fit: BoxFit.contain,
                  gaplessPlayback: true,
                ),
              ),
              const SizedBox(height: 24),
              const Text(
                'HOÀN THÀNH BUỔI TẬP',
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 27, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  StatCard(
                    'THỜI LƯỢNG',
                    formatDuration(session?['durationSeconds'] ?? 0),
                  ),
                  const SizedBox(width: 10),
                  StatCard(
                    'BÀI TẬP',
                    (session?['completedExercises'] ?? 0).toString(),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              if (guest)
                SurfaceCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Bạn đang dùng chế độ khách',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 6),
                      const Text(
                        'Kết quả này sẽ không được lưu. Đăng nhập để theo dõi lịch sử, cân nặng và nhận kế hoạch cá nhân hóa.',
                        style: TextStyle(color: muted, height: 1.4),
                      ),
                      const SizedBox(height: 8),
                      TextButton(
                        onPressed: () => openLogin(context),
                        child: const Text('ĐĂNG NHẬP ĐỂ LƯU TIẾN TRÌNH'),
                      ),
                    ],
                  ),
                )
              else
                const Padding(
                  padding: EdgeInsets.symmetric(vertical: 12),
                  child: Text(
                    'Đã lưu vào tiến trình của bạn.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: AppColors.accent),
                  ),
                ),
              const Spacer(),
              if (!guest) ...[
                OutlinedButton(
                  onPressed: () => Navigator.of(context).pushAndRemoveUntil(
                    fadeRoute(const ShellScreen(initialTab: 1)),
                    (_) => false,
                  ),
                  child: const Text('XEM TIẾN ĐỘ'),
                ),
                const SizedBox(height: 10),
              ],
              PrimaryButton(
                label: 'VỀ TRANG CHỦ',
                onTap: () => Navigator.of(context).pushAndRemoveUntil(
                  fadeRoute(const ShellScreen()),
                  (_) => false,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
