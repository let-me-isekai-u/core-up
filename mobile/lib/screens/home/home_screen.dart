part of '../core_up_screens.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  String? _loadingMessage;
  bool _measurementDeferred = false;

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
        context.read<EntitlementProvider>().refresh(),
        context.read<ProfileProvider>().load(),
        context.read<WorkoutProvider>().home(),
        context.read<WorkoutProvider>().loadProgress(),
      ]);
    });
    if (!mounted) return;
    final profileId = context.read<ProfileProvider>().data?['id']?.toString();
    if (profileId != null) {
      _measurementDeferred = await context.read<LocalStorageService>().isMeasurementReviewDeferred(profileId);
    }
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
              'Core Beginner · Khách',
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            const WorkoutFeature(
              icon: Icons.fitness_center_rounded,
              label: '6 bài tập',
            ),
            const WorkoutFeature(
              icon: Icons.schedule_rounded,
              label: 'Giáo trình cố định · Mới bắt đầu',
            ),
            const WorkoutFeature(
              icon: Icons.home_work_outlined,
              label: 'Chuẩn bị thảm tập',
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
              'Muốn chọn giáo trình theo mức?',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            const Text(
              'Đăng nhập để trả lời questionnaire, chọn giáo trình chuẩn và lưu tiến độ.',
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
    final profileProvider = context.watch<ProfileProvider>();
    if (profileProvider.error != null && profile == null) {
      return [
        Text(profileProvider.error!),
        TextButton(onPressed: _loadAuthenticatedHome, child: const Text('THỬ TẢI LẠI')),
      ];
    }
    if (profile?['assessment'] == null) {
      return [
        const Text('BẮT ĐẦU VỚI FREE', style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800)),
        const SizedBox(height: 20),
        const Text('Trả lời 7 câu hỏi và xác nhận mức Dễ/Vừa/Khó để nhận giáo trình Core chuẩn. Không bắt buộc nhập số đo.'),
        const SizedBox(height: 16),
        PrimaryButton(label: 'CHỌN MỨC TẬP', onTap: () => Navigator.of(context).push(fadeRoute(const ProfileSetupScreen(review: true)))),
        const SizedBox(height: 12),
        const Text('Bạn có thể để sau và xem tiến độ đã có. Chỉ bắt đầu buổi Free sau khi xác nhận mức tập.'),
      ];
    }
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
                  : 'Core chuẩn · ${_levelLabel(workout.plan!['difficulty'].toString())}',
              style: const TextStyle(fontSize: 21, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 6),
            Text(
              workout.plan == null
                  ? 'Nhận giáo trình chuẩn theo mức đã xác nhận.'
                  : '${exercises?.length ?? 0} bài • ${workout.plan!['estimatedDurationMinutes']} phút',
            ),
            const SizedBox(height: 12),
            const Text('Cấu trúc chung cho tài khoản Free cùng mức; không tự điều chỉnh theo số đo.'),
            if (exercises != null)
              for (final item in exercises) ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(item['exercise']['name'].toString()),
                subtitle: Text('${item['sets']} hiệp × ${item['reps'] ?? item['durationSeconds']} ${item['durationSeconds'] == null ? 'lần' : 'giây'}'),
                trailing: const Icon(Icons.chevron_right_rounded),
                onTap: () => Navigator.of(context).push(fadeRoute(ExerciseDetailScreen(item: Map<String, dynamic>.from(item['exercise'])))),
              ),
            TextButton(onPressed: () => Navigator.of(context).push(fadeRoute(const ProfileSetupScreen(review: true))),
              child: const Text('XEM LẠI QUESTIONNAIRE / ĐỔI MỨC')),
            if (workout.error != null) Text(workout.error!, style: const TextStyle(color: Colors.orangeAccent)),
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
                      } else {
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                          content: Text(workout.error ?? 'Chưa bắt đầu được buổi tập. Vui lòng thử lại.'),
                        ));
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
      if (!_measurementDeferred && _needsWeightCheckIn(workout.weights)) ...[
        const SizedBox(height: 14),
        const SectionLabel('CẬP NHẬT CÂN NẶNG'),
        SurfaceCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Cập nhật số đo tháng này (tùy chọn)',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 6),
              const Text(
                'Bạn có thể để sau. Số đo không tự thay đổi giáo trình.',
                style: TextStyle(color: muted, height: 1.4),
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: _reviewMeasurements,
                child: const Text('CẬP NHẬT SỐ ĐO'),
              ),
              TextButton(onPressed: () async {
                final id = profile?['id']?.toString();
                if (id != null) await context.read<LocalStorageService>().deferMeasurementReview(id);
                if (mounted) setState(() => _measurementDeferred = true);
              }, child: const Text('ĐỂ SAU')),
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

  Future<void> _reviewMeasurements() async {
    final saved = await Navigator.of(context).push<bool>(fadeRoute(const MeasurementsScreen()));
    if (saved == true && mounted) setState(() => _measurementDeferred = true);
  }

  bool _needsWeightCheckIn(List<Map<String, dynamic>> weights) {
    if (weights.isEmpty) return true;
    final dates = weights
        .map((record) => DateTime.tryParse(record['recordedAt'].toString()))
        .whereType<DateTime>()
        .toList();
    if (dates.isEmpty) return true;
    dates.sort();
    return DateTime.now().difference(dates.last).inDays >= 30;
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
