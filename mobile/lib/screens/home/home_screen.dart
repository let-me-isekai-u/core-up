part of '../core_up_screens.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String? _loadingMessage;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (context.read<AuthProvider>().isAuthenticated) {
        _loadAuthenticatedHome();
      }
    });
  }

  Future<void> _loadAuthenticatedHome() async {
    setState(() => _loadingMessage = 'Đang chuẩn bị kế hoạch của bạn...');
    await withMinimumLoading(() async {
      await Future.wait([
        context.read<ProfileProvider>().load(),
        context.read<WorkoutProvider>().home(),
        context.read<WorkoutProvider>().loadProgress(),
      ]);
    });
    if (mounted) setState(() => _loadingMessage = null);
  }

  String get _greeting {
    final hour = DateTime.now().hour;
    if (hour >= 5 && hour < 12) return 'Chào buổi sáng';
    if (hour >= 12 && hour < 18) return 'Chào buổi chiều';
    return 'Chào buổi tối';
  }

  @override
  Widget build(BuildContext context) {
    final isGuest = context.watch<AuthProvider>().isGuest;
    if (_loadingMessage != null) {
      return CoreUpChibiLoading(message: _loadingMessage);
    }
    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 28),
        children: isGuest ? _guestContent() : _authenticatedContent(),
      ),
    );
  }

  List<Widget> _guestContent() {
    final workout = context.watch<WorkoutProvider>();
    return [
      const Text(
        'Sẵn sàng tập luyện?',
        style: TextStyle(fontSize: 30, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 24),
      const SectionLabel('BUỔI TẬP MẪU'),
      SurfaceCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'CoreUp Quick Workout',
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            const WorkoutFeature(
              icon: Icons.fitness_center_rounded,
              label: '6 bài tập',
            ),
            const WorkoutFeature(
              icon: Icons.schedule_rounded,
              label: 'Khoảng 15–20 phút',
            ),
            const WorkoutFeature(
              icon: Icons.home_work_outlined,
              label: 'Không cần thiết bị',
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              label: 'BẮT ĐẦU TẬP',
              onTap: () {
                workout.startGuestWorkout();
                Navigator.of(
                  context,
                ).push(fadeRoute(const WorkoutSessionScreen()));
              },
            ),
          ],
        ),
      ),
      const SizedBox(height: 14),
      SurfaceCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Muốn có kế hoạch dành cho bạn?',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            const Text(
              'Đăng nhập để cá nhân hóa lịch tập và lưu tiến độ.',
              style: TextStyle(color: muted, height: 1.4),
            ),
            const SizedBox(height: 14),
            OutlinedButton(
              onPressed: () => openLogin(context),
              child: const Text('ĐĂNG NHẬP'),
            ),
          ],
        ),
      ),
    ];
  }

  List<Widget> _authenticatedContent() {
    final profile = context.watch<ProfileProvider>().data;
    final workout = context.watch<WorkoutProvider>();
    final progress = workout.progress;
    final exercises = workout.plan?['exercises'] as List?;
    return [
      Text(
        '$_greeting, ${formatDisplayName(profile?['fullName']?.toString() ?? 'Vận động viên')}',
        style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 24),
      const SectionLabel('KẾ HOẠCH HÔM NAY'),
      Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(22),
          gradient: const LinearGradient(colors: [blue, Color(0xff0a397c)]),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              workout.plan == null
                  ? 'Kế hoạch đang chờ bạn'
                  : localizeValue(workout.plan!['name'].toString()),
              style: const TextStyle(fontSize: 21, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 6),
            Text(
              workout.plan == null
                  ? 'Tạo kế hoạch cá nhân hóa từ hồ sơ của bạn.'
                  : '${exercises?.length ?? 0} bài • ${workout.plan!['estimatedDurationMinutes']} phút',
            ),
            const SizedBox(height: 18),
            ElevatedButton(
              onPressed: workout.plan == null
                  ? _generatePlan
                  : () async {
                      setState(
                        () => _loadingMessage = 'Đang chuẩn bị buổi tập...',
                      );
                      final started = await withMinimumLoading(workout.start);
                      if (!mounted) return;
                      setState(() => _loadingMessage = null);
                      if (started) {
                        Navigator.of(
                          context,
                        ).push(fadeRoute(const WorkoutSessionScreen()));
                      }
                    },
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.white,
                foregroundColor: bg,
              ),
              child: Text(
                workout.plan == null ? 'TẠO KẾ HOẠCH' : 'BẮT ĐẦU TẬP',
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 18),
      Row(
        children: [
          StatCard('TUẦN NÀY', (progress?['workoutsThisWeek'] ?? 0).toString()),
          const SizedBox(width: 10),
          StatCard('CHUỖI', '${progress?['currentStreakDays'] ?? 0} ngày'),
          const SizedBox(width: 10),
          StatCard(
            'CÂN NẶNG',
            progress?['currentWeightKg'] == null
                ? '—'
                : '${progress!['currentWeightKg']} kg',
          ),
        ],
      ),
      if (_needsWeightCheckIn(workout.weights)) ...[
        const SizedBox(height: 14),
        const SectionLabel('CẬP NHẬT CÂN NẶNG'),
        SurfaceCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Cập nhật cân nặng tuần này',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 6),
              const Text(
                'Theo dõi thay đổi để CoreUp đánh giá tiến trình của bạn.',
                style: TextStyle(color: muted, height: 1.4),
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: () => showModalBottomSheet(
                  context: context,
                  backgroundColor: surface,
                  builder: (_) => WeightSheet(provider: workout),
                ),
                child: const Text('CẬP NHẬT'),
              ),
            ],
          ),
        ),
      ],
      if (workout.history.isNotEmpty) ...[
        const SizedBox(height: 14),
        const SectionLabel('GẦN ĐÂY'),
        SurfaceCard(
          child: ListTile(
            contentPadding: EdgeInsets.zero,
            leading: const CircleAvatar(
              backgroundColor: AppColors.surfaceElevated,
              child: Icon(Icons.history_rounded, color: blue),
            ),
            title: Text(
              workout.history.first['workoutName']?.toString() ?? 'Buổi tập',
            ),
            subtitle: Text(
              formatVietnameseDate(
                DateTime.parse(workout.history.first['startedAt'].toString()),
              ),
            ),
            trailing: Text(
              formatDuration(
                (workout.history.first['durationSeconds'] as num? ?? 0).toInt(),
              ),
            ),
          ),
        ),
      ],
    ];
  }

  bool _needsWeightCheckIn(List<Map<String, dynamic>> weights) {
    if (weights.isEmpty) return true;
    final dates = weights
        .map((record) => DateTime.tryParse(record['recordedAt'].toString()))
        .whereType<DateTime>()
        .toList();
    if (dates.isEmpty) return true;
    dates.sort();
    return DateTime.now().difference(dates.last).inDays >= 7;
  }

  Future<void> _generatePlan() async {
    setState(() => _loadingMessage = 'Đang tạo kế hoạch...');
    await withMinimumLoading(context.read<WorkoutProvider>().generate);
    if (mounted) setState(() => _loadingMessage = null);
  }
}

class WorkoutFeature extends StatelessWidget {
  const WorkoutFeature({super.key, required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      children: [
        Icon(icon, size: 19, color: blue),
        const SizedBox(width: 10),
        Text(label, style: const TextStyle(color: muted, fontSize: 15)),
      ],
    ),
  );
}
