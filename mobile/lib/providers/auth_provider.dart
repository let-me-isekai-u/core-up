import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/auth/auth_response.dart';
import '../services/google_auth_service.dart';
import '../core/storage/local_storage_service.dart';

const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://127.0.0.1:5027',
);

enum AuthStatus { initial, unauthenticated, guest, authenticated }

class AuthProvider extends ChangeNotifier {
  // The native macOS login Keychain supports the project's ad-hoc desktop
  // development signing. iOS/Android keep their existing secure-storage defaults.
  final _storage = const FlutterSecureStorage(
    mOptions: MacOsOptions(usesDataProtectionKeychain: false),
  );
  final LocalStorageService localStorage;
  final String tokenStorageKey;
  late final Dio dio;
  String? token, error;
  Map<String, String> fieldErrors = {};
  AuthStatus status = AuthStatus.initial;
  bool needsProfile = false;
  bool needsEmailVerification = false;
  bool verificationRequired = false;
  String? email;
  bool submitting = false;
  AuthProvider(this.localStorage, {this.tokenStorageKey = 'access_token'}) {
    dio = Dio(
      BaseOptions(
        baseUrl: apiBaseUrl,
        connectTimeout: const Duration(seconds: 12),
      ),
    );
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (o, h) {
          if (token != null) o.headers['Authorization'] = 'Bearer ' + token!;
          h.next(o);
        },
      ),
    );
  }
  bool get isGuest => status == AuthStatus.guest;
  bool get isAuthenticated => status == AuthStatus.authenticated;

  Future<void> _deleteStoredToken() async {
    // The macOS plugin probes synchronizable variants when deleting. On an
    // ad-hoc build, deleting an absent key can return a missing-entitlement error.
    // Read the configured key first so logout/reset/Guest cleanup is idempotent.
    if (await _storage.read(key: tokenStorageKey) != null) {
      await _storage.delete(key: tokenStorageKey);
    }
  }

  Future<AuthStatus> restoreSession() async {
    final savedMode = await localStorage.sessionMode();
    if (savedMode == 'guest') {
      token = null;
      await _deleteStoredToken();
      status = AuthStatus.guest;
      notifyListeners();
      return status;
    }
    token = await _storage.read(key: tokenStorageKey);
    if (token != null) {
      try {
        final me = Map<String, dynamic>.from(
          (await dio.get('/api/auth/me')).data,
        );
        needsProfile = me['needsProfile'] == true;
        needsEmailVerification = me['needsEmailVerification'] == true;
        verificationRequired = me['verificationRequired'] == true;
        email = me['email']?.toString();
        status = AuthStatus.authenticated;
        await localStorage.saveSessionMode('authenticated');
        notifyListeners();
        return status;
      } on DioException catch (e) {
        if (e.response?.statusCode != 401) {
          needsProfile = e.response?.statusCode == 404;
          error = needsProfile ? null : message(e);
          status = AuthStatus.authenticated;
          notifyListeners();
          return status;
        }
        token = null;
        await _deleteStoredToken();
        await localStorage.clearSessionMode();
      }
    }
    status = await localStorage.sessionMode() == 'guest'
        ? AuthStatus.guest
        : AuthStatus.unauthenticated;
    notifyListeners();
    return status;
  }

  Future<void> continueAsGuest() async {
    token = null;
    needsProfile = false;
    needsEmailVerification = false;
    email = null;
    await _deleteStoredToken();
    await localStorage.saveSessionMode('guest');
    status = AuthStatus.guest;
    error = null;
    notifyListeners();
  }

  Future<AuthResponse?> auth(
    String path,
    String username,
    String password, {
    String? email,
  }) async {
    if (submitting) return null;
    submitting = true;
    error = null;
    fieldErrors = {};
    notifyListeners();
    try {
      final r = await dio.post(
        '/api/auth/' + path,
        data: {
          'username': username.trim(),
          'password': password,
          if (path == 'register') 'email': email?.trim(),
        },
      );
      final data = AuthResponse.fromJson(Map<String, dynamic>.from(r.data));
      token = data.accessToken;
      needsProfile = data.needsProfile;
      needsEmailVerification = data.needsEmailVerification;
      verificationRequired = data.verificationRequired;
      this.email = data.email;
      await _storage.write(key: tokenStorageKey, value: token);
      await localStorage.saveSessionMode('authenticated');
      status = AuthStatus.authenticated;
      notifyListeners();
      return data;
    } catch (e) {
      error = message(e);
      if (e is DioException) {
        if (path == 'register' && e.response?.statusCode == 409) {
          fieldErrors[error!.startsWith('Email') ? 'email' : 'username'] =
              error!;
        }
        final body = e.response?.data;
        if (body is Map && body['errors'] is Map) {
          for (final key in (body['errors'] as Map).keys) {
            final field = key.toString().toLowerCase();
            fieldErrors[field] = switch (field) {
              'username' => 'Dùng 3–24 chữ Latin, số hoặc dấu gạch dưới',
              'email' => 'Nhập email hợp lệ',
              'password' => 'Mật khẩu cần từ 8 đến 128 ký tự',
              _ => 'Thông tin chưa hợp lệ',
            };
          }
        }
      }
      notifyListeners();
      return null;
    } finally {
      submitting = false;
      notifyListeners();
    }
  }

  void _requireGoogleTransport() {
    final uri = Uri.parse(apiBaseUrl);
    final localDebug = kDebugMode && ['localhost', '127.0.0.1', '10.0.2.2', '::1'].contains(uri.host);
    if (uri.scheme != 'https' && !localDebug) {
      throw StateError('Google Sign-In yêu cầu backend HTTPS (HTTP chỉ dành cho localhost debug).');
    }
  }

  Future<AuthResponse?> googleSignIn() async {
    if (submitting) return null;
    submitting = true; error = null; fieldErrors = {}; notifyListeners();
    try {
      _requireGoogleTransport();
      final idToken = await GoogleAuthService.instance.idToken();
      if (idToken == null) return null;
      final response = await dio.post('/api/auth/google', data: {'idToken': idToken});
      final data = AuthResponse.fromJson(Map<String, dynamic>.from(response.data));
      token = data.accessToken; needsProfile = data.needsProfile;
      needsEmailVerification = data.needsEmailVerification;
      verificationRequired = data.verificationRequired; email = data.email;
      await _storage.write(key: tokenStorageKey, value: token);
      await localStorage.saveSessionMode('authenticated');
      status = AuthStatus.authenticated;
      return data;
    } catch (e) {
      error = e is StateError ? e.message.toString() : message(e);
      return null;
    } finally { submitting = false; notifyListeners(); }
  }

  Future<bool> linkGoogle(String password) async {
    if (!isAuthenticated || submitting) return false;
    submitting = true; error = null; notifyListeners();
    final owner = token;
    try {
      _requireGoogleTransport();
      final idToken = await GoogleAuthService.instance.idToken();
      if (idToken == null || token != owner) return false;
      await dio.post('/api/auth/google/link', data: {'idToken': idToken, 'currentPassword': password});
      await logout();
      return true;
    } catch (e) {
      error = e is StateError ? e.message.toString() : securityMessage(e);
      return false;
    } finally { submitting = false; notifyListeners(); }
  }

  Future<bool> securityAction(String path, Map<String, dynamic> data) async {
    error = null;
    try {
      await dio.post('/api/auth/$path', data: data);
      return true;
    } catch (e) {
      error = securityMessage(e);
      notifyListeners();
      return false;
    }
  }

  String securityMessage(Object e) {
    if (e is DioException && e.response?.data is Map) {
      final body = e.response!.data as Map;
      if (body['detail'] is String) return body['detail'] as String;
      if (body['errors'] is Map)
        return 'Kiểm tra mã OTP, mật khẩu và xác nhận mật khẩu.';
    }
    return message(e);
  }

  Future<void> logout() async {
    await GoogleAuthService.instance.signOut();
    token = null;
    needsProfile = false;
    needsEmailVerification = false;
    email = null;
    await _deleteStoredToken();
    await localStorage.clearSessionMode();
    status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  String message(Object e) {
    if (e is DioException) {
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout)
        return 'Không thể kết nối tới máy chủ.';
      if (e.response?.statusCode == 400) {
        return 'Thông tin chưa hợp lệ. Kiểm tra tên tài khoản, email và mật khẩu.';
      }
      if (e.response?.statusCode == 401) {
        return 'Tên tài khoản hoặc mật khẩu không đúng.';
      }
      if (e.response?.statusCode == 429) {
        return 'Bạn thao tác quá nhanh. Vui lòng thử lại sau.';
      }
      if ((e.response?.statusCode ?? 0) >= 500) {
        return 'Máy chủ đang gặp sự cố. Vui lòng thử lại sau.';
      }
      final d = e.response?.data;
      if (d is Map)
        return (d['detail'] ?? d['title'] ?? 'Yêu cầu thất bại.').toString();
    }
    return 'Đã có lỗi xảy ra.';
  }
}
