#!/usr/bin/env python3
"""
ESL Scenarios Update Tool

This script updates the scenarios.json file based on data from the ESLScenarios.json file.
"""
import argparse
import json
import os
import sys
from datetime import datetime
from typing import Dict, List, Any, Optional
from difflib import unified_diff
import re
import copy

def format_json_diff(old_json: Dict, new_json: Dict) -> str:
    """Format a diff between two JSON objects with color coding"""
    old_str = json.dumps(old_json, indent=2).splitlines()
    new_str = json.dumps(new_json, indent=2).splitlines()
    diff = unified_diff(old_str, new_str, fromfile='Current', tofile='Proposed', n=0)
    
    # Color code the diff output
    colored_diff = []
    for line in diff:
        if line.startswith('+'):
            colored_diff.append(f"\033[92m{line}\033[0m")  # Green for additions
        elif line.startswith('-'):
            colored_diff.append(f"\033[91m{line}\033[0m")  # Red for removals
        else:
            colored_diff.append(line)
    
    return '\n'.join(colored_diff)

def review_changes(target_json: Dict, scenario_map: Dict) -> None:
    """Show a diff of changes for each scenario"""
    print("\033[92mReview Changes:\033[0m")
    print("\033[92m---------------\033[0m")
    
    for scenario in target_json.get('scenarios', []):
        scenario_id = scenario.get('id')
        if scenario_id in scenario_map:
            source_scenario = scenario_map[scenario_id]
            print(f"\033[96mScenario #{scenario_id}: {scenario.get('name', '')}\033[0m")
            
            # Create a copy of the current scenario for comparison
            current_scenario = scenario.copy()
            
            # Update the fields as we would in the real update
            current_scenario.update({
                'name': source_scenario['name'],
                'description': source_scenario['description'],
                'reason_code': source_scenario['reason_code'],
                'process_levels': source_scenario['process_levels'],
                'conditions': source_scenario['conditions'],
                'variables_required': source_scenario['variables_required'],
                'logging': source_scenario['logging'],
                'updates': source_scenario['updates']
            })
            
            # Show the diff
            diff = format_json_diff(scenario, current_scenario)
            if diff:
                print(diff)
                print("\033[93mPress Enter to continue, 'q' to quit review...\033[0m")
                response = input()
                if response.lower() == 'q':
                    break
            else:
                print("\033[92mNo changes\033[0m")
            print("\033[92m" + "="*80 + "\033[0m")

def extract_variables_from_expression(expr):
    """Extract variable names from an expression string."""
    if not isinstance(expr, str):
        return []
    
    variables = []
    # Look for variables.VariableName pattern (common in source expressions)
    if 'variables.' in expr:
        parts = expr.split('variables.')
        for part in parts[1:]:  # Skip first part as it's before any variables
            var_end = re.search(r'[^a-zA-Z0-9_]', part)
            if var_end:
                var_name = part[:var_end.start()]
            else:
                var_name = part
            variables.append(var_name)
    
    # Handle specific variable references
    if expr == 'SCHED_HRS':
        variables.append('ScheduledHours')
    
    return variables

def standardize_field_name(name):
    """Convert field names to the standard format used in scenarios.json."""
    mapping = {
        'STD_HRS': 'STD_HOURS',
        'SCHED_HRS': 'ScheduledHours'
    }
    return mapping.get(name, name)

def get_required_variables(updates):
    """Determine required variables from update expressions."""
    variables = set()
    
    # Handle old format (flat structure)
    if isinstance(updates, dict) and not ('order' in updates and 'fields' in updates):
        for field, value in updates.items():
            if isinstance(value, dict) and 'base' in value and value['base'] == 'SCHED_HRS':
                variables.add('ScheduledHours')
            elif isinstance(value, str) and value == 'SCHED_HRS':
                variables.add('ScheduledHours')
    
    # Handle new format with fields structure
    elif isinstance(updates, dict) and 'fields' in updates:
        for field, value in updates['fields'].items():
            if isinstance(value, dict):
                if 'source' in value and isinstance(value['source'], str):
                    variables.update(extract_variables_from_expression(value['source']))
                if 'calculation' in value and 'operands' in value['calculation']:
                    for operand in value['calculation']['operands']:
                        if isinstance(operand, dict) and 'variable' in operand:
                            variables.add(operand['variable'])
    
    return sorted(list(variables))

