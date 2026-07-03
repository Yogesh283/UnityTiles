using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mkey.NakamaPoc
{
    /// <summary>
    /// PASS/FAIL tracker for Phase 1.5 Nakama POC.
    /// </summary>
    public sealed class NakamaPocReport
    {
        private readonly List<(int step, string name, bool pass, string detail)> _rows = new();

        public void Step(int step, string name, bool pass, string detail = null)
        {
            _rows.Add((step, name, pass, detail));
            string verdict = pass ? "PASS" : "FAIL";
            string msg = string.IsNullOrEmpty(detail)
                ? $"{NakamaPocSettings.Tag} STEP {step} {name} => {verdict}"
                : $"{NakamaPocSettings.Tag} STEP {step} {name} => {verdict} | {detail}";
            if (pass)
                Debug.Log(msg);
            else
                Debug.LogWarning(msg);
        }

        public void Fail(string name, string detail)
        {
            Step(0, name, false, detail);
        }

        public bool AllPassed
        {
            get
            {
                if (_rows.Count == 0)
                    return false;
                foreach (var row in _rows)
                {
                    if (!row.pass)
                        return false;
                }
                return true;
            }
        }

        public void PrintSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{NakamaPocSettings.Tag} === PHASE 1.5 REPORT ===");
            foreach (var row in _rows)
            {
                sb.AppendLine($"  [{row.step}] {row.name}: {(row.pass ? "PASS" : "FAIL")}" +
                              (string.IsNullOrEmpty(row.detail) ? "" : $" ({row.detail})"));
            }

            bool overall = AllPassed;
            sb.AppendLine($"{NakamaPocSettings.Tag} === OVERALL PHASE 1.5: {(overall ? "PASS" : "FAIL")} ===");
            if (overall)
                Debug.Log(sb.ToString());
            else
                Debug.LogWarning(sb.ToString());
        }
    }
}
