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
        ExerciseVisual(slug: item['slug'].toString()),
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
        const SizedBox(height: 12),
        const Text('Dừng khi đau, chóng mặt, khó thở hoặc mất kiểm soát tư thế. Nếu có chấn thương hoặc chưa chắc khả năng tập, hãy hỏi chuyên viên y tế trước khi bắt đầu.',
          style: TextStyle(color: muted, height: 1.5)),
        const SizedBox(height: 24),
        PrimaryButton(
          label: context.watch<AuthProvider>().isGuest ? 'BẮT ĐẦU GIÁO TRÌNH KHÁCH' : 'ĐÃ XEM HƯỚNG DẪN',
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