def format_calculation_to_string(calculation):
    """Format a calculation object into a string expression."""
    if not calculation or 'operation' not in calculation or 'operands' not in calculation:
        return ""
    
    operation = calculation['operation']
    operands = calculation['operands']
    
    if len(operands) < 2:
        return ""
    
    if operation == 'multiply':
        if 'variable' in operands[0] and 'constant' in operands[1]:
            return f"variables.{operands[0]['variable']} * {operands[1]['constant']}"
    
    return ""

def create_updates_structure(updates_dict):
    """Convert a flat updates dictionary to the structured format with order and fields."""
    if 'order' in updates_dict and 'fields' in updates_dict:
        return updates_dict  # Already in the correct format
    
    result = {
        "order": [],
        "fields": {}
    }
    
    for field, value in updates_dict.items():
        std_field = standardize_field_name(field)
        result["order"].append(std_field)
        
        if isinstance(value, dict) and 'multiplier' in value and 'base' in value:
            # Convert to the new format with calculation object
            if value['base'] == 'SCHED_HRS' and 'multiplier' in value:
                result["fields"][std_field] = {
                    "source": f"variables.ScheduledHours * {value['multiplier']}",
                    "type": "double",
                    "calculation": {
                        "operation": "multiply",
                        "operands": [
                            {"variable": "ScheduledHours"},
                            {"constant": value['multiplier']}
                        ]
                    }
                }
        elif value == 'SCHED_HRS':
            # Convert SCHED_HRS string to proper format
            result["fields"][std_field] = {
                "source": "variables.ScheduledHours",
                "type": "double"
            }
        elif isinstance(value, (int, float)) or value == 0:
            # Simple numeric values
            result["fields"][std_field] = {
                "source": str(value),
                "type": "double"
            }
        elif value is None or value == 'null':
            # Null values
            result["fields"][std_field] = {
                "source": "null",
                "type": "string",
                "allow_null": True
            }
        elif value == 'CURRENT_DATE':
            # Date values
            result["fields"][std_field] = {
                "source": "CURRENT_DATE",
                "type": "date"
            }
        elif value == 'Y' or value == 'ESL':
            # Simple string values
            result["fields"][std_field] = {
                "source": value,
                "type": "string"
            }
        else:
            # Default case - string values
            result["fields"][std_field] = {
                "source": str(value),
                "type": "string"
            }
    
    return result

def format_logging_messages(updates):
    """Create properly formatted logging messages based on updates."""
    logging = {
        "add_message": "",
        "update_message": ""
    }
    
    # Extract non-zero updates for logging
    messages = []
    for field, value in updates.items():
        if isinstance(value, dict):
            if 'fields' in value:  # New format
                for field_name, field_def in value['fields'].items():
                    if field_name == 'STD_HOURS' and 'calculation' in field_def:
                        calc_str = format_calculation_to_string(field_def['calculation'])
                        if calc_str:
                            messages.append(f"{field_name}: {{{calc_str}}}")
                            logging["add_message"] = f"{field_name}: {{{calc_str}}}"
                            logging["update_message"] = f"{field_name} from {{current.{field_name}}} to {{{calc_str}}}"
            elif 'base' in value and value['base'] == 'SCHED_HRS':  # Old format
                std_field = standardize_field_name(field)
                msg = f"variables.ScheduledHours * {value['multiplier']}"
                messages.append(f"{std_field}: {{{msg}}}")
                logging["add_message"] = f"{std_field}: {{{msg}}}"
                logging["update_message"] = f"{std_field} from {{current.{std_field}}} to {{{msg}}}"
    
    return logging

