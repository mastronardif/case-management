"""
Summarizes claude_usage_log.jsonl — the running record of every headless `claude -p` call
made through ask_claude.py (by session_doc_agent.py or any future script that reuses it).

Usage:
    python claude_usage_report.py
    python claude_usage_report.py --since 2026-08-01
    python claude_usage_report.py --by caller
"""

import argparse
import json
import os
from collections import defaultdict
from datetime import datetime, timezone

LOG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "claude_usage_log.jsonl")


def load_records(since=None):
    if not os.path.exists(LOG_FILE):
        return []

    records = []
    with open(LOG_FILE, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                rec = json.loads(line)
            except json.JSONDecodeError:
                continue
            if since:
                ts = datetime.fromisoformat(rec["timestamp"])
                if ts < since:
                    continue
            records.append(rec)
    return records


def group_sum(records, key_fn, label):
    totals = defaultdict(lambda: {"calls": 0, "cost": 0.0})
    for rec in records:
        key = key_fn(rec)
        cost = rec.get("totalCostUsd") or 0.0
        totals[key]["calls"] += 1
        totals[key]["cost"] += cost

    print(f"\n=== By {label} ===")
    print(f"{label:<30} {'calls':>8} {'cost':>12}")
    for key, agg in sorted(totals.items(), key=lambda kv: -kv[1]["cost"]):
        print(f"{str(key):<30} {agg['calls']:>8} {agg['cost']:>12.4f}")


def main():
    parser = argparse.ArgumentParser(description="Summarize claude -p usage logged by ask_claude.py")
    parser.add_argument("--since", help="Only include calls on/after this date (YYYY-MM-DD)")
    parser.add_argument("--by", choices=["caller", "day", "both"], default="both",
                         help="Breakdown grouping (default: both)")
    args = parser.parse_args()

    since = None
    if args.since:
        since = datetime.strptime(args.since, "%Y-%m-%d").replace(tzinfo=timezone.utc)

    records = load_records(since)
    if not records:
        print(f"No usage records found in {LOG_FILE}"
              + (f" since {args.since}" if args.since else ""))
        return

    total_cost = sum(rec.get("totalCostUsd") or 0.0 for rec in records)
    total_calls = len(records)
    errors = sum(1 for rec in records if rec.get("isError"))
    first_ts = min(rec["timestamp"] for rec in records)
    last_ts = max(rec["timestamp"] for rec in records)

    print("=== claude -p usage summary ===")
    print(f"Log file : {LOG_FILE}")
    print(f"Range    : {first_ts} .. {last_ts}")
    print(f"Calls    : {total_calls}  ({errors} error(s))")
    print(f"Total    : ${total_cost:.4f}")
    print(f"Avg/call : ${(total_cost / total_calls):.4f}")

    if args.by in ("caller", "both"):
        group_sum(records, lambda r: r.get("caller") or "(unknown)", "caller")

    if args.by in ("day", "both"):
        group_sum(records, lambda r: r["timestamp"][:10], "day")


if __name__ == "__main__":
    main()
