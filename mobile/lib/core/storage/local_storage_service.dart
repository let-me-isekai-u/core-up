import 'dart:io' show pid;

import 'package:shared_preferences/shared_preferences.dart';

class LocalStorageService {
  static const _onboardingKey = 'has_seen_onboarding';
  static const _sessionModeKey = 'app_session_mode';
  static const _debugProcessKey = 'debug_process_id';

  Future<bool> hasSeenOnboarding() async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getBool(_onboardingKey) ?? false;
  }

  Future<void> markOnboardingSeen() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setBool(_onboardingKey, true);
  }

  Future<String?> sessionMode() async {
    final preferences = await SharedPreferences.getInstance();
    return preferences.getString(_sessionModeKey);
  }

  Future<void> saveSessionMode(String mode) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(_sessionModeKey, mode);
  }

  Future<void> clearSessionMode() async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.remove(_sessionModeKey);
  }

  Future<bool> isNewDebugProcess() async {
    final preferences = await SharedPreferences.getInstance();
    final previousProcess = preferences.getInt(_debugProcessKey);
    await preferences.setInt(_debugProcessKey, pid);
    return previousProcess != pid;
  }
}
