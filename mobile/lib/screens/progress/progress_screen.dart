part of '../core_up_screens.dart';

class ProgressScreen extends StatefulWidget {
  const ProgressScreen({super.key});
  @override
  State<ProgressScreen> createState() => _ProgressScreenState();
}

class _ProgressScreenState extends State<ProgressScreen> {
  var _tab = 0;
  var _loading = false;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      if (!context.read<AuthProvider>().isAuthenticated) return;
      setState(() => _loading = true);
      await withMinimumLoading(context.read<WorkoutProvider>().loadProgress);
      if (mounted) setState(() => _loading = false);
    });
  }

  @override
  Widget build(BuildContext context) {
    if (context.watch<AuthProvider>().isGuest) {
      return SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            const Text(
              'TIẾN ĐỘ',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 24),
            SurfaceCard(
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 24),
                child: Column(
                  children: [
                    const Icon(Icons.insights_rounded, size: 58, color: blue),
                    const SizedBox(height: 18),
                    const Text(
                      'Theo dõi sự thay đổi của bạn',
                      style: TextStyle(
                        fontSize: 21,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 10),
                    const Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        BenefitLine('Lưu lịch sử tập luyện'),
                        BenefitLine('Theo dõi cân nặng'),
                        BenefitLine('Xem tiến trình theo tuần'),
                        BenefitLine('Nhận kế hoạch cá nhân hóa'),
                      ],
                    ),
                    const SizedBox(height: 20),
                    PrimaryButton(
                      label: 'ĐĂNG NHẬP',
                      onTap: () => openLogin(context),
                    ),
                    const SizedBox(height: 12),
                    const Text(
                      'Bạn vẫn có thể tiếp tục tập nhanh mà không cần tài khoản.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: muted, fontSize: 12),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      );
    }
    if (_loading) {
      return const CoreUpChibiLoading(message: 'Đang tải tiến trình...');
    }
    final data = context.watch<WorkoutProvider>();
    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'TIẾN ĐỘ',
            style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 20),
          SegmentedButton<int>(
            segments: const [
              ButtonSegment(value: 0, label: Text('Tập luyện')),
              ButtonSegment(value: 1, label: Text('Cân nặng')),
            ],
            selected: {_tab},
            onSelectionChanged: (value) => setState(() => _tab = value.first),
          ),
          const SizedBox(height: 20),
          if (_tab == 0) ...[
            Row(
              children: [
                StatCard(
                  'TỔNG BUỔI TẬP',
                  (data.progress?['totalWorkouts'] ?? 0).toString(),
                ),
                const SizedBox(width: 10),
                StatCard(
                  'TUẦN NÀY',
                  (data.progress?['workoutsThisWeek'] ?? 0).toString(),
                ),
              ],
            ),
            const SizedBox(height: 20),
            const SectionLabel('HOẠT ĐỘNG GẦN ĐÂY'),
            if (data.history.isEmpty)
              const EmptyState(
                icon: Icons.event_available_rounded,
                title: 'Chưa có đủ dữ liệu',
                message:
                    'Hoàn thành thêm vài buổi tập để xem xu hướng của bạn.',
              )
            else
              ...data.history.map(
                (s) => SurfaceCard(
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(s['workoutName'] ?? 'Buổi tập'),
                    subtitle: Text(
                      formatVietnameseDate(DateTime.parse(s['startedAt'])),
                    ),
                    trailing: Text(formatDuration(s['durationSeconds'] ?? 0)),
                  ),
                ),
              ),
          ] else ...[
            SurfaceCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'CÂN NẶNG HIỆN TẠI',
                    style: TextStyle(
                      color: muted,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    data.weights.isEmpty
                        ? '—'
                        : '${data.weights.first['weightKg']} kg',
                    style: const TextStyle(
                      fontSize: 30,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ],
              ),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'LỊCH SỬ CÂN NẶNG',
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                TextButton(
                  onPressed: () => showModalBottomSheet(
                    context: context,
                    backgroundColor: surface,
                    builder: (_) => WeightSheet(provider: data),
                  ),
                  child: const Text('THÊM CÂN NẶNG'),
                ),
              ],
            ),
            if (data.weights.isEmpty)
              const Padding(
                padding: EdgeInsets.all(20),
                child: Text(
                  'Chưa có dữ liệu cân nặng',
                  style: TextStyle(color: muted),
                ),
              )
            else
              ...data.weights.map(
                (w) => ListTile(
                  title: Text(w['weightKg'].toString() + ' kg'),
                  trailing: Text(
                    formatVietnameseDate(DateTime.parse(w['recordedAt'])),
                  ),
                ),
              ),
          ],
        ],
      ),
    );
  }
}

class WeightSheet extends StatefulWidget {
  const WeightSheet({super.key, required this.provider});
  final WorkoutProvider provider;
  @override
  State<WeightSheet> createState() => _WeightSheetState();
}

class _WeightSheetState extends State<WeightSheet> {
  final controller = TextEditingController();
  var _saving = false;
  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _saving
      ? const SizedBox(
          height: 280,
          child: CoreUpChibiLoading(message: 'Đang cập nhật cân nặng...'),
        )
      : Padding(
          padding: EdgeInsets.fromLTRB(
            24,
            24,
            24,
            MediaQuery.of(context).viewInsets.bottom + 24,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'THÊM CÂN NẶNG',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: controller,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Cân nặng (kg)'),
              ),
              const SizedBox(height: 16),
              PrimaryButton(
                label: 'LƯU',
                onTap: () async {
                  setState(() => _saving = true);
                  final saved = await withMinimumLoading(
                    () => widget.provider.weight(
                      double.tryParse(controller.text) ?? 0,
                    ),
                  );
                  if (!mounted) return;
                  setState(() => _saving = false);
                  if (saved) {
                    Navigator.of(context).pop();
                  }
                },
              ),
            ],
          ),
        );
}
