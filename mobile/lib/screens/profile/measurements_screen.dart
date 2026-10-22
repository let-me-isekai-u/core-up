part of '../core_up_screens.dart';

class MeasurementsScreen extends StatefulWidget {
  const MeasurementsScreen({super.key});
  @override
  State<MeasurementsScreen> createState() => _MeasurementsScreenState();
}

class _MeasurementsScreenState extends State<MeasurementsScreen> {
  final _form = GlobalKey<FormState>();
  final _height = TextEditingController(), _weight = TextEditingController(), _target = TextEditingController();
  bool _consent = false, _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final profile = context.read<ProfileProvider>().data;
    _height.text = profile?['heightCm']?.toString() ?? '';
    _weight.text = profile?['weightKg']?.toString() ?? '';
    _target.text = profile?['targetWeightKg']?.toString() ?? '';
  }

  @override
  void dispose() {
    _height.dispose(); _weight.dispose(); _target.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_saving || !_consent || !_form.currentState!.validate()) return;
    setState(() { _saving = true; _error = null; });
    final provider = context.read<ProfileProvider>();
    final saved = await provider.saveMeasurements({
      'heightCm': _optionalNumber(_height.text),
      'weightKg': _optionalNumber(_weight.text),
      'targetWeightKg': _optionalNumber(_target.text),
    });
    if (!mounted) return;
    if (saved) {
      final id = provider.data?['id']?.toString();
      if (id != null) await context.read<LocalStorageService>().deferMeasurementReview(id);
      if (!mounted) return;
      await context.read<WorkoutProvider>().loadProgress();
      if (mounted) Navigator.of(context).pop(true);
    } else {
      setState(() { _saving = false; _error = provider.error; });
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('SỐ ĐO TÙY CHỌN')),
    body: _saving ? const CoreUpChibiLoading(message: 'Đang lưu số đo...')
      : Form(key: _form, child: ListView(padding: const EdgeInsets.all(24), children: [
          const Text('Bạn có thể cập nhật mỗi tháng hoặc để sau. Số đo chỉ giúp theo dõi, không tự đổi mức tập hay giáo trình. Để trống nếu chưa cung cấp; lịch sử cân nặng đã ghi vẫn được giữ.'),
          const SizedBox(height: 16),
          TextFormField(controller: _height, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Chiều cao (cm)'), validator: (value) => _measurementError(value, 100, 250)),
          const SizedBox(height: 12),
          TextFormField(controller: _weight, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Cân nặng (kg)'), validator: (value) => _measurementError(value, 30, 300)),
          const SizedBox(height: 12),
          TextFormField(controller: _target, keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Cân nặng mục tiêu (kg, tùy chọn)'), validator: (value) => _measurementError(value, 30, 300)),
          CheckboxListTile(contentPadding: EdgeInsets.zero, title: const Text('Tôi đồng ý lưu các số đo trên'),
            value: _consent, onChanged: (value) => setState(() => _consent = value ?? false)),
          if (_error != null) Text(_error!, style: const TextStyle(color: Colors.redAccent)),
          FilledButton(onPressed: _consent ? _save : null, child: const Text('LƯU SỐ ĐO')),
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('ĐỂ SAU')),
        ])),
  );
}
