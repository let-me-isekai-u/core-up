import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:google_sign_in/google_sign_in.dart';

/// SDK credentials stay in memory; only the Core-Up JWT is persisted by AuthProvider.
class GoogleAuthService {
  static final GoogleAuthService instance = GoogleAuthService._();
  GoogleAuthService._();
  Future<void>? _initialization;
  StreamSubscription<GoogleSignInAuthenticationEvent>? _events;
  GoogleSignInAccount? _account;

  Future<void> _initialize() async {
    const server = String.fromEnvironment('GOOGLE_SERVER_CLIENT_ID');
    const ios = String.fromEnvironment('GOOGLE_IOS_CLIENT_ID');
    const iosConfigured = bool.fromEnvironment(
      'GOOGLE_IOS_URL_SCHEME_CONFIGURED',
    );
    if (kIsWeb ||
        (defaultTargetPlatform != TargetPlatform.iOS &&
            defaultTargetPlatform != TargetPlatform.android)) {
      throw StateError(
        'Google Sign-In chỉ hỗ trợ iOS và Android trong Core-Up.',
      );
    }
    if (server.isEmpty) {
      throw StateError('Thiếu GOOGLE_SERVER_CLIENT_ID khi launch Flutter. Cần OAuth Web Client ID.');
    }
    if (defaultTargetPlatform == TargetPlatform.iOS && ios.isEmpty) {
      throw StateError('Thiếu GOOGLE_IOS_CLIENT_ID khi launch Flutter. Cần OAuth iOS Client ID khớp Bundle ID.');
    }
    if (defaultTargetPlatform == TargetPlatform.iOS && !iosConfigured) {
      throw StateError('Chưa xác nhận callback iOS. Cấu hình reversed URL scheme trong Info.plist rồi truyền GOOGLE_IOS_URL_SCHEME_CONFIGURED=true.');
    }
    await GoogleSignIn.instance.initialize(
      clientId: defaultTargetPlatform == TargetPlatform.iOS ? ios : null,
      serverClientId: server,
    );
    _events ??= GoogleSignIn.instance.authenticationEvents.listen(
      (event) {
        switch (event) {
          case GoogleSignInAuthenticationEventSignIn():
            _account = event.user;
          case GoogleSignInAuthenticationEventSignOut():
            _account = null;
        }
      },
      onError: (Object _) {
        _account = null;
      },
    );
  }

  Future<String?> idToken() async {
    _initialization ??= _initialize();
    try {
      await _initialization;
    } catch (e) {
      _initialization = null;
      if (e is StateError) rethrow;
      if (e is GoogleSignInException) {
        throw StateError('Khởi tạo Google SDK thất bại (${e.code.name}). Kiểm tra iOS Client ID, Web Client ID và cấu hình native; SDK chưa bắt đầu xác thực.');
      }
      throw StateError(
        'Chưa khởi tạo được Google Sign-In. Kiểm tra mạng và OAuth rồi thử lại.',
      );
    }
    try {
      if (!GoogleSignIn.instance.supportsAuthenticate()) {
        throw StateError('Thiết bị chưa hỗ trợ Google Sign-In.');
      }
      final account = await GoogleSignIn.instance.authenticate();
      _account = account;
      final token = _account!.authentication.idToken;
      if (token == null || token.isEmpty)
        throw StateError(
          'Google không trả ID token. Kiểm tra OAuth Client ID.',
        );
      return token;
    } on GoogleSignInException catch (e) {
      if (e.code == GoogleSignInExceptionCode.canceled) return null;
      throw StateError(switch (e.code) {
        GoogleSignInExceptionCode.clientConfigurationError => 'Google từ chối cấu hình ứng dụng. Kiểm tra iOS Client ID, Bundle ID và reversed URL scheme.',
        GoogleSignInExceptionCode.providerConfigurationError => 'Google SDK chưa sẵn sàng. Kiểm tra cấu hình OAuth của provider.',
        GoogleSignInExceptionCode.interrupted => 'Đăng nhập Google bị gián đoạn. Thử lại khi ứng dụng đang mở.',
        GoogleSignInExceptionCode.uiUnavailable => 'Chưa mở được giao diện Google. Quay lại màn hình đăng nhập rồi thử lại.',
        GoogleSignInExceptionCode.userMismatch => 'Tài khoản Google đã thay đổi. Đăng xuất rồi chọn lại tài khoản.',
        _ => 'Google Sign-In gặp lỗi SDK hoặc kết nối. Thử lại; nếu tiếp diễn, kiểm tra mạng và cấu hình Google Cloud.',
      });
    }
  }

  Future<void> signOut() async {
    _account = null;
    if (_initialization != null) {
      try {
        await _initialization;
        await GoogleSignIn.instance.signOut();
      } catch (_) {
        /* Core-Up logout still completes. */
      }
    }
  }
}
