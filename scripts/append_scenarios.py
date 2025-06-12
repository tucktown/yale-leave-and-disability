#!/usr/bin/env python3
"""
append_scenarios.py
-------------------
Reads scenarios from a source JSON file, transforms them to the target schema,
and appends them to the main scenarios configuration file.

This script handles the structural differences and name mappings between the
two formats.

Usage
-----
$ python append_scenarios.py --source new_scenarios.json --target ESLFeeder/Config/scenarios.json
"""

import argparse
import json
import re
from pathlib import Path

# ----- Configuration ---------------------------------------------------------

# Maps field names from the source file to the target file
FIELD_NAME_MAP = {
    "STD_HRS": "STD_HOURS",
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

# Maps variable names from source formulas to target (C#) variable names
VARIABLE_NAME_MAP = {
    "SCHED_HRS": "ScheduledHours",
    "STD_OR_NOT": "StdOrNot",
    "PAY_RATE": "PayRate",
    "PTO_USE_HRS": "PtoUseHrs",
    "PTO_USABLE": "PtoUsable",
    "BASIC_SICK_AVAIL_CALC": "BasicSickAvailCalc",
    "PTO_SUPP_HRS": "PtoSuppHrs",
    "PTO_BASIC_SICK_STD_CTPL": "PtoBasicSickStdCtpl",
    "BASIC_SICK_STD_CTPL": "BasicSickStdCtpl",
    "PTO_BASIC_SICK_STD": "PtoBasicSickStd",
}

# Variables that should be treated as string types in the final JSON
STRING_VARS = ["PTO_USABLE", "BASIC_SICK_AVAIL_CALC"]


# ----- Transformation Helpers ------------------------------------------------

def parse_calculation(value_str):
    """
    Parses a simple calculation string like "A * B" or "(A / B)"
    into a structured calculation object. Returns None if no operator found.
    """
    if not isinstance(value_str, str):
        return None

    # Pattern for operand1 (op) operand2
    match = re.search(r"([\w\.\_]+)\s*([\*\/])\s*([\w\.\_]+)", value_str)
    if match:
        op1_str, op, op2_str = match.groups()
        
        operands = []
        for op_str in [op1_str, op2_str]:
            if op_str in VARIABLE_NAME_MAP:
                operands.append({"variable": VARIABLE_NAME_MAP[op_str]})
            else:
                try:
                    operands.append({"constant": float(op_str)})
                except ValueError:
                    # Fallback for unknown variables
                    operands.append({"variable": op_str})

        return {
            "operation": "multiply" if op == "*" else "divide",
            "operands": operands,
        }
    
    # Pattern for (operand1 / operand2)
    match = re.search(r"\(\s*([\w\.\_]+)\s*\/\s*([\w\.\_]+)\s*\)", value_str)
    if match:
        op1_str, op2_str = match.groups()
        operands = []
        for op_str in [op1_str, op2_str]:
            if op_str in VARIABLE_NAME_MAP:
                operands.append({"variable": VARIABLE_NAME_MAP[op_str]})
            else:
                operands.append({"variable": op_str}) # Assume variable if not in map
        
        return {
            "operation": "divide",
            "operands": operands
        }
        
    return None


def transform_field(value):
    """
    Transforms a single field value from the source format to the target format.
    Returns the transformed field object and a list of required variables.
    """
    field_obj = {}
    required_vars = []

    # --- Determine Type and Nullability ---
    if value is None:
        field_obj["type"] = "string"
        field_obj["allow_null"] = True
        field_obj["source"] = "null"
    elif isinstance(value, (int, float)):
        field_obj["type"] = "double"
        field_obj["source"] = str(value)
    elif isinstance(value, str):
        if value == "CURRENT_DATE":
            field_obj["type"] = "date"
            field_obj["source"] = value
        else:
            # Detect variables in the string
            source_vars = re.findall(r"([A-Z_]+)", value)
            source_str = value

            for var in source_vars:
                if var in VARIABLE_NAME_MAP:
                    req_var = VARIABLE_NAME_MAP[var]
                    if req_var not in required_vars:
                        required_vars.append(req_var)
                    # Replace with `variables.` prefix for the source string
                    source_str = source_str.replace(var, f"variables.{req_var}")

            field_obj["source"] = source_str
            field_obj["type"] = "string" if source_vars and source_vars[0] in STRING_VARS else "double"

            # --- Add Calculation Object ---
            calculation = parse_calculation(value)
            if calculation:
                field_obj["calculation"] = calculation
    else:
         # Default for unknown types
        field_obj["type"] = "string"
        field_obj["source"] = str(value)

    return field_obj, required_vars


def transform_scenario(source_scenario):
    """Converts a single scenario object to the target schema."""
    target_scenario = {
        key: value for key, value in source_scenario.items()
        if key not in ["fields"]
    }

    all_required_vars = []
    updates = {"order": [], "fields": {}}

    source_fields = source_scenario.get("fields", {})
    for source_key, source_value in source_fields.items():
        target_key = FIELD_NAME_MAP.get(source_key, source_key)
        updates["order"].append(target_key)

        field_obj, required_vars = transform_field(source_value)
        updates["fields"][target_key] = field_obj

        for var in required_vars:
            if var not in all_required_vars:
                all_required_vars.append(var)

    target_scenario["updates"] = updates
    target_scenario["variables_required"] = all_required_vars
    target_scenario["logging"] = {"add_message": "", "update_message": ""}

    return target_scenario


# ----- CLI -------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Transform and append scenarios.")
    ap.add_argument("-s", "--source", required=True, help="Path to source JSON file with new scenarios")
    ap.add_argument("-t", "--target", required=True, help="Path to the existing scenarios.json to check for duplicate IDs")
    ap.add_argument("-o", "--output", required=True, help="Path to write the new, transformed scenarios")
    args = ap.parse_args()

    source_path = Path(args.source)
    target_path = Path(args.target)
    output_path = Path(args.output)

    if not source_path.exists():
        print(f"Error: Source file not found at {source_path}")
        return
    if not target_path.exists():
        print(f"Error: Target file for duplicate check not found at {target_path}")
        return

    # Load source and target files
    source_scenarios = json.loads(source_path.read_text())
    target_data = json.loads(target_path.read_text())

    existing_scenarios = target_data.get("scenarios", [])
    existing_ids = {s["id"] for s in existing_scenarios}

    scenarios_to_load = []
    for source_scenario in source_scenarios:
        if source_scenario["id"] in existing_ids:
            print(f"Warning: Scenario ID {source_scenario['id']} already exists in target. Skipping.")
            continue

        transformed = transform_scenario(source_scenario)
        scenarios_to_load.append(transformed)

    # Write the list of new scenarios to the output file
    output_path.write_text(json.dumps(scenarios_to_load, indent=2))

    print(f"Successfully transformed {len(scenarios_to_load)} new scenarios to {output_path.resolve()}")


if __name__ == "__main__":
    main() 