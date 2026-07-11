#!/usr/bin/env python3
"""One-off script: convert the IEEE MA-L OUI registry CSV into a compact TSV asset.

Source: https://standards-oui.ieee.org/oui/oui.csv (download manually, no runtime fetching).
Usage: python generate_oui_asset.py <path-to-oui.csv> <output-tsv-path>
"""
import csv
import sys


def convert(input_path: str, output_path: str) -> None:
    entries = {}
    with open(input_path, encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            prefix = row["Assignment"].strip().upper()
            vendor = row["Organization Name"].strip()
            if len(prefix) == 6 and vendor:
                entries[prefix] = vendor
    with open(output_path, "w", encoding="utf-8", newline="\n") as f:
        for prefix in sorted(entries):
            f.write(f"{prefix}\t{entries[prefix]}\n")
    print(f"Wrote {len(entries)} entries to {output_path}")


if __name__ == "__main__":
    convert(sys.argv[1], sys.argv[2])
