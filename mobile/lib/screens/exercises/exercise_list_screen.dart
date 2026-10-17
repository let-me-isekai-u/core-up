part of '../core_up_screens.dart';

class ExercisesScreen extends StatefulWidget {
  const ExercisesScreen({super.key});
  @override
  State<ExercisesScreen> createState() => _ExercisesScreenState();
}

class _ExercisesScreenState extends State<ExercisesScreen> {
  var _query = '';
  var _difficulty = 'Tất cả';
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback(
      (_) => context.read<WorkoutProvider>().loadExercises(),
    );
  }

  @override
  Widget build(BuildContext context) {
    final data = context.watch<WorkoutProvider>();
    final items = data.exercises
        .where(
          (x) =>
              x['name'].toString().toLowerCase().contains(
                _query.toLowerCase(),
              ) &&
              (_difficulty == 'Tất cả' || x['difficulty'] == _difficulty),
        )
        .toList();
    return SafeArea(
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 18, 20, 10),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'BÀI TẬP',
                style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: TextField(
              onChanged: (v) => setState(() => _query = v),
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search),
                hintText: 'Tìm bài tập...',
              ),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 38,
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              scrollDirection: Axis.horizontal,
              children: ['Tất cả', 'Beginner', 'Intermediate', 'Advanced']
                  .map(
                    (value) => Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: FilterChip(
                        label: Text(
                          value == 'Tất cả' ? value : localizeValue(value),
                        ),
                        selected: _difficulty == value,
                        onSelected: (_) => setState(() => _difficulty = value),
                      ),
                    ),
                  )
                  .toList(),
            ),
          ),
          Expanded(
            child: data.loading
                ? const CoreUpChibiLoading(message: 'Đang tải bài tập...')
                : items.isEmpty
                ? const Center(
                    child: Text(
                      'Không tìm thấy bài tập',
                      style: TextStyle(color: muted),
                    ),
                  )
                : ListView.separated(
                    padding: const EdgeInsets.all(20),
                    itemCount: items.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (_, index) => SizedBox(
                      height: 96,
                      child: SurfaceCard(
                        child: ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: CircleAvatar(
                            radius: 27,
                            backgroundColor: AppColors.surfaceElevated,
                            child: Icon(
                              exerciseIcon(
                                items[index]['muscleGroup'].toString(),
                              ),
                              color: blue,
                            ),
                          ),
                          title: Text(
                            items[index]['name'],
                            style: const TextStyle(fontWeight: FontWeight.bold),
                          ),
                          subtitle: Text(
                            localizeValue(
                                  items[index]['muscleGroup'].toString(),
                                ) +
                                ' • ' +
                                localizeValue(
                                  items[index]['difficulty'].toString(),
                                ),
                          ),
                          onTap: () => Navigator.of(context).push(
                            fadeRoute(ExerciseDetailScreen(item: items[index])),
                          ),
                          trailing: const Icon(Icons.chevron_right_rounded),
                        ),
                      ),
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}
