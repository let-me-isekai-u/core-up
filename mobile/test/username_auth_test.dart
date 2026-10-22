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

class AuthAdapter implements HttpClientAdapter {
  AuthAdapter(this.respond);
  final ResponseBody Function(RequestOptions) respond;
  final requests = <RequestOptions>[];
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return respond(options);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody jsonBody(String json, [int code = 200]) => ResponseBody.fromString(
  json,
  code,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
  },
);

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
  });

  testWidgets(
    'login has username only; registration validates confirm and supports visibility',
    (tester) async {
      final auth = AuthProvider(LocalStorageService());
      final adapter = AuthAdapter(
        (_) => jsonBody('{"detail":"Tên tài khoản đã được sử dụng."}', 409),
      );
      auth.dio.httpClientAdapter = adapter;
      await tester.pumpWidget(
        ChangeNotifierProvider.value(
          value: auth,
          child: const MaterialApp(home: AuthScreen(initialShowForm: true)),
        ),
      );
      expect(
        find.widgetWithText(TextFormField, 'Tên tài khoản'),
        findsOneWidget,
      );
      expect(find.widgetWithText(TextFormField, 'Email'), findsNothing);
      await tester.ensureVisible(find.text('Chưa có tài khoản? Tạo tài khoản'));
      await tester.tap(find.text('Chưa có tài khoản? Tạo tài khoản'));
      await tester.pumpAndSettle();
      final fields = find.byType(TextFormField);
      expect(fields, findsNWidgets(4));
      await tester.enterText(fields.at(0), 'valid_user');
      await tester.enterText(fields.at(1), 'user@example.com');
      await tester.enterText(fields.at(2), 'Password123!');
      await tester.enterText(fields.at(3), 'different');
      await tester.ensureVisible(find.text('TẠO TÀI KHOẢN').last);
      await tester.tap(find.text('TẠO TÀI KHOẢN').last);
      await tester.pump();
      expect(find.text('Mật khẩu nhập lại chưa khớp'), findsOneWidget);
      expect(adapter.requests, isEmpty);
      await tester.enterText(fields.at(0), 'ab-');
      await tester.enterText(fields.at(1), 'not-email');
      await tester.enterText(fields.at(2), 'short');
      await tester.ensureVisible(find.text('TẠO TÀI KHOẢN').last);
      await tester.tap(find.text('TẠO TÀI KHOẢN').last);
      await tester.pump();
      expect(
        find.text('Dùng 3–24 chữ Latin, số hoặc dấu gạch dưới'),
        findsOneWidget,
      );
      expect(find.text('Nhập email hợp lệ'), findsOneWidget);
      expect(find.text('Dùng ít nhất 8 ký tự'), findsOneWidget);
      expect(adapter.requests, isEmpty);
      await tester.ensureVisible(find.byTooltip('Hiện mật khẩu').first);
      await tester.tap(find.byTooltip('Hiện mật khẩu').first);
      await tester.pump();
      expect(find.byTooltip('Ẩn mật khẩu'), findsOneWidget);
    },
  );

  test(
    'username register/login payload, secure token and restore profile missing',
    () async {
      final storage = LocalStorageService();
      final auth = AuthProvider(storage);
      final adapter = AuthAdapter(
        (o) => o.path == '/api/profile'
            ? jsonBody('{"detail":"Profile was not found."}', 404)
            : jsonBody(
                '{"userId":"u1","email":"user@example.com","accessToken":"test-token","needsProfile":true}',
              ),
      );
      auth.dio.httpClientAdapter = adapter;
      expect(
        await auth.auth(
          'register',
          ' User_1 ',
          'Password123!',
          email: 'user@example.com',
        ),
        isNotNull,
      );
      expect(adapter.requests.single.data, {
        'username': 'User_1',
        'email': 'user@example.com',
        'password': 'Password123!',
      });
      expect(
        await const FlutterSecureStorage().read(key: 'access_token'),
        'test-token',
      );
      await auth.auth('login', 'USER_1', 'Password123!');
      expect((adapter.requests.last.data as Map).containsKey('email'), isFalse);
      expect(
        (adapter.requests.last.data as Map).containsKey('confirmPassword'),
        isFalse,
      );
      final restored = AuthProvider(storage)..dio.httpClientAdapter = adapter;
      expect(await restored.restoreSession(), AuthStatus.authenticated);
      expect(restored.needsProfile, isTrue);
      expect(
        adapter.requests.last.headers['Authorization'],
        'Bearer test-token',
      );
    },
  );

  test(
    'restore valid, expired and offline tokens; guest never loads a profile',
    () async {
      for (final code in [200, 401, 503]) {
        SharedPreferences.setMockInitialValues({
          'app_session_mode': 'authenticated',
        });
        FlutterSecureStorage.setMockInitialValues({
          'access_token': 'test-token',
        });
        final auth = AuthProvider(LocalStorageService());
        auth.dio.httpClientAdapter = AuthAdapter((_) => jsonBody('{}', code));
        expect(
          await auth.restoreSession(),
          code == 401 ? AuthStatus.unauthenticated : AuthStatus.authenticated,
        );
        expect(auth.token, code == 401 ? isNull : 'test-token');
      }
      final auth = AuthProvider(LocalStorageService());
      final adapter = AuthAdapter(
        (_) => throw StateError('Guest must not call profile'),
      );
      auth.dio.httpClientAdapter = adapter;
      await auth.continueAsGuest();
      expect(await auth.restoreSession(), AuthStatus.guest);
      expect(adapter.requests, isEmpty);
    },
  );
  test(
    'double submit sends one request and maps duplicate username to field',
    () async {
      final auth = AuthProvider(LocalStorageService());
      final adapter = AuthAdapter(
        (_) => jsonBody('{"detail":"Tên tài khoản đã được sử dụng."}', 409),
      );
      auth.dio.httpClientAdapter = adapter;
      final first = auth.auth(
        'register',
        'User_1',
        'Password123!',
        email: 'u@example.com',
      );
      final duplicate = auth.auth(
        'register',
        'User_1',
        'Password123!',
        email: 'u@example.com',
      );
      expect(await duplicate, isNull);
      expect(await first, isNull);
      expect(adapter.requests, hasLength(1));
      expect(auth.fieldErrors['username'], 'Tên tài khoản đã được sử dụng.');
      expect(auth.submitting, isFalse);
    },
  );
}