def update_scenario(current, new):
    """Update a scenario with new data while preserving structure."""
    # Create a deep copy to avoid modifying the original
    updated = copy.deepcopy(current)
    
    # Update basic fields
    updated['name'] = new.get('name', current.get('name', ''))
    updated['description'] = new.get('description', current.get('description', ''))
    updated['reason_code'] = new.get('reason_code', current.get('reason_code', ''))
    
    # Handle conditions section
    if 'conditions' in new:
        updated['conditions'] = new['conditions']
    
    # Handle process_level vs process_levels
    if 'process_level' in new:
        updated['process_level'] = new['process_level']
    elif 'process_levels' in new:
        if 'process_level' in updated:
            del updated['process_level']
        updated['process_levels'] = new['process_levels']
    
    # Update updates section
    if 'updates' in new:
        # Convert to the structured format with order and fields
        structured_updates = create_updates_structure(new['updates'])
        updated['updates'] = structured_updates
        
        # Set variables_required based on expressions used
        variables_required = get_required_variables(structured_updates)
        if variables_required:
            updated['variables_required'] = variables_required
        
        # Set logging messages based on updates
        updated['logging'] = format_logging_messages(structured_updates)
    
    return updated

def save_changes_to_file(target_json: Dict, scenario_map: Dict, output_file: str) -> None:
    """Save the full changes to a file for review"""
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("ESL Scenarios Update Review\n")
        f.write("=========================\n\n")
        
        for scenario in target_json.get('scenarios', []):
            scenario_id = scenario.get('id')
            if scenario_id in scenario_map:
                # Create an updated version of the scenario
                source_scenario = scenario_map[scenario_id]
                updated_scenario = update_scenario(scenario, source_scenario)
                
                # Write scenario header
                f.write(f"Scenario #{scenario_id}: {scenario.get('name', '')}\n")
                f.write("=" * 80 + "\n\n")
                
                # Write current values
                f.write("Current Values:\n")
                f.write("-" * 40 + "\n")
                f.write(f"Name: {scenario.get('name', '')}\n")
                f.write(f"Description: {scenario.get('description', '')}\n")
                f.write(f"Reason Code: {scenario.get('reason_code', '')}\n")
                
                if 'process_level' in scenario:
                    f.write(f"Process Level: {scenario.get('process_level', '')}\n")
                elif 'process_levels' in scenario:
                    f.write(f"Process Levels: {json.dumps(scenario.get('process_levels', []), indent=2)}\n")
                
                f.write(f"Conditions: {json.dumps(scenario.get('conditions', {}), indent=2)}\n")
                f.write(f"Variables Required: {json.dumps(scenario.get('variables_required', []), indent=2)}\n")
                f.write(f"Logging: {json.dumps(scenario.get('logging', {}), indent=2)}\n")
                f.write(f"Updates: {json.dumps(scenario.get('updates', {}), indent=2)}\n\n")
                
                # Write proposed values
                f.write("Proposed Values:\n")
                f.write("-" * 40 + "\n")
                f.write(f"Name: {updated_scenario.get('name', '')}\n")
                f.write(f"Description: {updated_scenario.get('description', '')}\n")
                f.write(f"Reason Code: {updated_scenario.get('reason_code', '')}\n")
                
                if 'process_level' in updated_scenario:
                    f.write(f"Process Level: {updated_scenario.get('process_level', '')}\n")
                elif 'process_levels' in updated_scenario:
                    f.write(f"Process Levels: {json.dumps(updated_scenario.get('process_levels', []), indent=2)}\n")
                
                f.write(f"Conditions: {json.dumps(updated_scenario.get('conditions', {}), indent=2)}\n")
                f.write(f"Variables Required: {json.dumps(updated_scenario.get('variables_required', []), indent=2)}\n")
                f.write(f"Logging: {json.dumps(updated_scenario.get('logging', {}), indent=2)}\n")
                f.write(f"Updates: {json.dumps(updated_scenario.get('updates', {}), indent=2)}\n\n")
                
                # Write summary of changes
                f.write("Changes Summary:\n")
                f.write("-" * 40 + "\n")
                
                # Compare and list changes
                changes = []
                
                if scenario.get('name') != updated_scenario.get('name'):
                    changes.append(f"• Name changed from '{scenario.get('name')}' to '{updated_scenario.get('name')}'")
                if scenario.get('description') != updated_scenario.get('description'):
                    changes.append("• Description updated")
                if scenario.get('reason_code') != updated_scenario.get('reason_code'):
                    changes.append(f"• Reason code changed from '{scenario.get('reason_code')}' to '{updated_scenario.get('reason_code')}'")
                
                # Check for process level changes
                if 'process_level' in scenario and 'process_level' in updated_scenario:
                    if scenario.get('process_level') != updated_scenario.get('process_level'):
                        changes.append(f"• Process level changed from {scenario.get('process_level')} to {updated_scenario.get('process_level')}")
                elif 'process_levels' in scenario and 'process_levels' in updated_scenario:
                    if scenario.get('process_levels') != updated_scenario.get('process_levels'):
                        changes.append(f"• Process levels changed from {json.dumps(scenario.get('process_levels', []))} to {json.dumps(updated_scenario.get('process_levels', []))}")
                elif 'process_level' in scenario and 'process_levels' in updated_scenario:
                    changes.append(f"• Changed from process_level {scenario.get('process_level')} to process_levels {json.dumps(updated_scenario.get('process_levels', []))}")
                elif 'process_levels' in scenario and 'process_level' in updated_scenario:
                    changes.append(f"• Changed from process_levels {json.dumps(scenario.get('process_levels', []))} to process_level {updated_scenario.get('process_level')}")
                
                if scenario.get('conditions') != updated_scenario.get('conditions'):
                    changes.append("• Conditions updated")
                if scenario.get('variables_required') != updated_scenario.get('variables_required'):
                    changes.append(f"• Required variables changed from {json.dumps(scenario.get('variables_required', []))} to {json.dumps(updated_scenario.get('variables_required', []))}")
                if scenario.get('logging') != updated_scenario.get('logging'):
                    changes.append("• Logging configuration updated")
                if json.dumps(scenario.get('updates', {})) != json.dumps(updated_scenario.get('updates', {})):
                    changes.append("• Updates section modified")
                
                if changes:
                    f.write("\n".join(changes) + "\n")
                else:
                    f.write("• No changes required\n")
                
                f.write("\n" + "="*80 + "\n\n")

