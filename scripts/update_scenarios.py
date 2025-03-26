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
import shutil

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

def create_new_scenario(source_scenario):
    """Create a new scenario from source data with proper formatting."""
    # Start with a deep copy of the source data
    new_scenario = copy.deepcopy(source_scenario)
    
    # Ensure required fields exist
    if 'id' not in new_scenario:
        raise ValueError("New scenario must have an ID")
    
    if 'name' not in new_scenario:
        new_scenario['name'] = f"New Scenario {new_scenario['id']}"
    
    if 'description' not in new_scenario:
        new_scenario['description'] = ""
    
    if 'reason_code' not in new_scenario:
        new_scenario['reason_code'] = ""
    
    # Handle process levels
    if 'process_level' in new_scenario and 'process_levels' not in new_scenario:
        new_scenario['process_levels'] = [new_scenario['process_level']]
        del new_scenario['process_level']
    elif 'process_levels' not in new_scenario:
        new_scenario['process_levels'] = []
    
    # Ensure conditions structure
    if 'conditions' not in new_scenario:
        new_scenario['conditions'] = {'required': [], 'forbidden': [], 'optional': []}
    elif not isinstance(new_scenario['conditions'], dict):
        # Convert to dictionary if it's not already
        new_scenario['conditions'] = {'required': [], 'forbidden': [], 'optional': []}
    else:
        # Ensure all condition types exist
        if 'required' not in new_scenario['conditions']:
            new_scenario['conditions']['required'] = []
        if 'forbidden' not in new_scenario['conditions']:
            new_scenario['conditions']['forbidden'] = []
        if 'optional' not in new_scenario['conditions']:
            new_scenario['conditions']['optional'] = []
    
    # Format updates section
    if 'updates' in new_scenario:
        structured_updates = create_updates_structure(new_scenario['updates'])
        new_scenario['updates'] = structured_updates
        
        # Add variables_required based on updates
        variables_required = get_required_variables(structured_updates)
        if variables_required:
            new_scenario['variables_required'] = variables_required
        elif 'variables_required' not in new_scenario:
            new_scenario['variables_required'] = []
        
        # Set logging message based on updates
        new_scenario['logging'] = format_logging_messages(structured_updates)
    else:
        new_scenario['updates'] = {"order": [], "fields": {}}
        new_scenario['variables_required'] = []
        new_scenario['logging'] = {"add_message": "", "update_message": ""}
    
    return new_scenario

def save_changes_to_file(target_json: Dict, scenario_map: Dict, output_file: str) -> None:
    """Save the full changes to a file for review"""
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("ESL Scenarios Update Review\n")
        f.write("=========================\n\n")
        
        # First process existing scenarios
        existing_scenarios = []
        for scenario in target_json.get('scenarios', []):
            scenario_id = scenario.get('id')
            if scenario_id in scenario_map:
                existing_scenarios.append(scenario_id)
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
        
        # Now process new scenarios that don't exist in the target
        for scenario_id, source_data in scenario_map.items():
            if scenario_id not in existing_scenarios:
                # This is a new scenario - create a properly formatted one
                new_scenario = create_new_scenario(source_data)
                
                # Write scenario header
                f.write(f"NEW Scenario #{scenario_id}: {new_scenario.get('name', '')}\n")
                f.write("=" * 80 + "\n\n")
                
                # Write current values
                f.write("Current Values:\n")
                f.write("-" * 40 + "\n")
                f.write("None - This is a new scenario that doesn't exist in the target file.\n\n")
                
                # Write proposed values
                f.write("Proposed Values:\n")
                f.write("-" * 40 + "\n")
                f.write(f"Name: {new_scenario.get('name', '')}\n")
                f.write(f"Description: {new_scenario.get('description', '')}\n")
                f.write(f"Reason Code: {new_scenario.get('reason_code', '')}\n")
                
                if 'process_level' in new_scenario:
                    f.write(f"Process Level: {new_scenario.get('process_level', '')}\n")
                elif 'process_levels' in new_scenario:
                    f.write(f"Process Levels: {json.dumps(new_scenario.get('process_levels', []), indent=2)}\n")
                
                f.write(f"Conditions: {json.dumps(new_scenario.get('conditions', {}), indent=2)}\n")
                f.write(f"Variables Required: {json.dumps(new_scenario.get('variables_required', []), indent=2)}\n")
                f.write(f"Logging: {json.dumps(new_scenario.get('logging', {}), indent=2)}\n")
                f.write(f"Updates: {json.dumps(new_scenario.get('updates', {}), indent=2)}\n\n")
                
                # Write summary of changes
                f.write("Changes Summary:\n")
                f.write("-" * 40 + "\n")
                f.write("• Adding new scenario\n")
                
                f.write("\n" + "="*80 + "\n\n")

def main():
    """Main function to process command line arguments and run the update."""
    parser = argparse.ArgumentParser(description='ESL Scenarios Update Tool')
    parser.add_argument('--source', required=True, help='Source JSON file with updated scenarios')
    parser.add_argument('--target', required=True, help='Target JSON file to update')
    parser.add_argument('--test', action='store_true', help='Run in test mode (no changes saved)')
    args = parser.parse_args()

    # Load source and target JSON files
    with open(args.source, 'r', encoding='utf-8-sig') as f:  # Use utf-8-sig to handle BOM
        source_json = json.load(f)

    with open(args.target, 'r', encoding='utf-8') as f:
        target_json = json.load(f)

    # Create a map of source scenarios by ID
    scenario_map = {s['id']: s for s in source_json.get('scenarios', [])}

    # Create backup of target file
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_file = f"{args.target}.backup.{timestamp}"
    shutil.copy2(args.target, backup_file)

    # Create review file
    review_file = f"scenario_changes_{timestamp}.txt"
    save_changes_to_file(target_json, scenario_map, review_file)

    if not args.test:
        # Update existing scenarios and add new ones
        updated_scenarios = []
        existing_scenario_ids = set()

        # First, process existing scenarios
        for scenario in target_json.get('scenarios', []):
            scenario_id = scenario.get('id')
            existing_scenario_ids.add(scenario_id)
            if scenario_id in scenario_map:
                # Update existing scenario
                source_scenario = scenario_map[scenario_id]
                updated_scenario = update_scenario(scenario, source_scenario)
                updated_scenarios.append(updated_scenario)
            else:
                # Keep unchanged scenario
                updated_scenarios.append(scenario)

        # Add new scenarios
        for scenario_id, source_data in scenario_map.items():
            if scenario_id not in existing_scenario_ids:
                new_scenario = create_new_scenario(source_data)
                updated_scenarios.append(new_scenario)

        # Sort scenarios by ID
        updated_scenarios.sort(key=lambda x: x.get('id', 0))

        # Update the target JSON with new scenarios list
        target_json['scenarios'] = updated_scenarios

        # Save the updated target file
        with open(args.target, 'w', encoding='utf-8') as f:
            json.dump(target_json, f, indent=2)

        print(f"Changes have been saved to {args.target}")
        print(f"A backup of the original file has been saved to {backup_file}")
    else:
        print("Test mode: No changes have been saved")

    print(f"Review of changes has been saved to {review_file}")

if __name__ == '__main__':
    main() 