import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/theme/app_colors.dart';
import '../../models/premium/premium_plan.dart';

class PremiumPaywallScreen extends StatefulWidget {
  const PremiumPaywallScreen({super.key});
  @override
  State<PremiumPaywallScreen> createState() => _PremiumPaywallScreenState();
}

class _PremiumPaywallScreenState extends State<PremiumPaywallScreen> {
  PremiumPlan _selected = PremiumPlan.options[1];

  void _close() => Navigator.of(context).pop();

  Future<void> _trial(BuildContext themedContext) => showDialog<void>(
    context: themedContext,
    builder: (dialogContext) => AlertDialog(
      title: const Text('Dùng thử Premium'),
      content: const Text(
        'Chương trình dùng thử Premium 7 ngày đang được hoàn thiện. Bạn vẫn có thể tiếp tục tập luyện miễn phí.',
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(dialogContext),
          child: const Text('ĐÃ HIỂU'),
        ),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    // A dark promotional surface, using the existing brand tokens. The user's
    // ThemeProvider preference is never mutated; surrounding routes retain it.
    final scheme = Theme.of(context).colorScheme.copyWith(
      brightness: Brightness.dark,
      primary: AppColors.primary,
      onPrimary: AppColors.textPrimary,
      secondary: AppColors.accent,
      surface: AppColors.surface,
      onSurface: AppColors.textPrimary,
      onSurfaceVariant: AppColors.textSecondary,
      surfaceContainerHighest: AppColors.surfaceElevated,
    );
    final theme = Theme.of(context).copyWith(
      brightness: Brightness.dark,
      colorScheme: scheme,
      textTheme: Theme.of(context).textTheme.apply(
        bodyColor: scheme.onSurface,
        displayColor: scheme.onSurface,
      ),
    );
    return Theme(
      data: theme,
      child: Builder(
        builder: (themedContext) => AnnotatedRegion<SystemUiOverlayStyle>(
          value: SystemUiOverlayStyle.light,
          child: Scaffold(
            backgroundColor: AppColors.background,
            body: SafeArea(
              child: Column(
                children: [
                  Align(
                    alignment: Alignment.centerRight,
                    child: IconButton(
                      tooltip: 'Đóng giới thiệu Premium',
                      constraints: const BoxConstraints(
                        minWidth: 48,
                        minHeight: 48,
                      ),
                      onPressed: _close,
                      icon: const Icon(Icons.close_rounded),
                    ),
                  ),
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                      child: Center(
                        child: ConstrainedBox(
                          constraints: const BoxConstraints(maxWidth: 480),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              ShaderMask(
                                blendMode: BlendMode.srcIn,
                                shaderCallback: (bounds) => LinearGradient(
                                  colors: [
                                    scheme.onSurface,
                                    scheme.secondary,
                                    scheme.primary,
                                  ],
                                ).createShader(bounds),
                                child: const Text(
                                  'CORE-UP PREMIUM',
                                  textAlign: TextAlign.center,
                                  style: TextStyle(
                                    fontSize: 29,
                                    fontWeight: FontWeight.w900,
                                    fontStyle: FontStyle.italic,
                                  ),
                                ),
                              ),
                              const SizedBox(height: 6),
                              const Text(
                                'Nâng cấp hành trình chinh phục cơ bụng 6 múi!',
                                textAlign: TextAlign.center,
                              ),
                              const SizedBox(height: 18),
                              // Source is 474×1024. A square window at y≈171…645 keeps
                              // feet, hands, head and torso together without distortion.
                              ClipRRect(
                                borderRadius: BorderRadius.circular(22),
                                child: AspectRatio(
                                  aspectRatio: 1,
                                  child: Image.asset(
                                    'assets/premium/premium_hero.png',
                                    fit: BoxFit.cover,
                                    alignment: const Alignment(0, -.38),
                                    semanticLabel:
                                        'Vận động viên tập cơ bụng với ánh sáng xanh',
                                  ),
                                ),
                              ),
                              const SizedBox(height: 18),
                              Text(
                                'QUYỀN LỢI PREMIUM DỰ KIẾN',
                                style: TextStyle(
                                  color: scheme.secondary,
                                  fontSize: 11,
                                  letterSpacing: 1.5,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              const SizedBox(height: 8),
                              const _PremiumBenefits(),
                              const SizedBox(height: 8),
                              Text(
                                'Các tính năng Premium đang được phát triển.',
                                style: TextStyle(
                                  color: scheme.onSurfaceVariant,
                                  fontSize: 12,
                                ),
                              ),
                              const SizedBox(height: 22),
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  for (final plan in PremiumPlan.options) ...[
                                    if (plan != PremiumPlan.options.first)
                                      const SizedBox(width: 8),
                                    Expanded(
                                      child: _PlanCard(
                                        plan: plan,
                                        selected: plan == _selected,
                                        onTap: () =>
                                            setState(() => _selected = plan),
                                      ),
                                    ),
                                  ],
                                ],
                              ),
                              const SizedBox(height: 22),
                              DecoratedBox(
                                decoration: BoxDecoration(
                                  borderRadius: BorderRadius.circular(32),
                                  gradient: LinearGradient(
                                    begin: Alignment.topCenter,
                                    end: Alignment.bottomCenter,
                                    colors: [scheme.secondary, scheme.primary],
                                  ),
                                ),
                                child: TextButton(
                                  style: TextButton.styleFrom(
                                    foregroundColor: scheme.onPrimary,
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 18,
                                      vertical: 17,
                                    ),
                                    minimumSize: const Size(0, 52),
                                  ),
                                  onPressed: () => _trial(themedContext),
                                  child: const Text(
                                    'DÙNG THỬ MIỄN PHÍ 7 NGÀY',
                                    textAlign: TextAlign.center,
                                    style: TextStyle(
                                      fontWeight: FontWeight.w800,
                                      fontSize: 16,
                                    ),
                                  ),
                                ),
                              ),
                              TextButton(
                                onPressed: _close,
                                child: Text(
                                  'Tiếp tục với bản Free',
                                  style: TextStyle(
                                    color: scheme.secondary,
                                    decoration: TextDecoration.underline,
                                  ),
                                ),
                              ),
                              Text(
                                'Giá và chương trình dùng thử đang được đề xuất. Thanh toán và tự động gia hạn chưa được kích hoạt.',
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  color: scheme.onSurfaceVariant,
                                  fontSize: 12,
                                  height: 1.4,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _PremiumBenefits extends StatelessWidget {
  const _PremiumBenefits();
  static const items = [
    (
      Icons.layers_outlined,
      'Giáo trình Core cá nhân hóa',
      'Dựa trên đánh giá thể lực và khả năng của bạn.',
    ),
    (
      Icons.restaurant_menu_rounded,
      'Hướng dẫn dinh dưỡng',
      'Định hướng năng lượng và dinh dưỡng phù hợp mục tiêu.',
    ),
    (
      Icons.event_available_outlined,
      'Lịch tập & phục hồi',
      'Theo dõi kế hoạch, ngày tập và ngày nghỉ.',
    ),
    (
      Icons.sell_outlined,
      'Trải nghiệm không quảng cáo',
      'Tập trung hoàn toàn vào việc tập luyện.',
    ),
  ];
  @override
  Widget build(BuildContext context) => Column(
    children: [
      for (final item in items)
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 7),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                item.$1,
                color: Theme.of(context).colorScheme.secondary,
                size: 28,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.$2,
                      style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 16,
                      ),
                    ),
                    Text(
                      item.$3,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
    ],
  );
}

class _PlanCard extends StatelessWidget {
  const _PlanCard({
    required this.plan,
    required this.selected,
    required this.onTap,
  });
  final PremiumPlan plan;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      selected: selected,
      button: true,
      label:
          '${plan.months} tháng, ${plan.price}${plan.badge == null ? '' : ', ${plan.badge}'}',
      child: AnimatedContainer(
        duration: MediaQuery.disableAnimationsOf(context)
            ? Duration.zero
            : const Duration(milliseconds: 180),
        decoration: BoxDecoration(
          color: selected
              ? scheme.primary.withValues(alpha: .16)
              : scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(
            color: selected
                ? scheme.secondary
                : scheme.onSurface.withValues(alpha: .18),
            width: 2,
          ),
          boxShadow: selected
              ? [
                  BoxShadow(
                    color: scheme.primary.withValues(alpha: .25),
                    blurRadius: 16,
                  ),
                ]
              : [],
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            borderRadius: BorderRadius.circular(16),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 12),
              child: Column(
                children: [
                  if (plan.badge != null)
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 4,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: selected
                            ? scheme.secondary
                            : scheme.onSurfaceVariant,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Text(
                        plan.badge!,
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: scheme.surface,
                          fontSize: 10,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    )
                  else
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 4),
                      child: Text('Linh hoạt', style: TextStyle(fontSize: 10)),
                    ),
                  const SizedBox(height: 14),
                  Text(
                    '${plan.months}\nTHÁNG',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 19,
                      fontWeight: FontWeight.w800,
                      color: selected ? scheme.secondary : scheme.onSurface,
                    ),
                  ),
                  const SizedBox(height: 12),
                  FittedBox(
                    fit: BoxFit.scaleDown,
                    child: Text(
                      plan.price,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  const SizedBox(height: 6),
                  Icon(
                    selected ? Icons.check_circle : Icons.circle_outlined,
                    size: 18,
                    color: selected
                        ? scheme.secondary
                        : scheme.onSurfaceVariant,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
