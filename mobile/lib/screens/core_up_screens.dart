library core_up_screens;

import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/storage/local_storage_service.dart';
import '../core/theme/app_colors.dart';
import '../core/utils/minimum_loading_duration.dart';
import '../providers/auth_provider.dart';
import '../providers/profile_provider.dart';
import '../providers/workout_provider.dart';
import 'onboarding/onboarding_screen.dart';

part 'splash/splash_screen.dart';
part 'auth/auth_screen.dart';
part 'auth/security_screens.dart';
part 'profile/profile_setup_screen.dart';
part 'home/app_shell.dart';
part 'home/home_screen.dart';
part 'exercises/exercise_list_screen.dart';
part 'exercises/exercise_detail_screen.dart';
part 'workout/workout_session_screen.dart';
part 'workout/rest_screen.dart';
part 'workout/workout_completed_screen.dart';
part 'progress/progress_screen.dart';
part 'profile/profile_screen.dart';
part '../widgets/common/core_widgets.dart';
part '../widgets/common/coreup_chibi_loading.dart';

const bg = AppColors.background;
const surface = AppColors.surface;
const blue = AppColors.primary;
const muted = AppColors.textSecondary;

class CoreUp extends StatelessWidget {
  const CoreUp({super.key});

  @override
  Widget build(BuildContext context) {
    final localStorage = LocalStorageService();
    return MultiProvider(
      providers: [
        Provider.value(value: localStorage),
        ChangeNotifierProvider(create: (_) => AuthProvider(localStorage)),
        ChangeNotifierProxyProvider<AuthProvider, ProfileProvider>(
          create: (context) => ProfileProvider(context.read<AuthProvider>()),
          update: (_, auth, previous) => previous ?? ProfileProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, WorkoutProvider>(
          create: (context) => WorkoutProvider(context.read<AuthProvider>()),
          update: (_, auth, previous) => previous ?? WorkoutProvider(auth),
        ),
      ],
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        title: 'CoreUp',
        theme: ThemeData(
          brightness: Brightness.dark,
          scaffoldBackgroundColor: Colors.transparent,
          canvasColor: Colors.transparent,
          colorScheme: const ColorScheme.dark(primary: blue, surface: surface),
          appBarTheme: const AppBarTheme(
            backgroundColor: Colors.transparent,
            elevation: 0,
            scrolledUnderElevation: 0,
            surfaceTintColor: Colors.transparent,
          ),
          inputDecorationTheme: const InputDecorationTheme(
            filled: true,
            fillColor: AppColors.surfaceElevated,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.all(Radius.circular(16)),
              borderSide: BorderSide.none,
            ),
          ),
          chipTheme: const ChipThemeData(
            selectedColor: blue,
            checkmarkColor: Colors.white,
            labelStyle: TextStyle(color: Colors.white),
            side: BorderSide(color: Colors.white38),
            shape: StadiumBorder(),
          ),
        ),
        builder: (_, child) => Stack(
          fit: StackFit.expand,
          children: [
            Image.asset('assets/splash_background.png', fit: BoxFit.cover),
            const ColoredBox(color: Color(0xe6070b12)),
            if (child != null) child,
          ],
        ),
        home: const SplashScreen(),
      ),
    );
  }
}
