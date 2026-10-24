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
    if (server.isEmpty ||
        (defaultTargetPlatform == TargetPlatform.iOS &&
            (ios.isEmpty || !iosConfigured))) {
      throw StateError(
        'Chưa cấu hình Google OAuth. Cần Web Client ID; iOS cần Client ID và reversed URL scheme.',
      );
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
      throw StateError(
        'Không thể đăng nhập Google. Kiểm tra mạng và cấu hình OAuth rồi thử lại.',
      );
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