def main():
    """Main entry point for the script"""
    parser = argparse.ArgumentParser(description="Update scenarios.json from JSON data")
    parser.add_argument("--target", default="../ESLFeeder/Config/scenarios.json", 
                        help="Path to target scenarios.json file to update")
    parser.add_argument("--source", default="../ESLScenarios.json", 
                        help="Path to source ESLScenarios.json file")
    parser.add_argument("--test", action="store_true", 
                        help="Run in test mode (don't write changes)")
    parser.add_argument("--review", action="store_true",
                        help="Review changes before applying them")
    args = parser.parse_args()

    print("\033[92mESL Scenarios Update Tool\033[0m")
    print("\033[92m------------------------\033[0m")

    # Verify files exist
    if not os.path.exists(args.target):
        print(f"\033[91mError: Target JSON file not found at {args.target}\033[0m")
        sys.exit(1)

    if not os.path.exists(args.source):
        print(f"\033[91mError: Source JSON file not found at {args.source}\033[0m")
        sys.exit(1)

    # Create backup of target JSON file
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = f"{args.target}.backup.{timestamp}"
    try:
        with open(args.target, 'r', encoding='utf-8') as source:
            with open(backup_path, 'w', encoding='utf-8') as target:
                target.write(source.read())
        print(f"\033[93mCreated backup at {backup_path}\033[0m")
    except Exception as e:
        print(f"\033[91mError creating backup: {e}\033[0m")
        sys.exit(1)

    # 1. Read and parse the existing target JSON
    try:
        with open(args.target, 'r', encoding='utf-8') as f:
            target_json = json.load(f)
        print("\033[92mSuccessfully loaded target JSON file\033[0m")
    except Exception as e:
        print(f"\033[91mError parsing target JSON file: {e}\033[0m")
        sys.exit(1)

    # 2. Read and parse the source JSON (using utf-8-sig to handle BOM)
    try:
        with open(args.source, 'r', encoding='utf-8-sig') as f:
            source_json = json.load(f)
        print(f"\033[92mSuccessfully loaded source JSON file with {len(source_json.get('scenarios', []))} scenarios\033[0m")
    except Exception as e:
        print(f"\033[91mError parsing source JSON file: {e}\033[0m")
        sys.exit(1)

    # 3. Create mapping structure from source JSON
    scenario_map = {}
    for scenario in source_json.get('scenarios', []):
        scenario_id = scenario.get('id')
        if scenario_id is None:
            continue

        scenario_map[scenario_id] = {
            'name': scenario.get('name', ''),
            'description': scenario.get('description', ''),
            'reason_code': scenario.get('reason_code', ''),
            'process_levels': scenario.get('process_levels', []),
            'conditions': {
                'required': scenario.get('conditions', {}).get('required', []),
                'forbidden': scenario.get('conditions', {}).get('forbidden', []),
                'optional': scenario.get('conditions', {}).get('optional', [])
            },
            'updates': scenario.get('updates', {}),
            'variables_required': scenario.get('variables_required', []),
            'logging': scenario.get('logging', {})
        }

    # 4. Update each scenario in the target JSON
    updated_count = 0
    skipped_count = 0

    for scenario in target_json.get('scenarios', []):
        scenario_id = scenario.get('id')
        if scenario_id in scenario_map:
            source_scenario = scenario_map[scenario_id]
            scenario_name = scenario.get('name', '')

            print(f"\033[96mProcessing scenario #{scenario_id}: {scenario_name}\033[0m")

            # Update the fields from source
            scenario = update_scenario(scenario, source_scenario)

            updated_count += 1
        else:
            skipped_count += 1

    # 5. Show update summary
    print("\033[92mUpdate Summary:\033[0m")
    print(f"\033[96m- Found {len(scenario_map)} scenarios in source JSON\033[0m")
    print(f"\033[96m- Updated {updated_count} scenarios in target JSON\033[0m")
    print(f"\033[93m- Skipped {skipped_count} scenarios (not found in source)\033[0m")

    # 6. Handle review mode and test mode
    if args.review:
        review_changes(target_json, scenario_map)
        print("\033[93mDo you want to apply these changes? (y/n):\033[0m")
        if input().lower() != 'y':
            print("\033[93mChanges not applied. Original file preserved.\033[0m")
            sys.exit(0)
    elif args.test:
        print("\033[93mTest mode enabled - not writing changes to file\033[0m")
        # Save changes to a review file
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        review_file = f"scenario_changes_{timestamp}.txt"
        save_changes_to_file(target_json, scenario_map, review_file)
        print(f"\033[92mFull changes saved to {review_file} for review\033[0m")
        print("\033[96mSample of first updated scenario:\033[0m")
        if updated_count > 0:
            first_scenario = next(s for s in target_json['scenarios'] if s['id'] in scenario_map)
            print(json.dumps(first_scenario, indent=2))
    else:
        try:
            with open(args.target, 'w', encoding='utf-8') as f:
                json.dump(target_json, f, indent=2)
            print(f"\033[92mSuccessfully wrote updated JSON to {args.target}\033[0m")
        except Exception as e:
            print(f"\033[91mError writing to JSON file: {e}\033[0m")
            print("\033[93mChanges not saved. Original file preserved.\033[0m")
            sys.exit(1)

    print("\033[92mScript completed successfully\033[0m")

if __name__ == "__main__":
    main() 