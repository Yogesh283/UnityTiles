import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/constants/pool_rules.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../data/models/models.dart';
import '../../providers/app_providers.dart';
import '../../widgets/buttons.dart';
import '../../widgets/common.dart';
import '../../widgets/containers.dart';

class CreatePoolScreen extends ConsumerWidget {
  const CreatePoolScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final form = ref.watch(createPoolFormProvider);

    void setForm(CreatePoolForm next) {
      ref.read(createPoolFormProvider.notifier).state = next;
    }

    void applyPreset({int? players, double? entryFee}) {
      final p = players ?? form.players;
      final e = entryFee ?? form.entryFee;
      setForm(CreatePoolForm.fromPreset(players: p, entryFee: e));
    }

    void update(void Function(CreatePoolForm f) fn) {
      final next = CreatePoolForm(
        players: form.players,
        winnerCount: form.winnerCount,
        entryFee: form.entryFee,
        firstPercent: form.firstPercent,
        secondPercent: form.secondPercent,
        thirdPercent: form.thirdPercent,
        othersPercent: form.othersPercent,
        notes: form.notes,
      );
      fn(next);
      setForm(next);
    }

    final amounts = PoolRules.prizeAmounts(form.players, form.entryFee);

    return AppScaffold(
      title: 'Create Pool',
      showBack: true,
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            'IQFX Pro Tournament · Pool Play',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 4),
          const Text(
            'Prize Pool = 70% of total collection',
            style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
          ),
          const SizedBox(height: 16),

          // Entry fee chips
          const Text('Entry Fee', style: TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: PoolRules.entryFees.map((fee) {
              final selected = form.entryFee == fee;
              return ChoiceChip(
                label: Text('₹${fee.toInt()}'),
                selected: selected,
                onSelected: (_) => applyPreset(entryFee: fee),
                selectedColor: AppColors.purple.withValues(alpha: 0.35),
                labelStyle: TextStyle(
                  color: selected ? AppColors.white : AppColors.textSecondary,
                  fontWeight: FontWeight.w700,
                ),
              );
            }).toList(),
          ),
          const SizedBox(height: 16),

          // Player size chips
          const Text('Players', style: TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: PoolRules.playerSizes.map((size) {
              final selected = form.players == size;
              return ChoiceChip(
                label: Text('$size'),
                selected: selected,
                onSelected: (_) => applyPreset(players: size),
                selectedColor: AppColors.blue.withValues(alpha: 0.35),
                labelStyle: TextStyle(
                  color: selected ? AppColors.white : AppColors.textSecondary,
                  fontWeight: FontWeight.w700,
                ),
              );
            }).toList(),
          ),
          const SizedBox(height: 16),

