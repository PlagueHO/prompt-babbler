---
name: create-builtin-prompt-template
description: >-
  Create a new built-in prompt template JSON file for Prompt Babbler that
  conforms to the prompt-template.schema.json schema. Use when the user wants
  to add a new builtin template, create a prompt template, scaffold a template,
  or add a new babble-to-prompt conversion template. Interviews the user for
  missing details via vscode_askQuestions, generates a conformant JSON file,
  and places it in the Templates directory.
metadata:
  author: PlagueHO
  version: 1.0.0
---

# Create Built-in Prompt Template

## Context

Templates live in `prompt-babbler-service/src/Infrastructure/Templates/` and
conform to `prompt-template.schema.json`. Each template is a JSON file named
`builtin-<slug>.json`.

## Process

### Step 1 — Gather Template Requirements

Read `assets/builtin-template-base.json` (relative to this skill) for the
required structure.

Extract from the user's description:

- Use case: what type of prompt does this template produce?
- Name: human-readable display name
- Description: brief summary (max 1000 chars)
- Instructions: core LLM directive for converting babble into the target format
- Output description: characteristics of the expected output
- Output template (optional): structural template for the output format
- Examples (optional): input/output pairs for few-shot prompting
- Guardrails (optional): constraints the LLM must follow
- Output format: `text` or `markdown`
- Allow emojis: whether emojis are appropriate
- Tags: categorization tags

If required information is missing, use `vscode_askQuestions` to interview the
user. Ask only for missing details. Use options where sensible.

Minimum questions when no context is provided:

```
1. Purpose — What type of prompt should this template produce?
   (e.g., email drafting, code review, meeting summary, creative writing)
2. Output format — Should the output be plain text or markdown?
   Options: text, markdown
3. Emojis — Should the output include emojis?
   Options: Yes, No
4. Tags — What tags describe this template? (comma-separated)
```

### Step 2 — Derive the Template ID and Filename

Generate the template ID from the purpose:

1. Derive a short slug from the use case (lowercase, hyphens, no spaces).
2. Prefix with `builtin-` to form the ID: `builtin-<slug>`.
3. The filename is `<id>.json`.

Confirm ID and filename with the user.

ID validation rules (enforced by the schema):

- Must match pattern `^builtin-[a-z0-9-]+$`
- 1–100 characters total
- No consecutive hyphens, no leading/trailing hyphens after `builtin-`

### Step 3 — Write the Instructions

Craft the `instructions` field following existing template patterns:

1. Open with a role statement: *"You are a [role]. Your task is to take a
   stream-of-consciousness recording transcript and convert it into..."*
2. Provide a `Guidelines:` section as a bulleted list of specific directives.
3. Be concrete about what to extract, preserve, and discard from the transcript.
4. Keep instructions between 500 and 2000 characters.

### Step 4 — Write the Guardrails

Guardrails are "don't-do" constraints describing behaviour the LLM must avoid.
Common guardrails across existing templates:

- "Include any preamble, explanation, or meta-commentary about the prompt"
- "Add assumptions or information not present in the original transcript"
- "Alter the user's original intent or tone"

Add use-case-specific guardrails.

### Step 5 — Assemble and Write the JSON File

Assemble the JSON object using the base template asset as a structural
reference. Required fields:

| Field | Type | Required |
|-------|------|----------|
| `$schema` | string | Yes — always `"./prompt-template.schema.json"` |
| `schemaVersion` | string | Yes — always `"2.0"` |
| `id` | string | Yes — `builtin-<slug>` |
| `name` | string | Yes — display name |
| `description` | string | Yes — 1–1000 chars |
| `instructions` | string | Yes — 1–50000 chars |
| `outputDescription` | string | No |
| `outputTemplate` | string | No |
| `examples` | array | No |
| `guardrails` | array | No |
| `defaultOutputFormat` | enum | No — `"text"` or `"markdown"` |
| `defaultAllowEmojis` | boolean | No |
| `tags` | array | No |

Write the file to:

```
prompt-babbler-service/src/Infrastructure/Templates/<id>.json
```

### Step 6 — Validate

After creating the file, verify:

- [ ] JSON is valid and parseable
- [ ] `$schema` is `"./prompt-template.schema.json"`
- [ ] `schemaVersion` is `"2.0"`
- [ ] `id` matches the filename (without `.json`)
- [ ] `id` matches pattern `^builtin-[a-z0-9-]+$`
- [ ] `name` is 1–200 characters
- [ ] `description` is 1–1000 characters
- [ ] `instructions` is 1–50000 characters
- [ ] `guardrails` (if present) has at most 20 items, each 1–500 chars
- [ ] `examples` (if present) has at most 10 items with `input` and `output`
- [ ] `tags` (if present) has at most 20 items, each 1–50 chars
- [ ] No duplicate of an existing template ID in the Templates directory

Use `get_errors` on the created file to check for JSON schema violations.

### Step 7 — Present the Result

Show the user:

1. The generated filename and path.
2. The `id`, `name`, and `description` for review.
3. A summary of the instructions and guardrails.
4. Any optional fields included or omitted.

Ask: *"Does this template look correct? Would you like to adjust the
instructions, guardrails, or any other fields?"*

## Scaffolding Scripts

For batch creation or CI automation, use the scripts in `scripts/`:

**PowerShell:**

```powershell
& "<skill-path>/scripts/New-BuiltinPromptTemplate.ps1" `
    -Slug "<slug>" `
    -Name "<display-name>" `
    -Description "<description>" `
    -OutputPath "prompt-babbler-service/src/Infrastructure/Templates"
```

**Shell:**

```bash
"<skill-path>/scripts/new-builtin-prompt-template.sh" \
    --slug "<slug>" \
    --name "<display-name>" \
    --description "<description>" \
    --output-path "prompt-babbler-service/src/Infrastructure/Templates"
```

These scripts generate a minimal valid JSON file from the base template asset
for further editing.

## Edge Cases

- **Duplicate ID**: If a template with the same ID already exists, warn the user
  and ask whether to overwrite or choose a different slug.
- **Very long instructions**: If instructions exceed 5000 characters, suggest
  splitting into a shorter `instructions` field and using `outputTemplate` for
  structural guidance.
- **No clear use case**: If the description is too vague, ask targeted
  follow-up questions via `vscode_askQuestions` rather than guessing.
