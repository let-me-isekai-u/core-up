part of '../core_up_screens.dart';

Future<void> _linkGoogleAccount(BuildContext context) async {
  final linked = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (_) => const _GoogleLinkDialog(),
  );
  if (linked == true && context.mounted) {
    Navigator.of(
      context,
    ).pushAndRemoveUntil(fadeRoute(const AuthScreen()), (_) => false);
  }
}

class _GoogleLinkDialog extends StatefulWidget {
  const _GoogleLinkDialog();
  @override
  State<_GoogleLinkDialog> createState() => _GoogleLinkDialogState();
}

class _GoogleLinkDialogState extends State<_GoogleLinkDialog> {
  final _password = TextEditingController();
  bool _busy = false;
  String? _error;
  @override
  void dispose() {
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !_busy,
    child: AlertDialog(
      title: const Text('Liên kết Google'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text(
            'Xác nhận mật khẩu Core-Up và chọn Google cùng email. Sau khi liên kết, bạn cần đăng nhập lại. Không gộp tài khoản tự động.',
          ),
          TextField(
            controller: _password,
            obscureText: true,
            enabled: !_busy,
            decoration: const InputDecoration(
              labelText: 'Mật khẩu Core-Up hiện tại',
            ),
          ),
          if (_error != null)
            Text(
              _error!,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: _busy ? null : () => Navigator.pop(context),
          child: const Text('HỦY'),
        ),
        TextButton(
          onPressed: _busy
              ? null
              : () async {
                  if (_password.text.isEmpty) {
                    setState(() => _error = 'Nhập mật khẩu hiện tại.');
                    return;
                  }
                  setState(() {
                    _busy = true;
                    _error = null;
                  });
                  final auth = context.read<AuthProvider>();
                  final linked = await auth.linkGoogle(_password.text);
                  _password.clear();
                  if (!mounted) return;
                  if (linked) {
                    Navigator.pop(context, true);
                    return;
                  }
                  setState(() {
                    _busy = false;
                    _error = auth.error;
                  });
                },
          child: Text(_busy ? 'ĐANG LIÊN KẾT…' : 'XÁC NHẬN & CHỌN GOOGLE'),
        ),
      ],
    ),
  );
}
