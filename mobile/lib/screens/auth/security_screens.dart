part of '../core_up_screens.dart';

class EmailVerificationScreen extends StatefulWidget {
  const EmailVerificationScreen({super.key});
  @override
  State<EmailVerificationScreen> createState() =>
      _EmailVerificationScreenState();
}

class _EmailVerificationScreenState extends State<EmailVerificationScreen> {
  final _code = TextEditingController();
  final _form = GlobalKey<FormState>();
  bool _busy = false;
  String? _message;
  DateTime _expires = DateTime.now().add(const Duration(minutes: 5));
  DateTime _resend = DateTime.now().add(const Duration(seconds: 60));
  Timer? _timer;
  @override
  void initState() {
    super.initState();
    _loadStatus();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  Future<void> _loadStatus() async {
    try {
      final response = await context.read<AuthProvider>().dio.get(
        '/api/auth/email-verification/status',
      );
      if (!mounted) return;
      setState(() {
        _expires =
            DateTime.tryParse(response.data['expiresAt']?.toString() ?? '') ??
            DateTime.now();
        _resend =
            DateTime.tryParse(response.data['resendAt']?.toString() ?? '') ??
            DateTime.now();
      });
    } catch (_) {
      // Countdown is informational; server validation/cooldown remains authoritative.
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    _code.dispose();
    super.dispose();
  }

  int remaining(DateTime time) =>
      time.difference(DateTime.now()).inSeconds.clamp(0, 300);
  Future<void> _send() async {
    if (_busy) return;
    setState(() => _busy = true);
    final ok = await context.read<AuthProvider>().securityAction(
      'email-verification/send',
      {},
    );
    if (!mounted) return;
    setState(() {
      _busy = false;
      _message = ok
          ? 'Đã yêu cầu mã mới. Kiểm tra email.'
          : context.read<AuthProvider>().error;
      if (ok) {
        _expires = DateTime.now().add(const Duration(minutes: 5));
        _resend = DateTime.now().add(const Duration(seconds: 60));
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('XÁC MINH EMAIL')),
      body: _busy
          ? const CoreUpChibiLoading(message: 'Đang xử lý...')
          : ListView(
              padding: const EdgeInsets.all(24),
              children: [
                Text(
                  'Nhập mã 6 chữ số gửi tới ${auth.email ?? 'email của bạn'}. Nếu chưa nhận được, hãy yêu cầu gửi lại.',
                ),
                Text('Mã hết hạn sau ${remaining(_expires)} giây'),
                Form(
                  key: _form,
                  child: TextFormField(
                    controller: _code,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Mã OTP'),
                    validator: otpValidator,
                  ),
                ),
                if (_message != null)
                  Text(_message!, style: const TextStyle(color: muted)),
                PrimaryButton(
                  label: 'XÁC MINH',
                  onTap: () async {
                    if (_busy || !_form.currentState!.validate()) return;
                    setState(() => _busy = true);
                    final ok = await auth.securityAction(
                      'email-verification/verify',
                      {'code': _code.text.trim()},
                    );
                    if (!mounted) return;
                    if (!ok) {
                      setState(() {
                        _busy = false;
                        _message = auth.error;
                      });
                      return;
                    }
                    auth.needsEmailVerification = false;
                    if (!context.mounted) return;
                    Navigator.of(context).pushAndRemoveUntil(
                      fadeRoute(
                        auth.needsProfile
                            ? const ProfileSetupScreen()
                            : const ShellScreen(),
                      ),
                      (_) => false,
                    );
                  },
                ),
                TextButton(
                  onPressed: remaining(_resend) == 0 ? _send : null,
                  child: Text(
                    remaining(_resend) == 0
                        ? 'Gửi lại mã'
                        : 'Gửi lại sau ${remaining(_resend)} giây',
                  ),
                ),
                if (!auth.verificationRequired)
                  TextButton(
                    onPressed: () => Navigator.of(context).pushAndRemoveUntil(
                      fadeRoute(
                        auth.needsProfile
                            ? const ProfileSetupScreen()
                            : const ShellScreen(),
                      ),
                      (_) => false,
                    ),
                    child: const Text('Để sau'),
                  ),
                // Legacy accounts keep access while verification is offered on return.
                TextButton(
                  onPressed: () => Navigator.of(context).pushAndRemoveUntil(
                    fadeRoute(const AuthScreen()),
                    (_) => false,
                  ),
                  child: const Text('Về màn hình chào mừng / Dùng Guest'),
                ),
              ],
            ),
    );
  }
}

String? otpValidator(String? code) =>
    RegExp(r'^[0-9]{6}$').hasMatch(code?.trim() ?? '')
    ? null
    : 'Nhập mã OTP gồm 6 chữ số';
String? newPasswordValidator(String? value) =>
    value == null || value.length < 8 || value.length > 128
    ? 'Mật khẩu cần từ 8 đến 128 ký tự'
    : null;

class PasswordRecoveryScreen extends StatefulWidget {
  const PasswordRecoveryScreen({super.key});
  @override
  State<PasswordRecoveryScreen> createState() => _PasswordRecoveryScreenState();
}

class _PasswordRecoveryScreenState extends State<PasswordRecoveryScreen> {
  final _email = TextEditingController();
  final _code = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  final _form = GlobalKey<FormState>();
  int _step = 0;
  bool _busy = false;
  String? _grant, _message;
  DateTime? _expires, _resend;
  Timer? _timer;
  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    for (final c in [_email, _code, _password, _confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  int remaining(DateTime? time) => time == null
      ? 0
      : time.difference(DateTime.now()).inSeconds.clamp(0, 300);
  Future<void> _send() async {
    final auth = context.read<AuthProvider>();
    setState(() => _busy = true);
    final ok = await auth.securityAction('forgot-password', {
      'email': _email.text.trim(),
    });
    if (!mounted) return;
    setState(() {
      _busy = false;
      _message = ok
          ? 'Nếu email đủ điều kiện, mã đặt lại đã được gửi.'
          : auth.error;
      if (ok) {
        _step = 1;
        _expires = DateTime.now().add(const Duration(minutes: 5));
        _resend = DateTime.now().add(const Duration(seconds: 60));
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(
        title: Text(_step == 2 ? 'ĐẶT LẠI MẬT KHẨU' : 'QUÊN MẬT KHẨU'),
      ),
      body: _busy
          ? const CoreUpChibiLoading(message: 'Đang xử lý...')
          : ListView(
              padding: const EdgeInsets.all(24),
              children: [
                Form(
                  key: _form,
                  child: Column(
                    children: [
                      if (_step == 0)
                        TextFormField(
                          controller: _email,
                          keyboardType: TextInputType.emailAddress,
                          decoration: const InputDecoration(labelText: 'Email'),
                          validator: (v) =>
                              RegExp(
                                r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
                              ).hasMatch(v?.trim() ?? '')
                              ? null
                              : 'Nhập email hợp lệ',
                        ),
                      if (_step == 1) ...[
                        Text('Mã hết hạn sau ${remaining(_expires)} giây'),
                        TextFormField(
                          controller: _code,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Mã OTP',
                          ),
                          validator: otpValidator,
                        ),
                      ],
                      if (_step == 2) ...[
                        _PasswordField(
                          controller: _password,
                          label: 'Mật khẩu mới',
                          validator: newPasswordValidator,
                        ),
                        _PasswordField(
                          controller: _confirm,
                          label: 'Nhập lại mật khẩu mới',
                          validator: (v) =>
                              v == null || v.isEmpty || v != _password.text
                              ? 'Mật khẩu nhập lại chưa khớp'
                              : null,
                        ),
                      ],
                    ],
                  ),
                ),
                if (_message != null)
                  Text(_message!, style: const TextStyle(color: muted)),
                PrimaryButton(
                  label: _step == 0
                      ? 'GỬI MÃ OTP'
                      : _step == 1
                      ? 'KIỂM TRA MÃ'
                      : 'ĐẶT LẠI MẬT KHẨU',
                  onTap: () async {
                    if (_busy || !_form.currentState!.validate()) return;
                    if (_step == 0) {
                      await _send();
                      return;
                    }
                    setState(() => _busy = true);
                    try {
                      if (_step == 1) {
                        final r = await auth.dio.post(
                          '/api/auth/reset-password/verify',
                          data: {
                            'email': _email.text.trim(),
                            'code': _code.text.trim(),
                          },
                        );
                        if (!mounted) return;
                        setState(() {
                          _grant = r.data['resetToken'] as String;
                          _step = 2;
                          _busy = false;
                          _message = null;
                        });
                      } else {
                        final ok = await auth.securityAction('reset-password', {
                          'email': _email.text.trim(),
                          'resetToken': _grant,
                          'newPassword': _password.text,
                          'confirmPassword': _confirm.text,
                        });
                        if (!mounted) return;
                        if (!ok) {
                          setState(() {
                            _busy = false;
                            _message = auth.error;
                          });
                          return;
                        }
                        await auth.logout();
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text(
                              'Đã đặt lại mật khẩu. Hãy đăng nhập lại.',
                            ),
                          ),
                        );
                        Navigator.of(context).pushAndRemoveUntil(
                          fadeRoute(const AuthScreen(initialShowForm: true)),
                          (_) => false,
                        );
                      }
                    } catch (e) {
                      if (mounted)
                        setState(() {
                          _busy = false;
                          _message = auth.securityMessage(e);
                        });
                    }
                  },
                ),
                if (_step == 1)
                  TextButton(
                    onPressed: remaining(_resend) == 0 ? _send : null,
                    child: Text(
                      remaining(_resend) == 0
                          ? 'Gửi lại mã'
                          : 'Gửi lại sau ${remaining(_resend)} giây',
                    ),
                  ),
                if (_step == 2)
                  TextButton(
                    onPressed: () => setState(() {
                      _step = 0;
                      _grant = null;
                    }),
                    child: const Text('Yêu cầu mã mới'),
                  ),
              ],
            ),
    );
  }
}

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});
  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _current = TextEditingController(),
      _password = TextEditingController(),
      _confirm = TextEditingController();
  final _form = GlobalKey<FormState>();
  bool _busy = false;
  String? _message;
  @override
  void dispose() {
    _current.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    return Scaffold(
      appBar: AppBar(title: const Text('ĐỔI MẬT KHẨU')),
      body: _busy
          ? const CoreUpChibiLoading(message: 'Đang đổi mật khẩu...')
          : ListView(
              padding: const EdgeInsets.all(24),
              children: [
                const Text(
                  'Đổi mật khẩu sẽ đăng xuất mọi thiết bị, gồm phiên hiện tại.',
                ),
                Form(
                  key: _form,
                  child: Column(
                    children: [
                      _PasswordField(
                        controller: _current,
                        label: 'Mật khẩu hiện tại',
                        validator: (v) => v == null || v.isEmpty
                            ? 'Nhập mật khẩu hiện tại'
                            : null,
                      ),
                      _PasswordField(
                        controller: _password,
                        label: 'Mật khẩu mới',
                        validator: (v) =>
                            newPasswordValidator(v) ??
                            (v == _current.text
                                ? 'Mật khẩu mới phải khác mật khẩu hiện tại'
                                : null),
                      ),
                      _PasswordField(
                        controller: _confirm,
                        label: 'Nhập lại mật khẩu mới',
                        validator: (v) =>
                            v == null || v.isEmpty || v != _password.text
                            ? 'Mật khẩu nhập lại chưa khớp'
                            : null,
                      ),
                    ],
                  ),
                ),
                if (_message != null)
                  Text(_message!, style: const TextStyle(color: muted)),
                PrimaryButton(
                  label: 'ĐỔI MẬT KHẨU',
                  onTap: () async {
                    if (_busy || !_form.currentState!.validate()) return;
                    setState(() => _busy = true);
                    final ok = await auth.securityAction('change-password', {
                      'currentPassword': _current.text,
                      'newPassword': _password.text,
                      'confirmPassword': _confirm.text,
                    });
                    if (!mounted) return;
                    if (!ok) {
                      setState(() {
                        _busy = false;
                        _message = auth.error;
                      });
                      return;
                    }
                    await auth.logout();
                    if (!context.mounted) return;
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Đã đổi mật khẩu. Hãy đăng nhập lại.'),
                      ),
                    );
                    Navigator.of(context).pushAndRemoveUntil(
                      fadeRoute(const AuthScreen(initialShowForm: true)),
                      (_) => false,
                    );
                  },
                ),
              ],
            ),
    );
  }
}
