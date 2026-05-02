#!/usr/bin/env bash
# Scaffolds a new built-in prompt template JSON file for Prompt Babbler.
#
# Usage:
#   ./new-builtin-prompt-template.sh \
#       --slug "email-drafting" \
#       --name "Email Drafting Prompt" \
#       --description "Converts babble into a professional email draft." \
#       --output-path "prompt-babbler-service/src/Infrastructure/Templates"

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

slug=""
name=""
description=""
output_path="${SCRIPT_DIR}/../../../prompt-babbler-service/src/Infrastructure/Templates"

usage() {
    echo "Usage: $0 --slug <slug> --name <name> --description <description> [--output-path <path>]"
    echo ""
    echo "  --slug         Template slug (lowercase, hyphens). Prefixed with 'builtin-'."
    echo "  --name         Human-readable display name (1-200 chars)."
    echo "  --description  Brief description of the template (1-1000 chars)."
    echo "  --output-path  Directory for the JSON file (default: Templates dir)."
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --slug) slug="$2"; shift 2 ;;
        --name) name="$2"; shift 2 ;;
        --description) description="$2"; shift 2 ;;
        --output-path) output_path="$2"; shift 2 ;;
        *) echo "Unknown argument: $1"; usage ;;
    esac
done

if [[ -z "$slug" || -z "$name" || -z "$description" ]]; then
    echo "Error: --slug, --name, and --description are required."
    usage
fi

if ! echo "$slug" | grep -qE '^[a-z0-9]+(-[a-z0-9]+)*$'; then
    echo "Error: Slug must be lowercase alphanumeric with single hyphens (no leading/trailing)."
    exit 1
fi

id="builtin-${slug}"
filename="${id}.json"
filepath="${output_path}/${filename}"

if [[ -f "$filepath" ]]; then
    echo "Error: Template '${filename}' already exists at '${filepath}'. Choose a different slug."
    exit 1
fi

base_template="${SCRIPT_DIR}/../assets/builtin-template-base.json"
if [[ ! -f "$base_template" ]]; then
    echo "Error: Base template asset not found at '${base_template}'."
    exit 1
fi

mkdir -p "$output_path"

# Use sed to replace placeholders in the base template
sed -e "s|builtin-REPLACE_WITH_SLUG|${id}|g" \
    -e "s|REPLACE_WITH_DISPLAY_NAME|${name}|g" \
    -e "s|REPLACE_WITH_DESCRIPTION|${description}|g" \
    "$base_template" > "$filepath"

echo "Created template: ${filepath}"
echo "Edit the file to customize instructions, guardrails, and other fields."
