part of '../core_up_screens.dart';

const _fitnessQuestions = <String>[
  'Bạn đã từng tập các bài cơ bụng/Core chưa?',
  'Bạn hiện vận động với tần suất nào?',
  'Bạn đã quen với Plank, Crunch, Dead Bug chưa?',
  'Bạn duy trì Plank với đúng tư thế được bao lâu?',
  'Bạn thường cảm thấy thế nào khi tập Core?',
  'Mục tiêu hiện tại của bạn?',
  'Có đau, chấn thương hoặc hạn chế vận động cần lưu ý không?',
];
const _fitnessOptions = <List<String>>[
  ['Chưa từng', 'Thỉnh thoảng', 'Thường xuyên'],
  ['Ít vận động', '1–2 buổi/tuần', '3 buổi/tuần trở lên'],
  ['Chưa quen', 'Biết cơ bản', 'Thực hiện khá thành thạo'],
  ['Chưa thực hiện được', 'Khoảng 10–20 giây', 'Trên 20 giây'],
  ['Nhanh mệt, cần nghỉ nhiều', 'Có thể tập mức vừa', 'Có thể kiểm soát bài khó'],
  ['Làm quen và tăng sức mạnh cơ bụng', 'Cải thiện sức bền và ổn định thân', 'Nâng cao kiểm soát và sức mạnh Core'],
  ['Không', 'Có', 'Không chắc chắn'],
];
const _fitnessLevels = ['Beginner', 'Intermediate', 'Advanced'];
String _levelLabel(String level) => switch (level) {
  'Beginner' => 'Dễ — Beginner',
  'Intermediate' => 'Vừa — Intermediate',
  'Advanced' => 'Khó — Advanced',
  _ => level,
};

/// Also gates returning Free accounts whose older profile has no questionnaire.
class ProfileSetupScreen extends StatefulWidget {
  const ProfileSetupScreen({super.key, this.review = false});
  final bool review;
  @override
  State<ProfileSetupScreen> createState() => _ProfileSetupScreenState();
}

class _ProfileSetupScreenState extends State<ProfileSetupScreen> {
  final _name = TextEditingController();
  final _height = TextEditingController();
  final _weight = TextEditingController();
  final _form = GlobalKey<FormState>();
  final List<int?> _answers = List.filled(7, null);
  bool _loading = true, _saving = false, _share = false, _confirming = false;
  String? _loadError, _message;
  String _selected = 'Beginner';
  Map<String, dynamic>? _recommendation;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  Future<void> _load() async {
    setState(() { _loading = true; _loadError = null; });
    final provider = context.read<ProfileProvider>();
    final loaded = await provider.load();
    if (!mounted) return;
    if (!loaded && !provider.missing) {
      setState(() { _loading = false; _loadError = provider.error ?? 'Chưa tải được hồ sơ.'; });
      return;
    }
    final data = provider.data;
    if (data?['assessment'] != null && !widget.review) {
      _home();
      return;
    }
    _name.text = data?['fullName']?.toString() ?? '';
    _height.text = data?['heightCm']?.toString() ?? '';
    _weight.text = data?['weightKg']?.toString() ?? '';
    final assessment = data?['assessment'] as Map?;
    final saved = assessment?['answers'] as List?;
    if (saved != null && saved.length == 7) {
      for (var i = 0; i < 7; i++) { _answers[i] = (saved[i] as num).toInt(); }
      _selected = data?['fitnessLevel']?.toString() ?? 'Beginner';
    }
    setState(() => _loading = false);
  }

  void _home() => Navigator.of(context).pushAndRemoveUntil(fadeRoute(const ShellScreen()), (_) => false);

  @override
  void dispose() {
    _name.dispose(); _height.dispose(); _weight.dispose();
    super.dispose();
  }

  Future<void> _recommend() async {
    if (_saving) return;
    if (_answers.any((answer) => answer == null)) {
      setState(() => _message = 'Vui lòng trả lời đủ 7 câu hỏi.');
      return;
    }
    setState(() { _saving = true; _message = null; });
    final provider = context.read<ProfileProvider>();
    final result = await provider.recommend(_answers.cast<int>());
    if (!mounted) return;
    setState(() {
      _saving = false;
      _recommendation = result;
      _selected = result?['recommendedLevel']?.toString() ?? 'Beginner';
      _message = result == null ? provider.error : null;
    });
  }

