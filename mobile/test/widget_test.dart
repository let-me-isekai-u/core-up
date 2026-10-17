import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:core_up_mobile/screens/onboarding/onboarding_screen.dart';

void main() {
  testWidgets('onboarding starts with the first CoreUp message', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: OnboardingScreen(destinationBuilder: (_) => const SizedBox()),
      ),
    );
    expect(find.text('Bắt đầu theo cách của bạn'), findsOneWidget);
    expect(find.text('Tiếp tục'), findsOneWidget);
  });
}
