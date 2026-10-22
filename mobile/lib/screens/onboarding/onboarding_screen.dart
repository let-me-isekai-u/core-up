import 'package:flutter/material.dart';

import '../../core/storage/local_storage_service.dart';
import '../../core/theme/app_colors.dart';

class OnboardingScreen extends StatefulWidget {
  const OnboardingScreen({super.key, required this.destinationBuilder});
  final WidgetBuilder destinationBuilder;

  @override
  State<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  final _controller = PageController();
  var _page = 0;

  Future<void> _finish() async {
    await LocalStorageService().markOnboardingSeen();
    if (!mounted) return;
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: widget.destinationBuilder),
      (_) => false,
    );
  }

  void _next() {
    if (_page == 2) {
      _finish();
    } else {
      _controller.nextPage(
        duration: const Duration(milliseconds: 320),
        curve: Curves.easeOutCubic,
      );
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: LayoutBuilder(
        builder: (context, constraints) => Padding(
          padding: const EdgeInsets.fromLTRB(24, 12, 24, 24),
          child: Column(
            children: [
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: _finish,
                  child: const Text('Bỏ qua'),
                ),
              ),
              Expanded(
                child: PageView(
                  controller: _controller,
                  onPageChanged: (value) => setState(() => _page = value),
                  children: const [
                    _OnboardingPage(
                      title: 'Bắt đầu theo cách của bạn',
                      description:
                          'Guest tập giáo trình Core Beginner cố định. Tài khoản Free có questionnaire và chọn mức Dễ/Vừa/Khó.',
                      visual: _DifficultyVisual(),
                    ),
                    _OnboardingPage(
                      title: 'Giáo trình chuẩn theo mức',
                      description:
                          'Đăng nhập, trả lời 7 câu hỏi và xác nhận mức tập để nhận giáo trình Core chuẩn. Số đo cơ thể là tùy chọn.',
                      visual: _ProfileVisual(),
                    ),
                    _OnboardingPage(
                      title: 'Theo dõi từng bước tiến bộ',
                      description:
                          'Lưu lịch sử, tổng thời gian và chuỗi ngày tập. Bạn có thể cập nhật số đo mỗi tháng hoặc để sau.',
                      visual: _ProgressVisual(),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 18),
              _Indicator(activeIndex: _page),
              const SizedBox(height: 22),
              SizedBox(
                width: double.infinity,
                height: 54,
                child: ElevatedButton(
                  onPressed: _next,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                  ),
                  child: Text(
                    _page == 2 ? 'Bắt đầu' : 'Tiếp tục',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _OnboardingPage extends StatelessWidget {
  const _OnboardingPage({
    required this.title,
    required this.description,
    required this.visual,
  });
  final String title;
  final String description;
  final Widget visual;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) => Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Flexible(
          flex: 6,
          child: AnimatedScale(
            duration: const Duration(milliseconds: 300),
            scale: 1,
            child: visual,
          ),
        ),
        const SizedBox(height: 34),
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            fontSize: 29,
            fontWeight: FontWeight.w800,
            color: AppColors.textPrimary,
          ),
        ),
        const SizedBox(height: 14),
        Text(
          description,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: AppColors.textSecondary,
            fontSize: 16,
            height: 1.45,
          ),
        ),
      ],
    ),
  );
}

class _DifficultyVisual extends StatelessWidget {
  const _DifficultyVisual();
  @override
  Widget build(BuildContext context) => Stack(
    alignment: Alignment.center,
    children: [
      Container(
        width: 215,
        height: 215,
        decoration: const BoxDecoration(
          shape: BoxShape.circle,
          gradient: RadialGradient(
            colors: [Color(0xff205cb2), AppColors.surface],
          ),
        ),
        child: const Icon(
          Icons.fitness_center_rounded,
          color: Colors.white,
          size: 78,
        ),
      ),
      const Positioned(
        left: 4,
        top: 44,
        child: _MiniChip('Dễ', Icons.bolt_rounded),
      ),
      const Positioned(
        right: 0,
        top: 92,
        child: _MiniChip('Vừa', Icons.speed_rounded),
      ),
      const Positioned(
        bottom: 20,
        child: _MiniChip('Khó', Icons.local_fire_department_rounded),
      ),
    ],
  );
}

class _ProfileVisual extends StatelessWidget {
  const _ProfileVisual();
  @override
  Widget build(BuildContext context) => Stack(
    alignment: Alignment.center,
    children: [
      Container(
        width: 156,
        height: 156,
        decoration: BoxDecoration(
          color: AppColors.surface,
          shape: BoxShape.circle,
          border: Border.all(color: AppColors.primary, width: 2),
        ),
        child: const Icon(
          Icons.person_outline_rounded,
          size: 78,
          color: AppColors.primary,
        ),
      ),
      const Positioned(
        left: 0,
        top: 30,
        child: _MiniChip('Mục tiêu', Icons.flag_rounded),
      ),
      const Positioned(
        right: 0,
        top: 54,
        child: _MiniChip('Cân nặng', Icons.monitor_weight_outlined),
      ),
      const Positioned(
        left: 20,
        bottom: 14,
        child: _MiniChip('Trình độ', Icons.speed_rounded),
      ),
      const Positioned(
        right: 0,
        bottom: 2,
        child: _MiniChip('Lịch tập', Icons.calendar_month_rounded),
      ),
    ],
  );
}

class _ProgressVisual extends StatelessWidget {
  const _ProgressVisual();
  @override
  Widget build(BuildContext context) => Container(
    width: 270,
    padding: const EdgeInsets.all(22),
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(28),
      border: Border.all(color: Colors.white12),
    ),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Icon(Icons.auto_graph_rounded, color: AppColors.accent),
            SizedBox(width: 8),
            Text('Tiến độ', style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        const SizedBox(height: 25),
        Row(
          children: [
            SizedBox(
              width: 76,
              height: 76,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  CircularProgressIndicator(
                    value: .72,
                    strokeWidth: 8,
                    backgroundColor: AppColors.surfaceElevated,
                    color: AppColors.primary,
                  ),
                  const Text(
                    '72%',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 18),
            Expanded(
              child: Column(
                children: [
                  _ProgressBar(width: .88),
                  SizedBox(height: 12),
                  _ProgressBar(width: .62),
                  SizedBox(height: 12),
                  _ProgressBar(width: .78),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        const Row(
          children: [
            Icon(Icons.check_circle_rounded, color: Color(0xff4fc47b)),
            SizedBox(width: 8),
            Text('Bạn đang tiến bộ từng ngày'),
          ],
        ),
      ],
    ),
  );
}

class _ProgressBar extends StatelessWidget {
  const _ProgressBar({required this.width});
  final double width;
  @override
  Widget build(BuildContext context) => ClipRRect(
    borderRadius: BorderRadius.circular(20),
    child: LinearProgressIndicator(
      value: width,
      minHeight: 8,
      backgroundColor: AppColors.surfaceElevated,
      color: AppColors.accent,
    ),
  );
}

class _MiniChip extends StatelessWidget {
  const _MiniChip(this.label, this.icon);
  final String label;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: Colors.white12),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 15, color: AppColors.accent),
        const SizedBox(width: 5),
        Text(
          label,
          style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
        ),
      ],
    ),
  );
}

class _Indicator extends StatelessWidget {
  const _Indicator({required this.activeIndex});
  final int activeIndex;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisAlignment: MainAxisAlignment.center,
    children: List.generate(
      3,
      (index) => AnimatedContainer(
        duration: const Duration(milliseconds: 240),
        curve: Curves.easeOut,
        width: activeIndex == index ? 28 : 8,
        height: 8,
        margin: const EdgeInsets.symmetric(horizontal: 4),
        decoration: BoxDecoration(
          color: activeIndex == index
              ? AppColors.primary
              : AppColors.surfaceElevated,
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    ),
  );
}
