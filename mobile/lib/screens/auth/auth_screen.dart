part of '../core_up_screens.dart';

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, this.initialShowForm = false});
  final bool initialShowForm;
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();
  final _form = GlobalKey<FormState>();
  var _register = false;
  var _submitting = false;
  late var _showForm = widget.initialShowForm;

  @override
  void dispose() {
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
                        email: _email,
                        password: _password,
                        confirmPassword: _confirmPassword,
                        register: _register,
                        error: auth.error,
                        onSubmit: () async {
                          if (!_form.currentState!.validate()) return;
                          setState(() => _submitting = true);
                          final response = await withMinimumLoading(
                            () => auth.auth(
                              _register ? 'register' : 'login',
                              _email.text.trim(),
                              _password.text,
                            ),
                          );
                          if (!mounted) return;
                          setState(() => _submitting = false);
                          if (response == null) return;
                          Navigator.of(context).pushReplacement(
                            fadeRoute(
                              response.needsProfile
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
        'Đăng nhập để nhận kế hoạch cá nhân hóa, lưu lịch sử và theo dõi tiến độ.',
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
    required this.email,
    required this.password,
    required this.confirmPassword,
    required this.register,
    required this.error,
    required this.onSubmit,
    required this.onSwitchMode,
    required this.onBack,
  });
  final GlobalKey<FormState> formKey;
  final TextEditingController email;
  final TextEditingController password;
  final TextEditingController confirmPassword;
  final bool register;
  final String? error;
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
              controller: email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'Email'),
              validator: (value) => value == null || !value.contains('@')
                  ? 'Nhập email hợp lệ'
                  : null,
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: password,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Mật khẩu'),
              validator: (value) => value == null || value.length < 8
                  ? 'Dùng ít nhất 8 ký tự'
                  : null,
            ),
            if (register) ...[
              const SizedBox(height: 14),
              TextFormField(
                controller: confirmPassword,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Nhập lại mật khẩu',
                ),
                validator: (value) => value != password.text
                    ? 'Mật khẩu nhập lại chưa khớp'
                    : null,
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
