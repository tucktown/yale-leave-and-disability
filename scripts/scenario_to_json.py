#!/usr/bin/env python3
"""
xlsx2scenario_json.py
---------------------
Convert "scenario matrix" workbooks (like ESL Scenario_2025-06-12_input.xlsx)
into a structured JSON file – one object per scenario column.

Usage
-----
$ python scenario_to_json.py -i ESL_Scenario.xlsx -o scenarios.json
"""

import argparse
import json
import math
import re
from pathlib import Path

import numpy as np
import pandas as pd


# ----- Configuration ---------------------------------------------------------

# column-1 labels in the sheet  →  keys you want in the final JSON
FIELDS_MAP = {
    "STD_HRS": "STD_HRS",
    "PTO_HRS": "PTO_HRS",
    "LOA_NO_HRS_PAID": "LOA_NO_HRS_PAID",
    "BASIC_SICK_HRS": "BASIC_SICK_HRS",
    "BRIDGEPORT_SICK_HRS": "BRIDGEPORT_SICK_HRS",
    "LM_PTO_HRS": "LM_PTO_HRS",
    "LM_SICK_HRS": "LM_SICK_HRS",
    "ATO_HRS": "ATO_HRS",
    "EXEMPT_HRS": "EXEMPT_HRS",
    "EXEC_NOTE": "EXEC_NOTE",
    "PHYS_NOTE": "PHYS_NOTE",
    "MANUAL_CHECK": "MANUAL_CHECK",
    "ENTRY_DATE": "ENTRY_DATE",
    "AUTH_BY": "AUTH_BY",
    "CHECK_KRONOS": "CHECK_KRONOS",
}


# ----- Helpers ---------------------------------------------------------------

def normalize_field_value(val):
    """Convert workbook cell contents → JSON-friendly value."""
    # Convert pandas NA / NaN to Python None
    if val is None or (isinstance(val, float) and math.isnan(val)):
        return None

    # Convert False → 0  (per latest requirement)
    if isinstance(val, (bool, np.bool_)):
        return 0 if not val else val

    # Preserve 0 numerically
    if isinstance(val, (int, float)) and val == 0:
        return 0

    return val


def split_process_levels(raw):
    """'500, 900' → [500, 900]   (keeps strings if they can't become ints)."""
    levels = [p.strip() for p in str(raw).split(",") if p.strip()]
    result = []
    for lv in levels:
        try:
            result.append(int(lv))
        except ValueError:
            result.append(lv)
    return result


# ----- Core extraction -------------------------------------------------------

def extract_scenarios(df):
    """Return a list of scenario dictionaries from the dataframe."""
    scenarios = []

    # Identify rows that hold condition IDs (column 0 values look like "C6", "C21", …)
    cond_rows = [idx for idx, val in enumerate(df.iloc[:, 0])
                 if isinstance(val, str) and re.match(r"^C\d+", val)]

    # Map "field label row → DataFrame row index"
    field_row_idx = {label: idx for idx, label in enumerate(df.iloc[:, 1])
                     if isinstance(label, str) and label.strip() in FIELDS_MAP}

    # Iterate over every numeric "scenario" column (row 0 holds the ID)
    for col in range(df.shape[1]):
        scenario_id = df.iat[0, col]
        if not isinstance(scenario_id, (int, float)) or math.isnan(scenario_id):
            continue  # skip non-scenario columns
        scenario_id = int(scenario_id)

        # --- header data ---
        name         = df.iat[1, col]
        description  = df.iat[2, col]
        proc_levels  = split_process_levels(df.iat[3, col])
        reason_code  = df.iat[4, col]

        # --- conditions ---
        required, forbidden = [], []
        for r in cond_rows:
            cond_id  = df.iat[r, 0].strip()
            cell_val = df.iat[r, col]

            if isinstance(cell_val, (bool, np.bool_)):
                (required if cell_val else forbidden).append(cond_id)
            elif isinstance(cell_val, (int, float)) and not math.isnan(cell_val):
                # treat 0 as FALSE, anything else as TRUE
                (required if cell_val else forbidden).append(cond_id)
            elif isinstance(cell_val, str):
                val_norm = cell_val.strip().lower()
                if val_norm == "true":
                    required.append(cond_id)
                elif val_norm == "false":
                    forbidden.append(cond_id)
            # blanks are ignored

        # --- fields ---
        fields_out = {}
        for sheet_label, json_key in FIELDS_MAP.items():
            row_idx = field_row_idx.get(sheet_label)
            if row_idx is None:
                continue  # safety
            val = normalize_field_value(df.iat[row_idx, col])
            fields_out[json_key] = val

        scenarios.append({
            "id":            scenario_id,
            "name":          name,
            "description":   description,
            "process_levels": proc_levels,
            "reason_code":   reason_code,
            "is_skip_scenario": False,
            "conditions": {
                "forbidden": forbidden,
                "required":  required,
            },
            "fields": fields_out,
        })

    # Keep scenarios sorted by ID (optional)
    scenarios.sort(key=lambda s: s["id"])
    return scenarios


# ----- CLI -------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Convert ESL scenario matrix → JSON.")
    ap.add_argument("-i", "--input",  required=True, help="Path to XLSX file")
    ap.add_argument("-o", "--output", required=True, help="Path to write JSON")
    ap.add_argument("-s", "--sheet",  default=0,
                    help='Sheet name or index (default: first sheet)')
    args = ap.parse_args()

    # Load the sheet exactly as it appears – no header rows from pandas
    df = pd.read_excel(args.input, sheet_name=args.sheet, header=None)

    scenarios = extract_scenarios(df)

    # Pretty-print JSON
    out_path = Path(args.output)
    out_path.write_text(json.dumps(scenarios, indent=2))
    print(f"Wrote {len(scenarios)} scenarios → {out_path.resolve()}")


if __name__ == "__main__":
    main()
