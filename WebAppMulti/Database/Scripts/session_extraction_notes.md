# Session extraction — accumulated corrections

Read by `session_doc_agent.py` and appended to every extraction prompt. When a `run` produces
a mistake, add a short rule here describing the correct behavior — it applies to every future
run automatically, no code change needed.

- Dates must be exactly `YYYY-MM-DD` (e.g. `2026-07-13`), never a written-out format like
  "Jul 13, 2026" or "7/13/2026".
- Times must be exactly 24-hour `HH:MM` (e.g. `15:30`), never 12-hour with am/pm like "3:30 pm".
- Any source path whose value in the document is a list of multiple items (e.g.
  `session.participants`) must be extracted as a JSON array of strings, never a single
  comma-joined string.
- `service.modifier` must be ONLY the raw HCPCS modifier code (e.g. `HM`, `HO`) — never append
  a role/credential in parentheses like `HO (BCBA)`. If the document shows a role next to the
  modifier, put the modifier alone in `service.modifier` and the role in `service.type`.
