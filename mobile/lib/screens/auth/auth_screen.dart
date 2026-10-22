part of '../core_up_screens.dart';

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, this.initialShowForm = false});
  final bool initialShowForm;
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _username = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();
  final _form = GlobalKey<FormState>();
  var _register = false;
  var _submitting = false;
  late var _showForm = widget.initialShowForm;

  @override
  void dispose() {
    _username.dispose();
    _email.dispose();
    _password.dispose();
    _confirmPassword.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    if (_submitting) {
      return Scaffold(
        body: SafeArea(
          child: CoreUpChibiLoading(
            message: _register ? 'Đang tạo tài khoản...' : 'Đang đăng nhập...',
          ),
        ),
      );
    }
    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(
                height: 230,
                child: Image.asset(
                  'assets/coreup_main_hero.png',
                  fit: BoxFit.contain,
                ),
              ),
              Text(
                _showForm
                    ? (_register ? 'TẠO TÀI KHOẢN' : 'CHÀO MỪNG TRỞ LẠI')
                    : 'BẮT ĐẦU VỚI COREUP',
                style: const TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Tập luyện thông minh. Khỏe mạnh hơn mỗi ngày.',
                style: TextStyle(color: muted),
              ),
              const SizedBox(height: 28),
              AnimatedSwitcher(
                duration: const Duration(milliseconds: 480),
                reverseDuration: const Duration(milliseconds: 360),
                switchInCurve: Curves.easeOutQuart,
                switchOutCurve: Curves.easeInOutCubic,
                transitionBuilder: (child, animation) => FadeTransition(
                  opacity: animation,
                  child: SlideTransition(
                    position: Tween<Offset>(
                      begin: const Offset(0, .09),
                      end: Offset.zero,
                    ).animate(animation),
                    child: child,
                  ),
                ),
                layoutBuilder: (currentChild, _) =>
                    currentChild ?? const SizedBox.shrink(),
                child: _showForm
                    ? _AuthForm(
                        key: ValueKey(_register),
                        formKey: _form,
                        username: _username,
                        email: _email,
                        password: _password,
                        confirmPassword: _confirmPassword,
                        register: _register,
                        error: auth.error,
                        fieldErrors: auth.fieldErrors,
                        onSubmit: () async {
                          if (_submitting || !_form.currentState!.validate())
                            return;
                          setState(() => _submitting = true);
                          final response = await withMinimumLoading(
                            () => auth.auth(
                              _register ? 'register' : 'login',
                              _username.text.trim(),
                              _password.text,
                              email: _register ? _email.text.trim() : null,
                            ),
                          );
                          if (!mounted) return;
                          setState(() => _submitting = false);
                          if (response == null) return;
                          Navigator.of(context).pushReplacement(
                            fadeRoute(
                              response.needsEmailVerification
                                  ? const EmailVerificationScreen()
                                  : response.needsProfile
                                  ? const ProfileSetupScreen()
                                  : const ShellScreen(),
                            ),
                          );
                        },
                        onSwitchMode: () =>
                            setState(() => _register = !_register),
                        onBack: () => setState(() => _showForm = false),
                      )
                    : _AuthChoices(
                        key: const ValueKey('choices'),
                        onLogin: () => setState(() => _showForm = true),
                        onRegister: () => setState(() {
                          _register = true;
                          _showForm = true;
                        }),
                        onGuest: () async {
                          await context.read<AuthProvider>().continueAsGuest();
                          if (!context.mounted) return;
                          Navigator.of(context).pushAndRemoveUntil(
                            fadeRoute(const ShellScreen()),
                            (_) => false,
                          );
                        },
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AuthChoices extends StatelessWidget {
  const _AuthChoices({
    super.key,
    required this.onLogin,
    required this.onRegister,
    required this.onGuest,
  });
  final VoidCallback onLogin;
  final VoidCallback onRegister;
  final VoidCallback onGuest;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      PrimaryButton(label: 'TIẾP TỤC KHÔNG ĐĂNG NHẬP', onTap: onGuest),
      const SizedBox(height: 16),
      const Text(
        'Đăng nhập để tập theo cấp độ, lưu lịch sử và theo dõi tiến độ.',
        textAlign: TextAlign.center,
        style: TextStyle(color: muted, height: 1.4),
      ),
      const SizedBox(height: 16),
      OutlinedButton.icon(
        onPressed: onLogin,
        icon: const Icon(Icons.login_rounded),
        label: const Text('ĐĂNG NHẬP'),
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(52),
          foregroundColor: Colors.white,
          side: const BorderSide(color: AppColors.accent),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      const SizedBox(height: 10),
      Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Text('Chưa có tài khoản?', style: TextStyle(color: muted)),
          TextButton(onPressed: onRegister, child: const Text('Đăng ký')),
        ],
      ),
    ],
  );
}

