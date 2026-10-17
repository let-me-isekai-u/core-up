import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/auth/auth_response.dart';
import '../core/storage/local_storage_service.dart';

const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://127.0.0.1:5027',
);

enum AuthStatus { initial, unauthenticated, guest, authenticated }

class AuthProvider extends ChangeNotifier {
  final _storage = const FlutterSecureStorage();
  final LocalStorageService localStorage;
  late final Dio dio;
  String? token, error;
  AuthStatus status = AuthStatus.initial;
  AuthProvider(this.localStorage) {
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

  Future<AuthStatus> restoreSession() async {
    final savedMode = await localStorage.sessionMode();
    if (savedMode == 'guest') {
      token = null;
      await _storage.delete(key: 'access_token');
      status = AuthStatus.guest;
      notifyListeners();
      return status;
    }
    token = await _storage.read(key: 'access_token');
    if (token != null) {
      try {
        await dio.get('/api/profile');
        status = AuthStatus.authenticated;
        await localStorage.saveSessionMode('authenticated');
        notifyListeners();
        return status;
      } catch (_) {
        token = null;
        await _storage.delete(key: 'access_token');
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
    await _storage.delete(key: 'access_token');
    await localStorage.saveSessionMode('guest');
    status = AuthStatus.guest;
    error = null;
    notifyListeners();
  }

  Future<AuthResponse?> auth(String path, String email, String password) async {
    error = null;
    notifyListeners();
    try {
      final r = await dio.post(
        '/api/auth/' + path,
        data: {'email': email, 'password': password},
      );
      final data = AuthResponse.fromJson(Map<String, dynamic>.from(r.data));
      token = data.accessToken;
      await _storage.write(key: 'access_token', value: token);
      await localStorage.saveSessionMode('authenticated');
      status = AuthStatus.authenticated;
      notifyListeners();
      return data;
    } catch (e) {
      error = message(e);
      notifyListeners();
      return null;
    }
  }

  Future<void> logout() async {
    token = null;
    await _storage.delete(key: 'access_token');
    await localStorage.clearSessionMode();
    status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  String message(Object e) {
    if (e is DioException) {
      if (e.type == DioExceptionType.connectionError ||
          e.type == DioExceptionType.connectionTimeout)
        return 'Không thể kết nối tới máy chủ.';
      final d = e.response?.data;
      if (d is Map)
        return (d['detail'] ?? d['title'] ?? 'Yêu cầu thất bại.').toString();
    }
    return 'Đã có lỗi xảy ra.';
  }
}
