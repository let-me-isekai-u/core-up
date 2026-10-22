import 'dart:typed_data';
import 'package:core_up_mobile/core/storage/local_storage_service.dart';
import 'package:core_up_mobile/providers/auth_provider.dart';
import 'package:core_up_mobile/screens/core_up_screens.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

class SecurityAdapter implements HttpClientAdapter {
  final requests = <RequestOptions>[];
  ResponseBody Function(RequestOptions) respond;
  SecurityAdapter(this.respond);
  @override
  Future<ResponseBody> fetch(
    RequestOptions o,
    Stream<Uint8List>? s,
    Future<void>? cancel,
  ) async {
    requests.add(o);
    return respond(o);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody json(String s, [int status = 200]) => ResponseBody.fromString(
  s,
  status,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
  },
);

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
  });
  Future<AuthProvider> show(
    WidgetTester t,
    Widget screen,
    SecurityAdapter adapter,
  ) async {
    final auth = AuthProvider(LocalStorageService())
      ..status = AuthStatus.authenticated
      ..token = 'fixture-token';
    auth.dio.httpClientAdapter = adapter;
    await t.pumpWidget(
      ChangeNotifierProvider.value(
        value: auth,
        child: MaterialApp(home: screen),
      ),
    );
    return auth;
  }

  testWidgets(
    'OTP form rejects non-six-digit values and shows server failure; timer disposes',
    (t) async {
      final adapter = SecurityAdapter(
        (_) => json('{"detail":"Mã không hợp lệ hoặc đã hết hạn."}', 400),
      );
      await show(t, const EmailVerificationScreen(), adapter);
      expect(find.textContaining('Gửi lại sau'), findsOneWidget);
      await t.enterText(find.byType(TextFormField), '123');
      await t.tap(find.text('XÁC MINH'));
      await t.pump();
      expect(find.text('Nhập mã OTP gồm 6 chữ số'), findsOneWidget);
      expect(adapter.requests.where((r) => r.method == 'POST'), isEmpty);
      await t.enterText(find.byType(TextFormField), '123456');
      await t.tap(find.text('XÁC MINH'));
      await t.pumpAndSettle();
      expect(find.text('Mã không hợp lệ hoặc đã hết hạn.'), findsOneWidget);
      await t.pumpWidget(const SizedBox.shrink());
      await t.pump(const Duration(seconds: 61));
      expect(t.takeException(), isNull);
    },
  );
  testWidgets(
    'forgot OTP then reset requires matching confirmation; no auto login',
    (t) async {
      final adapter = SecurityAdapter(
        (o) => o.path.endsWith('/verify')
            ? json('{"resetToken":"${'A' * 64}"}')
            : json('{"message":"Nếu email đủ điều kiện"}', 202),
      );
      final auth = await show(t, const PasswordRecoveryScreen(), adapter);
      auth.status = AuthStatus.unauthenticated;
      auth.token = null;
      await t.enterText(find.byType(TextFormField), 'user@example.com');
      await t.tap(find.text('GỬI MÃ OTP'));
      await t.pumpAndSettle();
      expect(auth.isAuthenticated, isFalse);
      await t.enterText(find.byType(TextFormField), '123456');
      await t.tap(find.text('KIỂM TRA MÃ'));
      await t.pumpAndSettle();
      await t.enterText(find.byType(TextFormField).at(0), 'NewPassword-123!');
      await t.enterText(find.byType(TextFormField).at(1), 'mismatch');
      await t.tap(find.widgetWithText(ElevatedButton, 'ĐẶT LẠI MẬT KHẨU'));
      await t.pump();
      expect(find.text('Mật khẩu nhập lại chưa khớp'), findsOneWidget);
      expect(adapter.requests, hasLength(2));
      await t.pumpWidget(const SizedBox.shrink());
    },
  );
  testWidgets(
    'change password validates confirmation, handles wrong current and logs out on success',
    (t) async {
      final adapter = SecurityAdapter(
        (_) => json('{"detail":"Mật khẩu hiện tại không đúng."}', 400),
      );
      final auth = await show(t, const ChangePasswordScreen(), adapter);
      final fields = find.byType(TextFormField);
      await t.enterText(fields.at(0), 'OldPassword-123!');
      await t.enterText(fields.at(1), 'NewPassword-123!');
      await t.enterText(fields.at(2), 'mismatch');
      await t.ensureVisible(
        find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'),
      );
      await t.tap(find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'));
      await t.pump();
      expect(find.text('Mật khẩu nhập lại chưa khớp'), findsOneWidget);
      expect(adapter.requests, isEmpty);
      await t.enterText(fields.at(2), 'NewPassword-123!');
      await t.tap(find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'));
      await t.pumpAndSettle();
      expect(find.text('Mật khẩu hiện tại không đúng.'), findsOneWidget);
      expect(auth.token, isNotNull);
      adapter.respond = (_) => json('{"message":"Đã đổi"}');
      await t.tap(find.widgetWithText(ElevatedButton, 'ĐỔI MẬT KHẨU'));
      await t.pumpAndSettle();
      expect(auth.token, isNull);
      expect(find.byType(AuthScreen), findsOneWidget);
    },
  );
  testWidgets(
    'legacy can postpone verification; new account cannot bypass it',
    (t) async {
      final adapter = SecurityAdapter((_) => json('{}'));
      final auth = await show(t, const EmailVerificationScreen(), adapter);
      await t.pumpAndSettle();
      expect(find.text('Để sau'), findsOneWidget);
      auth.verificationRequired = true;
      auth.notifyListeners();
      await t.pump();
      expect(find.text('Để sau'), findsNothing);
      await t.pumpWidget(const SizedBox.shrink());
      await t.pumpAndSettle();
    },
  );
}
