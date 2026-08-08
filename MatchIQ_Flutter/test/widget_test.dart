import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:match_iq/app.dart';

void main() {
  testWidgets('MATCH IQ app boots', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: MatchIqApp()));
    await tester.pump();
    expect(find.textContaining('MATCH'), findsWidgets);
  });
}
