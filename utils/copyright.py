#!/usr/bin/env python3
"""
Copyright Management Utility for .NET Projects
================================================
Adds, removes, or updates StyleCop copyright headers in .cs files.
Reads configuration from stylecop.json and provides both interactive
and command-line modes.

Usage:
    Interactive:     python copyright.py
    Command-line:    python copyright.py --add | --remove | --update
    Help:            python copyright.py --help
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

# ==============================================================================
# Configuration
# ==============================================================================

UTF8_BOM = "\ufeff"
COPYRIGHT_PATTERN = re.compile(r"<copyright\s+file=")
CS_EXTENSION = "*.cs"


# ==============================================================================
# Data classes
# ==============================================================================

@dataclass
class CopyrightConfig:
    """Parsed copyright settings from stylecop.json."""
    company_name: str
    copyright_text: str
    header_decoration: Optional[str] = None

    @property
    def resolved_text(self) -> str:
        """Copyright text with {companyName} placeholder substituted."""
        return self.copyright_text.replace("{companyName}", self.company_name)


@dataclass
class ProjectPaths:
    """Resolved project directory paths."""
    project_root: Path
    src_dir: Path
    stylecop_config: Path


# ==============================================================================
# Path resolution
# ==============================================================================

def resolve_paths() -> ProjectPaths:
    """Determine project root and source directories.

    The script lives in utils/, so project_root = script_parent / "..".
    """
    script_dir = Path(__file__).resolve().parent
    project_root = script_dir.parent
    src_dir = project_root / "src"
    stylecop_config = src_dir / "stylecop.json"
    return ProjectPaths(
        project_root=project_root,
        src_dir=src_dir,
        stylecop_config=stylecop_config,
    )


# ==============================================================================
# StyleCop config parser
# ==============================================================================

def parse_stylecop_config(config_path: Path) -> Optional[CopyrightConfig]:
    """Read and parse stylecop.json, returning CopyrightConfig or None."""
    if not config_path.is_file():
        return None

    try:
        with open(config_path, encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, OSError) as e:
        print_error(f"Failed to parse {config_path}: {e}")
        return None

    rules = data.get("settings", {}).get("documentationRules", {})
    company = rules.get("companyName")
    copyright_text = rules.get("copyrightText")

    if not company or not copyright_text:
        return None

    return CopyrightConfig(
        company_name=company,
        copyright_text=copyright_text,
        header_decoration=rules.get("headerDecoration"),
    )


# ==============================================================================
# Copyright header generation
# ==============================================================================

def generate_copyright_header(config: CopyrightConfig, filename: str) -> str:
    """Build the full copyright header string for a given file.

    Includes optional decoration lines if configured.
    """
    lines: list[str] = []

    if config.header_decoration:
        lines.append(f"// {config.header_decoration}")

    lines.append(f'// <copyright file="{filename}" company="{config.company_name}">')
    lines.append(f"// {config.resolved_text}")
    lines.append("// </copyright>")

    if config.header_decoration:
        lines.append(f"// {config.header_decoration}")

    return "\n".join(lines)


# ==============================================================================
# File operations
# ==============================================================================

def read_cs_file(filepath: Path) -> tuple[str, bool]:
    """Read a .cs file and return (content, has_bom) tuple."""
    raw = filepath.read_bytes()
    has_bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")  # strips BOM if present
    return text, has_bom


def write_cs_file(filepath: Path, content: str, has_bom: bool) -> None:
    """Write content to a .cs file, optionally preserving BOM."""
    if has_bom:
        filepath.write_bytes((UTF8_BOM + content).encode("utf-8"))
    else:
        filepath.write_text(content, encoding="utf-8")


def file_has_copyright(content: str) -> bool:
    """Check if the first 10 lines of a file contain a copyright header."""
    first_lines = content.splitlines()[:10]
    return any("<copyright file=" in line for line in first_lines)


def add_copyright_to_file(
    filepath: Path,
    config: CopyrightConfig,
) -> None:
    """Add a copyright header to a .cs file that doesn't have one."""
    content, has_bom = read_cs_file(filepath)
    header = generate_copyright_header(config, filepath.name)
    new_content = f"{header}\n\n{content}"
    write_cs_file(filepath, new_content, has_bom)


