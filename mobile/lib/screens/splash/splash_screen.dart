part of '../core_up_screens.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});
  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      final localStorage = context.read<LocalStorageService>();
      final showDebugOnboarding =
          kDebugMode && await localStorage.isNewDebugProcess();
      final seen = kDebugMode
          ? !showDebugOnboarding
          : await localStorage.hasSeenOnboarding();
      final status = await context.read<AuthProvider>().restoreSession();
      if (!mounted) return;
      final page = !seen
          ? OnboardingScreen(destinationBuilder: (_) => const AuthScreen())
          : status == AuthStatus.guest
          ? const ShellScreen()
          : status == AuthStatus.authenticated
          ? (context.read<AuthProvider>().needsEmailVerification
                ? const EmailVerificationScreen()
                : const ProfileSetupScreen())
          : const AuthScreen();
      Navigator.of(context).pushReplacement(fadeRoute(page));
    });
  }

  @override
  Widget build(BuildContext context) => const Scaffold(
    body: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            'CoreUp',
            style: TextStyle(fontSize: 38, fontWeight: FontWeight.w800),
          ),
          SizedBox(height: 10),
          Text(
            'XÂY DỰNG PHIÊN BẢN MẠNH MẼ HƠN',
            style: TextStyle(color: muted, letterSpacing: 1.4, fontSize: 11),
          ),
        ],
      ),
    ),
  );
}
