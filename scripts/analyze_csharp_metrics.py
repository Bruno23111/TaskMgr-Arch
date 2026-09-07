#!/usr/bin/env python3
"""Gera métricas reprodutíveis de código C# para comparação arquitetural."""

from __future__ import annotations

import argparse
import csv
import json
import math
import os
import re
import subprocess
from importlib.metadata import version
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

import lizard

COMMENT_PATTERN = re.compile(r"/\*.*?\*/|//[^\r\n]*", re.DOTALL)
STRING_PATTERN = re.compile(
    r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'', re.DOTALL
)
TOKEN_PATTERN = re.compile(
    r"\b[A-Za-z_][A-Za-z0-9_]*\b|\b\d+(?:\.\d+)?\b|"
    r"==|!=|<=|>=|&&|\|\||\+\+|--|\?\?|=>|\+=|-=|\*=|/=|[+*/%<>=!&|?:~-]"
)
KEYWORDS = {
    "if", "else", "switch", "case", "for", "foreach", "while", "do", "catch",
    "return", "throw", "new", "class", "interface", "public", "private", "protected",
    "internal", "static", "async", "await", "using", "namespace", "var", "null", "true",
    "false", "this", "base", "in", "is", "as", "default", "try", "finally",
}
OPERATORS = {
    "==", "!=", "<=", ">=", "&&", "||", "++", "--", "??", "=>", "+=", "-=", "*=",
    "/=", "+", "*", "/", "%", "<", ">", "=", "!", "&", "|", "?", ":", "~",
}


def parse_source(value: str) -> tuple[str, Path]:
    if "=" not in value:
        raise argparse.ArgumentTypeError("A origem deve seguir o formato ARQUITETURA=caminho")
    architecture, raw_path = value.split("=", 1)
    if not architecture or not raw_path:
        raise argparse.ArgumentTypeError("A origem deve conter arquitetura e caminho")
    return architecture.upper(), Path(raw_path)


def source_files(root: Path) -> list[Path]:
    return sorted(
        path for path in root.rglob("*.cs")
        if "bin" not in path.parts and "obj" not in path.parts
    )


def source_without_comments_and_strings(text: str) -> str:
    without_strings = STRING_PATTERN.sub('"texto"', text)
    return COMMENT_PATTERN.sub(" ", without_strings)


def physical_nloc(text: str) -> int:
    cleaned = source_without_comments_and_strings(text)
    return sum(1 for line in cleaned.splitlines() if line.strip())


def halstead_volume(text: str) -> float:
    tokens = TOKEN_PATTERN.findall(source_without_comments_and_strings(text))
    operators = [token for token in tokens if token in OPERATORS or token in KEYWORDS]
    operands = [token for token in tokens if token not in OPERATORS and token not in KEYWORDS]
    vocabulary = len(set(operators)) + len(set(operands))
    length = len(operators) + len(operands)
    return length * math.log2(vocabulary) if vocabulary > 1 else 0.0


def maintainability_index(volume: float, cyclomatic_complexity: int, nloc: int) -> float:
    """Índice de manutenibilidade normalizado (0-100), sem comentário bônus."""
    if nloc <= 0:
        return 100.0
    raw = 171 - 5.2 * math.log(max(volume, 1.0)) - 0.23 * cyclomatic_complexity - 16.2 * math.log(nloc)
    return round(max(0.0, raw * 100 / 171), 2)


def git_history(repository: Path, output: Path) -> None:
    command = [
        "git", "log", "--all", "--date=iso-strict",
        "--pretty=format:%H%x1f%aI%x1f%an%x1f%s",
    ]
    result = subprocess.run(command, cwd=repository, capture_output=True, text=True, check=False)
    with output.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["commit", "author_date", "author", "subject"])
        if result.returncode == 0:
            for line in result.stdout.splitlines():
                writer.writerow(line.split("\x1f"))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", action="append", type=parse_source, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--repository", type=Path, default=Path.cwd())
    arguments = parser.parse_args()

    output = arguments.output
    output.mkdir(parents=True, exist_ok=True)
    file_rows: list[dict[str, object]] = []
    method_rows: list[dict[str, object]] = []

    for architecture, root in arguments.source:
        for path in source_files(root):
            text = path.read_text(encoding="utf-8-sig")
            analysis = lizard.analyze_file(str(path))
            nloc = physical_nloc(text)
            ccn = sum(function.cyclomatic_complexity for function in analysis.function_list)
            volume = halstead_volume(text)
            file_rows.append({
                "architecture": architecture,
                "file": path.as_posix(),
                "nloc": nloc,
                "cyclomatic_complexity": ccn,
                "function_count": len(analysis.function_list),
                "halstead_volume_approximation": round(volume, 2),
                "maintainability_index": maintainability_index(volume, ccn, nloc),
            })
            for function in analysis.function_list:
                method_rows.append({
                    "architecture": architecture,
                    "file": path.as_posix(),
                    "method": function.long_name,
                    "start_line": function.start_line,
                    "end_line": function.end_line,
                    "nloc": function.nloc,
                    "cyclomatic_complexity": function.cyclomatic_complexity,
                    "parameter_count": function.parameter_count,
                    "token_count": function.token_count,
                })

    def write_csv(filename: str, rows: list[dict[str, object]]) -> None:
        fields = list(rows[0]) if rows else []
        with (output / filename).open("w", newline="", encoding="utf-8") as stream:
            writer = csv.DictWriter(stream, fieldnames=fields)
            if fields:
                writer.writeheader()
                writer.writerows(rows)

    write_csv("csharp-file-metrics.csv", file_rows)
    write_csv("csharp-method-metrics.csv", method_rows)
    git_history(arguments.repository, output / "git-history.csv")

    grouped: dict[str, list[dict[str, object]]] = {}
    for row in file_rows:
        grouped.setdefault(str(row["architecture"]), []).append(row)
    summary = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "build_id": os.getenv("BUILD_BUILDID"),
        "source_version": os.getenv("BUILD_SOURCEVERSION"),
        "source_branch": os.getenv("BUILD_SOURCEBRANCH"),
        "tool": {"name": "lizard", "version": version("lizard")},
        "maintainability_index": {
            "formula": "max(0, (171 - 5.2*ln(V) - 0.23*CC - 16.2*ln(LOC))*100/171)",
            "halstead_volume": "Aproximação léxica documentada; não comparar com ferramentas que usem outra variante.",
        },
        "architectures": {
            architecture: {
                "file_count": len(rows),
                "nloc": sum(int(row["nloc"]) for row in rows),
                "cyclomatic_complexity": sum(int(row["cyclomatic_complexity"]) for row in rows),
                "function_count": sum(int(row["function_count"]) for row in rows),
                "weighted_maintainability_index": round(
                    sum(float(row["maintainability_index"]) * max(int(row["nloc"]), 1) for row in rows)
                    / max(sum(max(int(row["nloc"]), 1) for row in rows), 1),
                    2,
                ),
            }
            for architecture, rows in grouped.items()
        },
    }
    (output / "metrics-summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