  Future<void> _save() async {
    if (_saving || _confirming || _recommendation == null || !_form.currentState!.validate()) return;
    var confirmedHigher = false;
    if (_fitnessLevels.indexOf(_selected) > _fitnessLevels.indexOf(_recommendation!['recommendedLevel'].toString())) {
      _confirming = true;
      confirmedHigher = await showDialog<bool>(context: context, builder: (dialogContext) => AlertDialog(
        title: const Text('Chọn mức cao hơn gợi ý?'),
        content: const Text('Mức này có thể vượt khả năng hiện tại. Điểm questionnaire không chứng minh bạn tập an toàn. Nếu đau, có chấn thương hoặc chưa chắc tư thế, nên hỏi chuyên viên trước. Chỉ tiếp tục nếu bạn hiểu giới hạn, có thể kiểm soát tư thế và sẵn sàng dừng khi cần.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: const Text('CHỌN LẠI')),
          TextButton(onPressed: () => Navigator.pop(dialogContext, true), child: const Text('TÔI HIỂU VÀ XÁC NHẬN')),
        ],
      )) ?? false;
      _confirming = false;
      if (!confirmedHigher || !mounted) return;
    }
    setState(() { _saving = true; _message = null; });
    final profile = context.read<ProfileProvider>();
    final ok = await profile.saveAssessment({
      'answers': _answers.cast<int>(),
      'userSelectedLevel': _selected,
      'confirmHigherLevel': confirmedHigher,
      'fullName': _name.text.trim(),
      'shareMeasurements': _share,
      'heightCm': _share ? _optionalNumber(_height.text) : null,
      'weightKg': _share ? _optionalNumber(_weight.text) : null,
    });
    if (!mounted) return;
    if (ok) {
      final profileId = profile.data?['id']?.toString();
      if (profileId != null) await context.read<LocalStorageService>().deferMeasurementReview(profileId);
      if (!mounted) return;
      await context.read<WorkoutProvider>().home();
      if (mounted) _home();
    } else {
      setState(() { _saving = false; _message = profile.error; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('KHẢ NĂNG TẬP LUYỆN · FREE')),
      body: _loading || _saving
        ? const CoreUpChibiLoading(message: 'Đang chuẩn bị giáo trình Free...')
        : _loadError != null ? Center(child: Column(mainAxisSize: MainAxisSize.min, children: [
            Text(_loadError!), TextButton(onPressed: _load, child: const Text('THỬ LẠI')),
          ]))
        : Form(key: _form, child: ListView(padding: const EdgeInsets.all(24), children: [
          const Text('Giáo trình Core chuẩn theo mức', style: TextStyle(fontSize: 25, fontWeight: FontWeight.w800)),
          const SizedBox(height: 12),
          const Text('7 câu hỏi giúp gợi ý Dễ/Vừa/Khó. Bạn chọn và xác nhận mức tập. Đây là phân loại của ứng dụng, không phải đánh giá y khoa. Câu trả lời được lưu trong hồ sơ để gợi ý mức tập. Số đo không quyết định mức tập.'),
          const SizedBox(height: 16),
          if (_recommendation == null) ...[
            for (var i = 0; i < _fitnessQuestions.length; i++) SurfaceCard(child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${i + 1}. ${_fitnessQuestions[i]}', style: const TextStyle(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                for (var j = 0; j < _fitnessOptions[i].length; j++) Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: ChoiceChip(
                    label: Text(_fitnessOptions[i][j], softWrap: true),
                    selected: _answers[i] == j,
                    onSelected: (_) => setState(() => _answers[i] = j),
                  ),
                ),
              ],
            )),
            if (_answers[6] != null && _answers[6] != 0)
              const Text('Có đau/chấn thương hoặc chưa chắc khả năng? Hãy hỏi chuyên viên y tế trước khi tập. Bạn có thể để sau, không cần bắt đầu buổi tập.', style: TextStyle(color: Colors.orangeAccent)),
            FilledButton(onPressed: _recommend, child: const Text('XEM GỢI Ý MỨC TẬP')),
          ] else ...[
            Text('Gợi ý: ${_levelLabel(_recommendation!['recommendedLevel'].toString())}', style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
            Text(_recommendation!['explanation'].toString()),
            const SizedBox(height: 12),
            for (final note in _recommendation!['safetyNotes'] as List)
              Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(note.toString(), style: const TextStyle(color: Colors.orangeAccent))),
            SelectField('Mức bạn xác nhận', _selected, _fitnessLevels, (value) => setState(() => _selected = value)),
            Text(_levelLabel(_selected)),
            const Text('Đổi mức chỉ thay giáo trình cho buổi bắt đầu sau này; lịch sử và buổi đang tập được giữ nguyên.'),
            TextButton(onPressed: () => setState(() => _recommendation = null), child: const Text('SỬA CÂU TRẢ LỜI')),
            TextFormField(controller: _name, maxLength: 150, decoration: const InputDecoration(labelText: 'Tên hiển thị (tùy chọn; mặc định tên tài khoản)')),
            SwitchListTile(contentPadding: EdgeInsets.zero, title: const Text('Tôi đồng ý cung cấp số đo (tùy chọn)'),
              subtitle: const Text('Chỉ để theo dõi. Bỏ qua vẫn nhận đầy đủ giáo trình Free.'),
              value: _share, onChanged: (value) => setState(() => _share = value)),
            if (_share) ...[
              TextFormField(controller: _height, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: const InputDecoration(labelText: 'Chiều cao (cm, tùy chọn)'), validator: (value) => _measurementError(value, 100, 250)),
              const SizedBox(height: 12),
              TextFormField(controller: _weight, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: const InputDecoration(labelText: 'Cân nặng (kg, tùy chọn)'), validator: (value) => _measurementError(value, 30, 300)),
            ],
            const SizedBox(height: 16),
            FilledButton(onPressed: _save, child: const Text('XÁC NHẬN & NHẬN GIÁO TRÌNH')),
          ],
          if (_message != null) Text(_message!, style: const TextStyle(color: Colors.redAccent)),
          TextButton(onPressed: widget.review ? () => Navigator.of(context).pop() : _home, child: const Text('ĐỂ SAU')),
        ])),
    );
  }
}

double? _optionalNumber(String text) => text.trim().isEmpty ? null : double.tryParse(text.trim().replaceAll(',', '.'));
String? _measurementError(String? text, double min, double max) {
  if (text == null || text.trim().isEmpty) return null;
  final number = _optionalNumber(text);
  return number == null || !number.isFinite || number < min || number > max ? 'Nhập giá trị từ $min đến $max hoặc để trống.' : null;
}
