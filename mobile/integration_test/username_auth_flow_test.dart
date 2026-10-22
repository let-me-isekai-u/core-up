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
    'native Flutter register, profile, username login, JWT and restore',
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
        await tester.pumpAndSettle();
      }

      try {
        await tester.ensureVisible(
          find.text('Chưa có tài khoản? Tạo tài khoản'),
        );
        await tester.tap(find.text('Chưa có tài khoản? Tạo tài khoản'));
        await tester.pumpAndSettle();
        var fields = find.byType(TextFormField);
        await tester.enterText(fields.at(0), name);
        await tester.enterText(fields.at(1), '$name@example.invalid');
        await tester.enterText(fields.at(2), 'NativeTest-123!');
        await tester.enterText(fields.at(3), 'NativeTest-123!');
        await tester.ensureVisible(find.text('TẠO TÀI KHOẢN').last);
        await tester.tap(find.text('TẠO TÀI KHOẢN').last);
        await waitFor(find.byType(ProfileSetupScreen));
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
        await tester.pumpAndSettle();
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
        await tester.pumpAndSettle();
        fields = find.byType(TextFormField);
        expect(fields, findsNWidgets(2));
        await tester.enterText(fields.at(0), name.toUpperCase());
        await tester.enterText(fields.at(1), 'NativeTest-123!');
        await tester.ensureVisible(find.text('TIẾP TỤC'));
        await tester.tap(find.text('TIẾP TỤC'));
        await waitFor(find.byType(ShellScreen));
        expect(auth.isAuthenticated, isTrue);
        expect(tester.takeException(), isNull);
      } finally {
        await auth.logout();
        await tester.pumpWidget(const SizedBox.shrink());
        await tester.pumpAndSettle();
      }
    },
  );
}