# Regex to match the full copyright block including decoration lines
# and trailing blank lines.  Works on the raw text (BOM already stripped).
_COPYRIGHT_BLOCK_RE = re.compile(
    r"(?:"                          # optional leading decoration
        r"//\s*-+\s*\n"
    r")?"
    r"//\s*<copyright\s+file=.*?\n"  # <copyright ...>
    r".*?\n"                        #   copyright line
    r"//\s*</copyright>\s*\n"       # </copyright>
    r"(?:"                          # optional trailing decoration
        r"//\s*-+\s*\n"
    r")?"
    r"\n?"                          # optional blank line after block
    r"",
    re.DOTALL,
)


def remove_copyright_from_file(filepath: Path) -> None:
    """Remove the StyleCop copyright header from a .cs file."""
    content, has_bom = read_cs_file(filepath)

    if not file_has_copyright(content):
        return

    new_content = _COPYRIGHT_BLOCK_RE.sub("", content, count=1)

    # Strip leading blank lines that may remain after removal
    new_content = new_content.lstrip("\n")

    write_cs_file(filepath, new_content, has_bom)


def update_copyright_in_file(
    filepath: Path,
    config: CopyrightConfig,
) -> None:
    """Remove old copyright and add a fresh one from config."""
    remove_copyright_from_file(filepath)
    add_copyright_to_file(filepath, config)


# ==============================================================================
# Batch operations
# ==============================================================================

def find_cs_files(src_dir: Path) -> list[Path]:
    """Return a sorted list of all .cs files under src_dir."""
    return sorted(src_dir.rglob(CS_EXTENSION))


def batch_add(config: CopyrightConfig, paths: ProjectPaths) -> None:
    """Add copyright to all .cs files missing it."""
    files = find_cs_files(paths.src_dir)
    added = skipped = 0

    print_header("Adding Copyright Headers")

    for f in files:
        rel = f.relative_to(paths.project_root)
        if file_has_copyright(read_cs_file(f)[0]):
            print_warning(f"Already has copyright: {rel}")
            skipped += 1
        else:
            add_copyright_to_file(f, config)
            print_success(f"Added: {rel}")
            added += 1

    _print_summary(len(files), added, skipped)


def batch_remove(paths: ProjectPaths) -> None:
    """Remove copyright from all .cs files that have it."""
    files = find_cs_files(paths.src_dir)
    removed = skipped = 0

    print_header("Removing Copyright Headers")

    for f in files:
        rel = f.relative_to(paths.project_root)
        content, _ = read_cs_file(f)
        if file_has_copyright(content):
            remove_copyright_from_file(f)
            print_success(f"Removed: {rel}")
            removed += 1
        else:
            print_warning(f"No copyright to remove: {rel}")
            skipped += 1

    _print_summary(len(files), removed, skipped)


def batch_update(config: CopyrightConfig, paths: ProjectPaths) -> None:
    """Update or add copyright in all .cs files."""
    files = find_cs_files(paths.src_dir)
    updated = 0

    print_header("Updating Copyright Headers")

    for f in files:
        rel = f.relative_to(paths.project_root)
        content, _ = read_cs_file(f)
        if file_has_copyright(content):
            update_copyright_in_file(f, config)
        else:
            add_copyright_to_file(f, config)
        print_success(f"Updated: {rel}")
        updated += 1

    _print_summary(len(files), updated, 0)


def _print_summary(total: int, succeeded: int, skipped: int) -> None:
    """Print a summary footer after a batch operation."""
    print()
    print_info(f"Total files scanned: {total}")
    print_success(f"Succeeded: {succeeded}")
    if skipped:
        print_warning(f"Skipped: {skipped}")


# ==============================================================================
# Interactive menu
# ==============================================================================

