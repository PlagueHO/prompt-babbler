<#
.SYNOPSIS
    Scaffolds a new built-in prompt template JSON file for Prompt Babbler.

.DESCRIPTION
    Creates a minimal valid JSON file from the base template asset with the
    provided slug, name, and description. The file is placed in the specified
    output directory and is ready for editing to add instructions, guardrails,
    and other fields.

.PARAMETER Slug
    The template slug (lowercase, hyphens). Will be prefixed with 'builtin-'.

.PARAMETER Name
    The human-readable display name for the template.

.PARAMETER Description
    A brief description of what the template does (max 1000 chars).

.PARAMETER OutputPath
    The directory where the JSON file will be created.
    Defaults to 'prompt-babbler-service/src/Infrastructure/Templates'.

.EXAMPLE
    .\New-BuiltinPromptTemplate.ps1 -Slug "email-drafting" -Name "Email Drafting Prompt" -Description "Converts babble into a professional email draft."
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9]+(-[a-z0-9]+)*$')]
    [ValidateLength(1, 86)]
    [string]$Slug,

    [Parameter(Mandatory)]
    [ValidateLength(1, 200)]
    [string]$Name,

    [Parameter(Mandatory)]
    [ValidateLength(1, 1000)]
    [string]$Description,

    [Parameter()]
    [string]$OutputPath = (Join-Path $PSScriptRoot '..' '..' '..' '..' 'prompt-babbler-service' 'src' 'Infrastructure' 'Templates')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$id = "builtin-$Slug"
$filename = "$id.json"
$filepath = Join-Path (Resolve-Path $OutputPath) $filename

if (Test-Path $filepath) {
    Write-Error "Template '$filename' already exists at '$filepath'. Choose a different slug or remove the existing file."
    return
}

$baseTemplatePath = Join-Path $PSScriptRoot '..' 'assets' 'builtin-template-base.json'
if (-not (Test-Path $baseTemplatePath)) {
    Write-Error "Base template asset not found at '$baseTemplatePath'."
    return
}

$template = Get-Content -Path $baseTemplatePath -Raw | ConvertFrom-Json

$template.id = $id
$template.name = $Name
$template.description = $Description

$json = $template | ConvertTo-Json -Depth 10
$json | Set-Content -Path $filepath -Encoding utf8NoBOM

Write-Host "Created template: $filepath" -ForegroundColor Green
Write-Host "Edit the file to customize instructions, guardrails, and other fields." -ForegroundColor Cyan
