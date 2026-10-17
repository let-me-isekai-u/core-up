part of '../../screens/core_up_screens.dart';

class PrimaryButton extends StatelessWidget {
  const PrimaryButton({super.key, required this.label, required this.onTap});
  final String label;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => SizedBox(
    height: 54,
    width: double.infinity,
    child: ElevatedButton(
      onPressed: onTap,
      style: ElevatedButton.styleFrom(
        backgroundColor: blue,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      child: Text(label, style: const TextStyle(fontWeight: FontWeight.bold)),
    ),
  );
}

class SurfaceCard extends StatelessWidget {
  const SurfaceCard({super.key, required this.child});
  final Widget child;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 10),
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: surface,
      borderRadius: BorderRadius.circular(20),
      border: Border.all(color: Colors.white12),
    ),
    child: child,
  );
}

class SectionLabel extends StatelessWidget {
  const SectionLabel(this.label, {super.key});
  final String label;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 10),
    child: Text(
      label,
      style: const TextStyle(
        color: muted,
        fontSize: 12,
        fontWeight: FontWeight.w700,
        letterSpacing: 1.1,
      ),
    ),
  );
}

class EmptyState extends StatelessWidget {
  const EmptyState({
    super.key,
    required this.icon,
    required this.title,
    required this.message,
  });
  final IconData icon;
  final String title, message;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 24),
    child: Column(
      children: [
        Icon(icon, size: 50, color: blue),
        const SizedBox(height: 12),
        Text(
          title,
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 6),
        Text(
          message,
          textAlign: TextAlign.center,
          style: const TextStyle(color: muted, height: 1.4),
        ),
      ],
    ),
  );
}

class StatCard extends StatelessWidget {
  const StatCard(this.label, this.value, {super.key});
  final String label, value;
  @override
  Widget build(BuildContext context) => Expanded(
    child: SurfaceCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: const TextStyle(
              color: muted,
              fontSize: 10,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
          ),
        ],
      ),
    ),
  );
}

class InfoLine extends StatelessWidget {
  const InfoLine(this.label, this.value, {super.key});
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: const TextStyle(color: muted)),
        Text(value, style: const TextStyle(fontWeight: FontWeight.bold)),
      ],
    ),
  );
}

class SelectField extends StatelessWidget {
  const SelectField(
    this.title,
    this.value,
    this.values,
    this.changed, {
    super.key,
  });
  final String title, value;
  final List<String> values;
  final ValueChanged<String> changed;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 12),
    child: DropdownButtonFormField<String>(
      value: value,
      decoration: InputDecoration(labelText: title),
      items: values
          .map((x) => DropdownMenuItem(value: x, child: Text(localizeValue(x))))
          .toList(),
      onChanged: (x) {
        if (x != null) changed(x);
      },
    ),
  );
}

Route<T> fadeRoute<T>(Widget page) => PageRouteBuilder<T>(
  transitionDuration: const Duration(milliseconds: 320),
  reverseTransitionDuration: const Duration(milliseconds: 250),
  pageBuilder: (_, animation, __) => FadeTransition(
    opacity: animation,
    child: SlideTransition(
      position: Tween<Offset>(
        begin: const Offset(0, .025),
        end: Offset.zero,
      ).animate(CurvedAnimation(parent: animation, curve: Curves.easeOutCubic)),
      child: page,
    ),
  ),
);

void openLogin(BuildContext context) => Navigator.of(
  context,
).push(fadeRoute(const AuthScreen(initialShowForm: true)));
String formatDuration(int seconds) {
  final value = seconds < 0 ? 0 : seconds;
  final d = Duration(seconds: value);
  final h = d.inHours;
  final m = d.inMinutes.remainder(60);
  final s = d.inSeconds.remainder(60);
  return h > 0
      ? h.toString().padLeft(2, '0') +
            ':' +
            m.toString().padLeft(2, '0') +
            ':' +
            s.toString().padLeft(2, '0')
      : m.toString().padLeft(2, '0') + ':' + s.toString().padLeft(2, '0');
}

String formatVietnameseDate(DateTime date) =>
    '${date.day} thg ${date.month}, ${date.year}';

String formatDisplayName(String name) => name
    .trim()
    .split(RegExp(r'\s+'))
    .map(
      (part) => part.isEmpty
          ? part
          : '${part[0].toUpperCase()}${part.substring(1).toLowerCase()}',
    )
    .join(' ');

String localizeValue(String value) =>
    const {
      'Beginner': 'Mới bắt đầu',
      'Intermediate': 'Trung cấp',
      'Advanced': 'Nâng cao',
      'Beginner Full Body': 'Toàn thân cho người mới',
      'LoseWeight': 'Giảm cân',
      'Maintain': 'Duy trì',
      'BuildMuscle': 'Tăng cơ',
      'Legs': 'Chân',
      'Core': 'Cơ bụng',
      'Chest': 'Ngực',
      'Back': 'Lưng',
      'Shoulders': 'Vai',
      'Arms': 'Tay',
      'Full Body': 'Toàn thân',
      'Cardio': 'Tim mạch',
      'Mobility': 'Linh hoạt',
    }[value] ??
    value;

IconData exerciseIcon(String group) => switch (group) {
  'Core' => Icons.accessibility_new_rounded,
  'Legs' => Icons.directions_run_rounded,
  'Arms' || 'Chest' => Icons.sports_gymnastics_rounded,
  'Cardio' => Icons.favorite_rounded,
  _ => Icons.fitness_center_rounded,
};
