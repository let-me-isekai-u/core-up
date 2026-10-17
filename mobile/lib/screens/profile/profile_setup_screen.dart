part of '../core_up_screens.dart';

class ProfileSetupScreen extends StatefulWidget {
  const ProfileSetupScreen({super.key});
  @override
  State<ProfileSetupScreen> createState() => _ProfileSetupScreenState();
}

class _ProfileSetupScreenState extends State<ProfileSetupScreen> {
  final _name = TextEditingController();
  final _height = TextEditingController(text: '170');
  final _weight = TextEditingController(text: '70');
  String _level = 'Beginner';
  String _goal = 'BuildMuscle';
  var _days = 3;
  var _minutes = 30;
  var _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _height.dispose();
    _weight.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final profile = context.watch<ProfileProvider>();
    if (_saving) {
      return const Scaffold(
        body: SafeArea(child: CoreUpChibiLoading(message: 'Đang lưu hồ sơ...')),
      );
    }
    return Scaffold(
      appBar: AppBar(title: const Text('THIẾT LẬP HỒ SƠ')),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          const Text(
            'Thông tin khởi đầu',
            style: TextStyle(fontSize: 26, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 18),
          TextField(
            controller: _name,
            decoration: const InputDecoration(labelText: 'Họ và tên'),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _height,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    labelText: 'Chiều cao (cm)',
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextField(
                  controller: _weight,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Cân nặng (kg)'),
                ),
              ),
            ],
          ),
          SelectField('Trình độ tập luyện', _level, const [
            'Beginner',
            'Intermediate',
            'Advanced',
          ], (v) => setState(() => _level = v)),
          SelectField('Mục tiêu', _goal, const [
            'LoseWeight',
            'Maintain',
            'BuildMuscle',
          ], (v) => setState(() => _goal = v)),
          Text('Số ngày tập: ' + _days.toString() + ' / tuần'),
          Slider(
            value: _days.toDouble(),
            min: 1,
            max: 7,
            divisions: 6,
            onChanged: (v) => setState(() => _days = v.toInt()),
          ),
          Text('Thời lượng buổi tập: ' + _minutes.toString() + ' phút'),
          Slider(
            value: _minutes.toDouble(),
            min: 10,
            max: 90,
            divisions: 8,
            onChanged: (v) => setState(() => _minutes = v.toInt()),
          ),
          if (profile.error != null)
            Text(
              profile.error!,
              style: const TextStyle(color: Colors.redAccent),
            ),
          PrimaryButton(
            label: 'LƯU HỒ SƠ',
            onTap: () async {
              setState(() => _saving = true);
              final saved = await withMinimumLoading(
                () => profile.save({
                  'fullName': _name.text.trim(),
                  'heightCm': double.tryParse(_height.text) ?? 0,
                  'weightKg': double.tryParse(_weight.text) ?? 0,
                  'fitnessLevel': _level,
                  'goal': _goal,
                  'trainingDaysPerWeek': _days,
                  'preferredWorkoutDurationMinutes': _minutes,
                }),
              );
              if (!mounted) return;
              setState(() => _saving = false);
              if (saved) {
                Navigator.of(context).pushAndRemoveUntil(
                  fadeRoute(const ShellScreen()),
                  (_) => false,
                );
              }
            },
          ),
        ],
      ),
    );
  }
}
