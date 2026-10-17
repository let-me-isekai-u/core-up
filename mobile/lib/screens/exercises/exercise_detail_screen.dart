part of '../core_up_screens.dart';

class ExerciseDetailScreen extends StatelessWidget {
  const ExerciseDetailScreen({super.key, required this.item});
  final Map<String, dynamic> item;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        Container(
          height: 220,
          decoration: BoxDecoration(
            color: surface,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Icon(
            exerciseIcon(item['muscleGroup'].toString()),
            size: 72,
            color: blue,
          ),
        ),
        const SizedBox(height: 24),
        Text(
          item['name'],
          style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 8),
        Text(
          localizeValue(item['muscleGroup'].toString()) +
              ' • ' +
              localizeValue(item['difficulty'].toString()),
          style: const TextStyle(color: blue),
        ),
        const SizedBox(height: 22),
        const Text(
          'HƯỚNG DẪN',
          style: TextStyle(fontWeight: FontWeight.bold, letterSpacing: 1.2),
        ),
        const SizedBox(height: 8),
        Text(
          item['instructions'] ??
              item['description'] ??
              'Thực hiện đúng kỹ thuật và kiểm soát từng lần lặp.',
          style: const TextStyle(color: muted, height: 1.5),
        ),
        const SizedBox(height: 24),
        PrimaryButton(
          label: 'BẮT ĐẦU',
          onTap: () {
            final workout = context.read<WorkoutProvider>();
            if (context.read<AuthProvider>().isGuest) {
              workout.startGuestWorkout();
              Navigator.of(
                context,
              ).push(fadeRoute(const WorkoutSessionScreen()));
            } else {
              Navigator.of(context).pop();
            }
          },
        ),
      ],
    ),
  );
}