class _AuthForm extends StatelessWidget {
  const _AuthForm({
    super.key,
    required this.formKey,
    required this.username,
    required this.email,
    required this.password,
    required this.confirmPassword,
    required this.register,
    required this.error,
    required this.fieldErrors,
    required this.onSubmit,
    required this.onSwitchMode,
    required this.onBack,
  });
  final GlobalKey<FormState> formKey;
  final TextEditingController username;
  final TextEditingController email;
  final TextEditingController password;
  final TextEditingController confirmPassword;
  final bool register;
  final String? error;
  final Map<String, String> fieldErrors;
  final VoidCallback onSubmit;
  final VoidCallback onSwitchMode;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) => Column(
    key: ValueKey(register),
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Form(
        key: formKey,
        child: Column(
          children: [
            TextFormField(
              controller: username,
              autocorrect: false,
              enableSuggestions: false,
              autofillHints: const [AutofillHints.username],
              decoration: InputDecoration(
                labelText: 'Tên tài khoản',
                errorText: fieldErrors['username'],
              ),
              validator: (value) {
                final name = value?.trim() ?? '';
                if (name.isEmpty) return 'Nhập tên tài khoản';
                if (!RegExp(r'^[A-Za-z0-9_]{3,24}$').hasMatch(name)) {
                  return 'Dùng 3–24 chữ Latin, số hoặc dấu gạch dưới';
                }
                return null;
              },
            ),
            if (register) ...[
              const SizedBox(height: 14),
              TextFormField(
                controller: email,
                keyboardType: TextInputType.emailAddress,
                autofillHints: const [AutofillHints.email],
                decoration: InputDecoration(
                  labelText: 'Email',
                  errorText: fieldErrors['email'],
                ),
                validator: (value) {
                  final address = value?.trim() ?? '';
                  if (address.isEmpty) return 'Nhập email';
                  if (address.length > 320 ||
                      !RegExp(
                        r'^[^\s@]+@[^\s@]+\.[^\s@]+$',
                      ).hasMatch(address)) {
                    return 'Nhập email hợp lệ';
                  }
                  return null;
                },
              ),
            ],
            const SizedBox(height: 14),
            _PasswordField(
              controller: password,
              label: 'Mật khẩu',
              error: fieldErrors['password'],
              validator: (value) {
                if (value == null || value.isEmpty) return 'Nhập mật khẩu';
                if (register && value.length < 8) return 'Dùng ít nhất 8 ký tự';
                if (value.length > 128) return 'Dùng tối đa 128 ký tự';
                return null;
              },
            ),
            if (register) ...[
              const SizedBox(height: 14),
              _PasswordField(
                controller: confirmPassword,
                label: 'Nhập lại mật khẩu',
                validator: (value) {
                  if (value == null || value.isEmpty)
                    return 'Nhập lại mật khẩu';
                  return value != password.text
                      ? 'Mật khẩu nhập lại chưa khớp'
                      : null;
                },
              ),
            ],
          ],
        ),
      ),
      if (error != null)
        Padding(
          padding: const EdgeInsets.only(top: 12),
          child: Text(error!, style: const TextStyle(color: Colors.redAccent)),
        ),
      const SizedBox(height: 20),
      PrimaryButton(
        label: register ? 'TẠO TÀI KHOẢN' : 'TIẾP TỤC',
        onTap: onSubmit,
      ),
      if (!register)
        TextButton(
          onPressed: () => Navigator.of(
            context,
          ).push(fadeRoute(const PasswordRecoveryScreen())),
          child: const Text('Quên mật khẩu?'),
        ),
      TextButton(
        onPressed: onSwitchMode,
        child: Text(
          register
              ? 'Đã có tài khoản? Đăng nhập'
              : 'Chưa có tài khoản? Tạo tài khoản',
        ),
      ),
      TextButton(onPressed: onBack, child: const Text('Quay lại')),
    ],
  );
}

class _PasswordField extends StatefulWidget {
  const _PasswordField({
    required this.controller,
    required this.label,
    required this.validator,
    this.error,
  });
  final TextEditingController controller;
  final String label;
  final String? error;
  final String? Function(String?) validator;
  @override
  State<_PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<_PasswordField> {
  bool _hidden = true;
  @override
  Widget build(BuildContext context) => TextFormField(
    controller: widget.controller,
    obscureText: _hidden,
    autocorrect: false,
    enableSuggestions: false,
    validator: widget.validator,
    decoration: InputDecoration(
      labelText: widget.label,
      errorText: widget.error,
      suffixIcon: IconButton(
        tooltip: _hidden ? 'Hiện mật khẩu' : 'Ẩn mật khẩu',
        onPressed: () => setState(() => _hidden = !_hidden),
        icon: Icon(
          _hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined,
        ),
      ),
    ),
  );
}
