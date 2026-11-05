part of '../../screens/core_up_screens.dart';

// Visual inspiration: Lucian #026/#036/#256, MIT © 2026 Seokwoooo.
// Flutter implementation; all verification stays on the Core-Up server.
class CoreUpOtpInput extends StatefulWidget {
  const CoreUpOtpInput({
    super.key,
    required this.controller,
    this.error = false,
    this.success = false,
  });
  final TextEditingController controller;
  final bool error, success;
  @override
  State<CoreUpOtpInput> createState() => _CoreUpOtpInputState();
}

class _CoreUpOtpInputState extends State<CoreUpOtpInput> {
  final _focus = FocusNode();
  @override
  void initState() {
    super.initState();
    _focus.addListener(_refresh);
    widget.controller.addListener(_refresh);
  }

  void _refresh() {
    if (mounted) setState(() {});
  }

  @override
  void didUpdateWidget(CoreUpOtpInput old) {
    super.didUpdateWidget(old);
    if (old.controller != widget.controller) {
      old.controller.removeListener(_refresh);
      widget.controller.addListener(_refresh);
    }
  }

  @override
  void dispose() {
    widget.controller.removeListener(_refresh);
    _focus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final value = widget.controller.text;
    final duration = MediaQuery.disableAnimationsOf(context)
        ? Duration.zero
        : const Duration(milliseconds: 160);
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: widget.error ? 1 : 0, end: 0),
      duration: MediaQuery.disableAnimationsOf(context)
          ? Duration.zero
          : const Duration(milliseconds: 360),
      builder: (context, t, child) => Transform.translate(
        offset: Offset(math.sin(t * math.pi * 4) * 6, 0),
        child: child,
      ),
      child: Column(
        children: [
          const Icon(Icons.verified_user_outlined),
          const SizedBox(height: 12),
          // One visible native editor owns selection, paste, deletion and iOS AutoFill.
          TextFormField(
            controller: widget.controller,
            focusNode: _focus,
            keyboardType: TextInputType.number,
            autofillHints: const [AutofillHints.oneTimeCode],
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp('[0-9]')),
              LengthLimitingTextInputFormatter(6),
            ],
            validator: otpValidator,
            textAlign: TextAlign.center,
            decoration: const InputDecoration(
              labelText: 'Mã xác minh gồm 6 chữ số',
              counterText: '',
            ),
          ),
          const SizedBox(height: 12),
          ExcludeSemantics(
            child: LayoutBuilder(
              builder: (context, constraints) => Row(
                children: [
                  for (int i = 0; i < 6; i++) ...[
                    if (i == 3)
                      const Padding(
                        padding: EdgeInsets.symmetric(horizontal: 4),
                        child: Text('–'),
                      ),
                    Expanded(
                      child: AnimatedContainer(
                        duration: duration,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(12),
                          color: colors.surfaceContainerHighest,
                          border: Border.all(
                            width: 2,
                            color: widget.error
                                ? colors.error
                                : widget.success
                                ? Colors.green
                                : _focus.hasFocus &&
                                      i == value.length.clamp(0, 5)
                                ? colors.primary
                                : colors.outlineVariant,
                          ),
                        ),
                        child: Center(
                          child: AnimatedSwitcher(
                            duration: duration,
                            child: Text(
                              i < value.length ? value[i] : '·',
                              key: ValueKey(
                                '$i${i < value.length ? value[i] : ''}',
                              ),
                              style: Theme.of(context).textTheme.titleLarge,
                            ),
                          ),
                        ),
                      ),
                    ),
                    if (i != 2 && i != 5) const SizedBox(width: 4),
                  ],
                ],
              ),
            ),
          ),
          if (widget.error)
            Semantics(
              liveRegion: true,
              child: Text(
                'Mã chưa được xác minh. Kiểm tra thông báo và thử lại.',
                style: TextStyle(color: colors.error),
              ),
            ),
          if (widget.success)
            Semantics(liveRegion: true, child: Text('Đã xác minh mã')),
        ],
      ),
    );
  }
}

// Conservative offline heuristic, not an entropy estimate or security guarantee.
int coreUpPasswordScore(String value) {
  final lower = value.toLowerCase();
  if (value.length < 8 ||
      RegExp(r'^(.)\1+$').hasMatch(value) ||
      [
        'password',
        'qwerty',
        '123456',
        'abcdef',
        'letmein',
        'matkhau',
      ].any(lower.contains))
    return 0;
  final unique = value.runes.toSet().length;
  if (unique < 5) return 0;
  if (value.length < 12) return 1;
  if (value.length < 16 || unique < 8) return 2;
  return 3;
}