          GlassCard(
            child: Column(
              children: [
                _ReadonlyRow(label: '👥 Players', value: '${form.players}'),
                _ReadonlyRow(label: '🏆 Winners', value: '${form.winnerCount}'),
                _ReadonlyRow(label: '💰 Entry Fee', value: formatRupee(form.entryFee)),
                _ReadonlyRow(
                  label: '🎁 Prize Pool',
                  value: formatRupee(form.prizePool),
                  highlight: true,
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),

          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('📈 Prize Distribution', style: TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 8),
                Text(
                  PoolRules.distributionLabel(form.winnerCount),
                  style: const TextStyle(color: AppColors.gold, fontWeight: FontWeight.w600),
                ),
                const SizedBox(height: 12),
                ...amounts.entries.map(
                  (e) => Padding(
                    padding: const EdgeInsets.only(bottom: 6),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(e.key, style: const TextStyle(color: AppColors.textSecondary)),
                        Text(
                          formatRupee(e.value),
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                      ],
                    ),
                  ),
                ),
                const Divider(height: 20),
                _NumberField(
                  label: '🥇 1st %',
                  value: form.firstPercent.toStringAsFixed(0),
                  onChanged: (v) => update((f) => f.firstPercent = double.tryParse(v) ?? f.firstPercent),
                ),
                if (form.winnerCount >= 2)
                  _NumberField(
                    label: '🥈 2nd %',
                    value: form.secondPercent.toStringAsFixed(0),
                    onChanged: (v) => update((f) => f.secondPercent = double.tryParse(v) ?? f.secondPercent),
                  ),
                if (form.winnerCount >= 3)
                  _NumberField(
                    label: '🥉 3rd %',
                    value: form.thirdPercent.toStringAsFixed(0),
                    onChanged: (v) => update((f) => f.thirdPercent = double.tryParse(v) ?? f.thirdPercent),
                  ),
                _NumberField(
                  label: '🏅 Others %',
                  value: form.othersPercent.toStringAsFixed(0),
                  onChanged: (v) => update((f) => f.othersPercent = double.tryParse(v) ?? f.othersPercent),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),

          const _PoolStructureTable(),
          const SizedBox(height: 12),
          const _WinnerDistributionTable(),
          const SizedBox(height: 12),

          GlassCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('📌 Notes', style: TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 8),
                const Text(
                  '• Prize Pool = 70% of Total Collection\n'
                  '• Remaining 30% = Platform Fee + Payment Gateway + Operations + Promotions\n'
                  '• Winners की संख्या Tournament Size के अनुसार बढ़ाई या घटाई जा सकती है।',
                  style: TextStyle(color: AppColors.textSecondary, height: 1.45, fontSize: 13),
                ),
                const SizedBox(height: 8),
                TextField(
                  decoration: const InputDecoration(labelText: 'Custom notes'),
                  maxLines: 2,
                  onChanged: (v) => update((f) => f.notes = v),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                child: AppButton(
                  label: 'Reset',
                  outlined: true,
                  onPressed: () => setForm(CreatePoolForm.fromPreset(players: 10, entryFee: 10)),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: GradientButton(
                  label: 'Create Pool',
                  onPressed: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text(
                          'Pool created · ${form.players}P · ${formatRupee(form.entryFee)} · Prize ${formatRupee(form.prizePool)}',
                        ),
                      ),
                    );
                    context.pop();
                  },
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

class _ReadonlyRow extends StatelessWidget {
  const _ReadonlyRow({required this.label, required this.value, this.highlight = false});
  final String label;
  final String value;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: AppColors.textSecondary)),
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.w800,
              color: highlight ? AppColors.gold : AppColors.white,
              fontSize: highlight ? 18 : 15,
            ),
          ),
        ],
      ),
    );
  }
}

class _PoolStructureTable extends StatelessWidget {
  const _PoolStructureTable();

  @override
  Widget build(BuildContext context) {
    return GlassCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('📊 Pool Play Structure', style: TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 10),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: DataTable(
              headingRowHeight: 36,
              dataRowMinHeight: 36,
              dataRowMaxHeight: 40,
              columnSpacing: 14,
              headingTextStyle: const TextStyle(
                color: AppColors.textMuted,
                fontSize: 11,
                fontWeight: FontWeight.w700,
              ),
              dataTextStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
              columns: const [
                DataColumn(label: Text('Players')),
                DataColumn(label: Text('Winners')),
                DataColumn(label: Text('₹10')),
                DataColumn(label: Text('₹50')),
                DataColumn(label: Text('₹100')),
              ],
              rows: PoolRules.structureRows
                  .map(
                    (r) => DataRow(
                      cells: [
                        DataCell(Text('${r.players}')),
                        DataCell(Text('${r.winners}')),
                        DataCell(Text(formatRupee(r.prize10))),
                        DataCell(Text(formatRupee(r.prize50))),
                        DataCell(Text(formatRupee(r.prize100))),
                      ],
                    ),
                  )
                  .toList(),
            ),
          ),
        ],
      ),
    );
  }
}

class _WinnerDistributionTable extends StatelessWidget {
  const _WinnerDistributionTable();

  @override
  Widget build(BuildContext context) {
    return GlassCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('🏅 Winner Distribution', style: TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 10),
          ...PoolRules.structureRows.map(
            (r) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Row(
                children: [
                  SizedBox(
                    width: 72,
                    child: Text('${r.players}P', style: const TextStyle(fontWeight: FontWeight.w700)),
                  ),
                  SizedBox(
                    width: 56,
                    child: Text('${r.winners}W', style: const TextStyle(color: AppColors.textSecondary)),
                  ),
                  Expanded(
                    child: Text(r.distributionLabel, style: const TextStyle(color: AppColors.gold, fontSize: 13)),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _NumberField extends StatelessWidget {
  const _NumberField({required this.label, required this.value, required this.onChanged});
  final String label;
  final String value;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextFormField(
        initialValue: value,
        key: ValueKey('$label-$value'),
        keyboardType: TextInputType.number,
        decoration: InputDecoration(labelText: label),
        onChanged: onChanged,
      ),
    );
  }
}
