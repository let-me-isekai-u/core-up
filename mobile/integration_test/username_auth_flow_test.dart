import 'package:dio/dio.dart';
import 'package:core_up_mobile/core/storage/local_storage_service.dart';
import 'package:core_up_mobile/providers/auth_provider.dart';
import 'package:core_up_mobile/providers/profile_provider.dart';
import 'package:core_up_mobile/providers/workout_provider.dart';
import 'package:core_up_mobile/screens/core_up_screens.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

// This native UI test must target an API configured with a disposable test database.
// It uses a separate secure-storage key and in-memory mode preferences, so it cannot
// replace the developer's saved login or onboarding preferences.
class TestLocalStorage extends LocalStorageService {
  String? mode;
  @override
  Future<String?> sessionMode() async => mode;
  @override
  Future<void> saveSessionMode(String value) async {
    mode = value;
  }

  @override
  Future<void> clearSessionMode() async {
    mode = null;
  }
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  testWidgets(
    'native OTP verification, profile, change and reset password with Mailpit',
    (tester) async {
      const enabled = bool.fromEnvironment('AUTH_PHASE1_TEST_API');
      expect(
        enabled,
        isTrue,
        reason:
            'Only run against a disposable test API with AUTH_PHASE1_TEST_API=true.',
      );
      final storage = TestLocalStorage();
      final key = 'phase1_test_${DateTime.now().microsecondsSinceEpoch}';
      final auth = AuthProvider(storage, tokenStorageKey: key);
      final name = 'ui_${DateTime.now().millisecondsSinceEpoch}';
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<LocalStorageService>.value(value: storage),
            ChangeNotifierProvider.value(value: auth),
            ChangeNotifierProvider(create: (_) => ProfileProvider(auth)),
            ChangeNotifierProvider(create: (_) => WorkoutProvider(auth)),
          ],
          child: const MaterialApp(home: AuthScreen(initialShowForm: true)),
        ),
      );
      Future<void> waitFor(Finder finder) async {
        for (var i = 0; i < 150 && finder.evaluate().isEmpty; i++) {
          await tester.pump(const Duration(milliseconds: 100));
        }
        expect(finder, findsWidgets);
        await tester.pump(const Duration(milliseconds: 600));
      }

      try {
        await tester.ensureVisible(
          find.text('Chưa có tài khoản? Tạo tài khoản'),
        );
        await tester.tap(find.text('Chưa có tài khoản? Tạo tài khoản'));
        await tester.pump(const Duration(milliseconds: 600));
        var fields = find.byType(TextFormField);
        await tester.enterText(fields.at(0), name);
        await tester.enterText(fields.at(1), '$name@example.invalid');
        await tester.enterText(fields.at(2), 'NativeTest-123!');
        await tester.enterText(fields.at(3), 'NativeTest-123!');
        await tester.ensureVisible(find.text('TẠO TÀI KHOẢN').last);
        await tester.tap(find.text('TẠO TÀI KHOẢN').last);
        await waitFor(find.byType(EmailVerificationScreen));
        debugPrint('Auth native stage: verify-screen');
        final code = await mailpitCode('$name@example.invalid', 'xác minh');
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Mã OTP'),
          code,
        );
        await tester.ensureVisible(find.text('XÁC MINH'));
        await tester.tap(find.text('XÁC MINH'));
        await waitFor(find.byType(ProfileSetupScreen));
        debugPrint('Auth native stage: email-verified');
        expect(auth.isAuthenticated, isTrue);
        await tester.enterText(
          find.byType(TextField).first,
          'Native Auth Test',
        );
        await tester.ensureVisible(find.text('LƯU HỒ SƠ'));
        await tester.tap(find.text('LƯU HỒ SƠ'));
        await waitFor(find.byType(ShellScreen));
        final restored = AuthProvider(storage, tokenStorageKey: key);
        expect(await restored.restoreSession(), AuthStatus.authenticated);
        expect(restored.needsProfile, isFalse);
        expect(restored.token, isNotEmpty);
        expect((await restored.dio.get('/api/exercises')).statusCode, 200);
        // Use the app's real logout flow and then its username login form.
        await tester.tap(find.text('Cá nhân').last);
        await tester.pump(const Duration(milliseconds: 600));
        await tester.scrollUntilVisible(
          find.text('Đăng xuất'),
          200,
          scrollable: find.descendant(
            of: find.byType(ProfileScreen),
            matching: find.byType(Scrollable),
          ),
        );
        await tester.tap(find.text('Đăng xuất'));
        await waitFor(find.text('ĐĂNG NHẬP'));
        await tester.ensureVisible(find.text('ĐĂNG NHẬP'));
        await tester.tap(find.text('ĐĂNG NHẬP'));
        await tester.pump(const Duration(milliseconds: 600));
        fields = find.byType(TextFormField);
        expect(fields, findsNWidgets(2));
        await tester.enterText(fields.at(0), name.toUpperCase());
        await tester.enterText(fields.at(1), 'NativeTest-123!');
        await tester.ensureVisible(find.text('TIẾP TỤC'));
        await tester.tap(find.text('TIẾP TỤC'));
        await waitFor(find.byType(ShellScreen));
        expect(auth.isAuthenticated, isTrue);
        // Profile → Settings → Security → Change password, using real HTTP/JWT.
        await tester.tap(find.text('Cá nhân').last);
        await tester.pump(const Duration(milliseconds: 600));
        await tester.scrollUntilVisible(
          find.text('Bảo mật'),
          200,
          scrollable: find.descendant(
            of: find.byType(ProfileScreen),
            matching: find.byType(Scrollable),
          ),
        );
        await tester.tap(find.text('Bảo mật'));
        await waitFor(find.byType(ChangePasswordScreen));
        debugPrint('Auth native stage: change-screen');
        fields = find.byType(TextFormField);
        await tester.enterText(fields.at(0), 'NativeTest-123!');
        await tester.enterText(fields.at(1), 'NativeChanged-456!');
        await tester.enterText(fields.at(2), 'NativeChanged-456!');
        await tester.ensureVisible(
          find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'),
        );
        await tester.tap(find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'));
        await waitFor(find.widgetWithText(TextFormField, 'Tên tài khoản'));
        expect(auth.token, isNull);
        await tester.ensureVisible(find.text('Quên mật khẩu?'));
        await tester.tap(find.text('Quên mật khẩu?'));
        await waitFor(find.byType(PasswordRecoveryScreen));
        debugPrint('Auth native stage: recovery-screen');
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Email'),
          '$name@example.invalid',
        );
        await tester.tap(find.text('GỬI MÃ OTP'));
        await waitFor(find.widgetWithText(TextFormField, 'Mã OTP'));
        final resetCode = await mailpitCode('$name@example.invalid', 'đặt lại');
        await tester.enterText(
          find.widgetWithText(TextFormField, 'Mã OTP'),
          resetCode,
        );
        await tester.tap(find.text('KIỂM TRA MÃ'));
        await waitFor(find.widgetWithText(TextFormField, 'Mật khẩu mới'));
        fields = find.byType(TextFormField);
        await tester.enterText(fields.at(0), 'NativeReset-789!');
        await tester.enterText(fields.at(1), 'NativeReset-789!');
        await tester.tap(
          find.widgetWithText(ElevatedButton, 'ĐẶT LẠI MẬT KHẨU'),
        );
        await waitFor(find.widgetWithText(TextFormField, 'Tên tài khoản'));
        fields = find.byType(TextFormField);
        await tester.enterText(fields.at(0), name);
        await tester.enterText(fields.at(1), 'NativeReset-789!');
        await tester.ensureVisible(find.text('TIẾP TỤC'));
        await tester.tap(find.text('TIẾP TỤC'));
        await waitFor(find.byType(ShellScreen));
        final finalRestore = AuthProvider(storage, tokenStorageKey: key);
        expect(await finalRestore.restoreSession(), AuthStatus.authenticated);
        expect(finalRestore.needsEmailVerification, isFalse);
        debugPrint('Auth native stage: reset-and-restore');
        expect(tester.takeException(), isNull);
      } finally {
        await auth.logout();
        await tester.pumpWidget(const SizedBox.shrink());
        await tester.pump(const Duration(milliseconds: 600));
      }
    },
  );
}

Future<String> mailpitCode(String recipient, String subject) async {
  final client = Dio();
  try {
    for (var i = 0; i < 80; i++) {
      final response = await client.get(
        'http://127.0.0.1:8025/api/v1/search',
        queryParameters: {'query': 'to:$recipient'},
      );
      final messages = response.data['messages'] as List? ?? [];
      for (final dynamic message in messages) {
        if (!(message['Subject'] as String).contains(subject)) continue;
        final detail = await client.get(
          'http://127.0.0.1:8025/api/v1/message/${message['ID']}',
        );
        final match = RegExp(
          r'Mã OTP của bạn: ([0-9]{6})',
        ).firstMatch(detail.data['Text'] as String);
        if (match != null) return match.group(1)!;
      }
      await Future<void>.delayed(const Duration(milliseconds: 100));
    }
    throw StateError('Test OTP email was not delivered to local Mailpit');
  } finally {
    client.close();
  }
}