def interactive_menu(config: CopyrightConfig, paths: ProjectPaths) -> None:
    """Display the interactive menu and execute the chosen action."""
    files = find_cs_files(paths.src_dir)
    with_count = sum(
        1 for f in files if file_has_copyright(read_cs_file(f)[0])
    )
    total = len(files)
    without_count = total - with_count

    print_header("Copyright Management Utility")

    print(f"  Project:          {paths.project_root}")
    print(f"  Source directory: {paths.src_dir}")
    print()

    print_success("✔ Copyright configuration detected:")
    print(f"      Company:     {config.company_name}")
    print(f"      Format:      {config.resolved_text}")
    if config.header_decoration:
        print(f"      Decoration:  {config.header_decoration}")
    print()

    print("  Current status:")
    print(f"      Total .cs files:   {total}")
    print(f"      With copyright:    {with_count}")
    print(f"      Without copyright: {without_count}")
    print()

    # Build dynamic options
    options: list[tuple[str, str]] = []  # (label, action_key)

    if with_count > 0:
        options.append(("Update copyright headers (refresh from stylecop.json)", "update"))
        options.append(("Remove all copyright headers", "remove"))

    if without_count > 0:
        options.append(("Add copyright headers to files without them", "add"))

    options.append(("Exit", "exit"))

    print("  Available actions:")
    for i, (label, _) in enumerate(options, 1):
        print(f"      {i}) {label}")
    print()

    try:
        choice = input(f"  Select action (1-{len(options)}): ").strip()
    except (EOFError, KeyboardInterrupt):
        print()
        print_info("Exiting. Goodbye!")
        sys.exit(0)

    if not choice.isdigit() or not (1 <= int(choice) <= len(options)):
        print_error("Invalid choice")
        sys.exit(1)

    action = options[int(choice) - 1][1]

    actions = {
        "add": lambda: batch_add(config, paths),
        "remove": lambda: batch_remove(paths),
        "update": lambda: batch_update(config, paths),
        "exit": lambda: print_info("Exiting. Goodbye!") or sys.exit(0),
    }

    actions[action]()


# ==============================================================================
# Terminal output helpers
# ==============================================================================

class _Style:
    """ANSI colour codes for terminal output."""
    RED = "\033[0;31m"
    GREEN = "\033[0;32m"
    YELLOW = "\033[1;33m"
    CYAN = "\033[0;36m"
    BOLD = "\033[1m"
    RESET = "\033[0m"


def _supports_color() -> bool:
    """Check if the terminal supports ANSI colours."""
    if not hasattr(sys.stdout, "isatty"):
        return False
    if not sys.stdout.isatty():
        return False
    if os.environ.get("NO_COLOR"):
        return False
    return True


_USE_COLOR = _supports_color()


def _colorize(text: str, color: str) -> str:
    if not _USE_COLOR:
        return text
    return f"{color}{text}{_Style.RESET}"


def print_info(msg: str) -> None:
    print(f"[{_colorize('INFO', _Style.CYAN)}] {msg}")


def print_success(msg: str) -> None:
    print(f"[{_colorize('SUCCESS', _Style.GREEN)}] {msg}")


def print_warning(msg: str) -> None:
    print(f"[{_colorize('WARNING', _Style.YELLOW)}] {msg}")


def print_error(msg: str) -> None:
    print(f"[{_colorize('ERROR', _Style.RED)}] {msg}", file=sys.stderr)


def print_header(title: str) -> None:
    separator = "=" * 40
    bold = _colorize(separator, _Style.BOLD)
    bold_title = _colorize(f"  {title}", _Style.BOLD)
    print(f"\n{bold}\n{bold_title}\n{bold}\n")


# ==============================================================================
# CLI argument parser
# ==============================================================================

def build_argument_parser() -> argparse.ArgumentParser:
    """Create the argparse.ArgumentParser for CLI mode."""
    parser = argparse.ArgumentParser(
        description="Copyright management utility for .cs files",
    )
    parser.add_argument(
        "--add",
        action="store_true",
        help="Add copyright headers to files without them",
    )
    parser.add_argument(
        "--remove",
        action="store_true",
        help="Remove all copyright headers",
    )
    parser.add_argument(
        "--update",
        action="store_true",
        help="Update/add copyright headers in all files",
    )
    return parser


# ==============================================================================
# Main
# ==============================================================================

def main() -> None:
    """Entry point for both CLI and interactive modes."""
    paths = resolve_paths()

    # Parse CLI arguments (non-interactive mode detection)
    parser = build_argument_parser()
    args = parser.parse_args()

    cli_mode = args.add or args.remove or args.update

    # Parse stylecop configuration
    config = parse_stylecop_config(paths.stylecop_config)

    if cli_mode and config is None:
        print_error(
            f"Copyright configuration not found at {paths.stylecop_config}. "
            "Add stylecop.json to continue."
        )
        sys.exit(1)

    if cli_mode:
        # --- Command-line mode ---
        if args.add:
            batch_add(config, paths)
        elif args.remove:
            batch_remove(paths)
        elif args.update:
            batch_update(config, paths)
    else:
        # --- Interactive mode ---
        if config is None:
            print_error(
                f"Copyright configuration not found at {paths.stylecop_config}. "
                "Please add it to continue."
            )
            sys.exit(1)
        interactive_menu(config, paths)


if __name__ == "__main__":
    main()
