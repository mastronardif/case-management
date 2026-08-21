"""
ask_claude.py — thin wrapper around headless Claude Code ("claude -p") for use by other
scripts or standalone. Uses your existing Claude Code subscription auth — no API key.

The prompt is read from stdin rather than a command-line argument: multi-line prompts with
special characters get mangled going through the npm .cmd shim's argument re-parsing on
Windows when passed as argv, but stdin bypasses that entirely.

Lines starting with "#" are treated as comments and stripped before the prompt is sent —
handy for leaving yourself notes in a saved prompt file.

"{name}" placeholders in the prompt are substituted from --var name=value, so one saved
prompt file can be reused across different docs/folders instead of hand-editing it each time.

Usage (standalone):
    echo "Say hello" | python ask_claude.py
    python ask_claude.py < prompt.txt
    python ask_claude.py --prompt-file ask.claude.txt
    python ask_claude.py --prompt-file ask.claude.txt --var docNumber=1976 --allowed-tools "Read,Write" --permission-mode acceptEdits

Usage (from another script):
    from ask_claude import ask_claude
    answer = ask_claude(prompt_text, allowed_tools="Read")
"""

import argparse
import json
import re
import shutil
import subprocess
import sys


def ask_claude(prompt, allowed_tools="Read", permission_mode=None, cwd=None, timeout=300):
    # On Windows, "claude" is an npm-installed .cmd/.ps1 shim, not a plain executable —
    # subprocess needs the resolved path (with extension) to launch it without shell=True.
    claude_exe = shutil.which("claude.cmd") or shutil.which("claude") or "claude"
    cmd = [claude_exe, "-p", "--output-format", "json"]
    if allowed_tools:
        cmd += ["--allowedTools", allowed_tools]
    if permission_mode:
        cmd += ["--permission-mode", permission_mode]

    # Headless Claude Code scopes file access to its working directory — pass cwd explicitly
    # rather than relying on whatever directory the calling script happens to be run from.
    result = subprocess.run(cmd, input=prompt, capture_output=True, text=True, timeout=timeout, cwd=cwd)
    if result.returncode != 0:
        raise RuntimeError(f"claude -p failed (exit {result.returncode}):\n{result.stderr}")

    wrapper = json.loads(result.stdout)
    return wrapper.get("result", "")


def strip_comments(raw):
    return "\n".join(line for line in raw.splitlines() if not line.strip().startswith("#")).strip()


def substitute_vars(prompt, var_args):
    for entry in var_args or []:
        if "=" not in entry:
            sys.exit(f"--var must be name=value, got: {entry}")
        name, value = entry.split("=", 1)
        prompt = prompt.replace("{" + name + "}", value)
    # Any placeholder nobody supplied a value for is blanked rather than left as literal
    # "{name}" text in what gets sent to Claude — e.g. a shared template's {notes} section
    # when the caller has no corrections to include.
    prompt = re.sub(r"\{[a-zA-Z_][a-zA-Z0-9_]*\}", "", prompt)
    return re.sub(r"\n{3,}", "\n\n", prompt).strip()


def main():
    parser = argparse.ArgumentParser(description="Ask headless Claude Code a question (file/stdin prompt -> stdout answer)")
    parser.add_argument("--prompt-file", default=None, help="Read the prompt from this file instead of stdin")
    parser.add_argument("--var", action="append", metavar="name=value",
                         help="Substitute {name} in the prompt with value; repeatable")
    parser.add_argument("--allowed-tools", default="Read", help="Comma-separated tool names to allow (default: Read)")
    parser.add_argument("--permission-mode", default=None,
                         choices=["default", "acceptEdits", "plan", "auto", "dontAsk", "bypassPermissions"],
                         help="Forwarded to claude -p --permission-mode (default: unset, i.e. claude's own default)")
    parser.add_argument("--cwd", default=None, help="Working directory for claude -p (scopes its file access)")
    parser.add_argument("--timeout", type=int, default=300)
    args = parser.parse_args()

    if args.prompt_file:
        with open(args.prompt_file, "r", encoding="utf-8") as f:
            raw = f.read()
    else:
        raw = sys.stdin.read()

    prompt = substitute_vars(strip_comments(raw), args.var)
    if not prompt:
        sys.exit("No prompt provided")

    print(ask_claude(prompt, allowed_tools=args.allowed_tools, permission_mode=args.permission_mode,
                      cwd=args.cwd, timeout=args.timeout))


if __name__ == "__main__":
    main()