class CoreUpPasswordStrength extends StatelessWidget {
  const CoreUpPasswordStrength({super.key, required this.controller});
  final TextEditingController controller;
  @override
  Widget build(
    BuildContext context,
  ) => ValueListenableBuilder<TextEditingValue>(
    valueListenable: controller,
    builder: (context, value, _) {
      if (value.text.isEmpty) return const SizedBox.shrink();
      final score = coreUpPasswordScore(value.text),
          colors = Theme.of(context).colorScheme;
      final labels = [
        'Rất dễ đoán',
        'Cần cải thiện',
        'Khá hơn',
        'Dài và ít mẫu phổ biến',
      ];
      final duration = MediaQuery.disableAnimationsOf(context)
          ? Duration.zero
          : const Duration(milliseconds: 180);
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                for (int i = 0; i < 4; i++)
                  Expanded(
                    child: AnimatedContainer(
                      duration: duration,
                      height: 5,
                      margin: const EdgeInsets.symmetric(horizontal: 2),
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(4),
                        color: i <= score
                            ? (score == 0 ? colors.error : colors.primary)
                            : colors.outlineVariant,
                      ),
                    ),
                  ),
              ],
            ),
            Text('Độ khó đoán: ${labels[score]}'),
            for (final item in <(bool, String)>[
              (
                value.text.length >= 8 && value.text.length <= 128,
                '8–128 ký tự (bắt buộc)',
              ),
              (value.text.length >= 16, 'Gợi ý: cụm từ dài từ 16 ký tự'),
              (score > 0, 'Tránh chuỗi lặp và mẫu phổ biến'),
            ])
              Row(
                children: [
                  Icon(
                    item.$1
                        ? Icons.check_circle_outline
                        : Icons.circle_outlined,
                    size: 16,
                  ),
                  const SizedBox(width: 6),
                  Expanded(child: Text(item.$2)),
                ],
              ),
            const Text(
              'Đánh giá cục bộ, không bảo đảm mật khẩu chưa bị lộ. Không cần bắt buộc đủ các loại ký tự.',
            ),
          ],
        ),
      );
    },
  );
}

class CoreUpProgressLoading extends StatelessWidget {
  const CoreUpProgressLoading({super.key, this.message, this.progress});
  final String? message;
  final double? progress;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CoreUpChibiLoading(message: message),
          const SizedBox(height: 16),
          SizedBox(
            width: 280,
            child: MediaQuery.disableAnimationsOf(context) && progress == null
                ? const Text('Đang xử lý…', textAlign: TextAlign.center)
                : LinearProgressIndicator(value: progress?.clamp(0, 1)),
          ),
          if (progress != null)
            Text('${(progress!.clamp(0, 1) * 100).floor()}%'),
        ],
      ),
    ),
  );
}

class PremiumDemoRequestScreen extends StatefulWidget {
  const PremiumDemoRequestScreen({super.key});
  @override
  State<PremiumDemoRequestScreen> createState() =>
      _PremiumDemoRequestScreenState();
}

class _PremiumDemoRequestScreenState extends State<PremiumDemoRequestScreen> {
  bool _busy = false;
  String? _message;
  List<dynamic> _rows = [];
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _load();
    });
  }

  Future<void> _load() async {
    try {
      final r = await context.read<AuthProvider>().dio.get(
        '/api/account/premium-requests',
      );
      if (mounted) setState(() => _rows = r.data as List<dynamic>);
    } catch (e) {
      if (mounted)
        setState(
          () => _message = context.read<AuthProvider>().securityMessage(e),
        );
    }
  }

  Future<void> _act(bool cancel) async {
    if (_busy) return;
    setState(() => _busy = true);
    final auth = context.read<AuthProvider>();
    try {
      if (cancel) {
        await auth.dio.delete('/api/account/premium-requests/pending');
      } else {
        await auth.dio.post('/api/account/premium-requests');
      }
      if (!mounted) return;
      setState(
        () => _message = cancel
            ? 'Đã hủy yêu cầu.'
            : 'Đã gửi. Chờ Admin xem xét; quyền chưa được cấp.',
      );
      await _load();
    } catch (e) {
      if (mounted) setState(() => _message = auth.securityMessage(e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Yêu cầu trải nghiệm Premium')),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        const Text(
          'Yêu cầu quyền demo miễn phí. Admin quyết định thời hạn. Không thanh toán và không tự cấp quyền.',
        ),
        if (_message != null)
          Semantics(liveRegion: true, child: Text(_message!)),
        if (_busy)
          const CoreUpProgressLoading(message: 'Đang xử lý yêu cầu...'),
        ElevatedButton(
          onPressed: _busy || _rows.any((r) => r['status'] == 'Pending')
              ? null
              : () => _act(false),
          child: const Text('Gửi yêu cầu demo'),
        ),
        if (_rows.any((r) => r['status'] == 'Pending'))
          TextButton(
            onPressed: _busy ? null : () => _act(true),
            child: const Text('Hủy yêu cầu đang chờ'),
          ),
        for (final row in _rows)
          ListTile(
            title: Text('${row['status']}'),
            subtitle: Text('${row['createdAt']} · ${row['reason'] ?? ''}'),
          ),
        TextButton(
          onPressed: _busy ? null : _load,
          child: const Text('Làm mới trạng thái'),
        ),
      ],
    ),
  );
}
