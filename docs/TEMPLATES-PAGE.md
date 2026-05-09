---
title: Templates Page
description: Detailed guide to browsing, filtering, viewing, and creating prompt templates in Prompt Babbler.
author: Prompt Babbler Team
ms.date: 2026-05-09
ms.topic: concept
keywords:
  - templates
  - built-in templates
  - custom templates
  - prompt design
estimated_reading_time: 6
---

## Overview

The Templates page manages the instructions that convert a babble into a structured prompt.

![Templates list view](/images/templates-list.png)

## Template list

The list screen is designed for browsing and filtering:

* **Create Template** opens the custom-template editor.
* **Filter templates by name** narrows the list by title or description.
* **Filter templates by tag** narrows the list by topic.
* **Sort** switches between recently used and alphabetical order.
* Built-in templates are clearly marked with a **Built-in** badge.

## Viewing built-in templates

Built-in templates are read-only. They let you inspect the default instructions shipped with Prompt Babbler.

![Built-in template detail](/images/template-detail-built-in.png)

The built-in detail view shows:

* name and description
* full instructions
* output description and output template
* examples and guardrails when present
* default output format and emoji behavior
* tags

Because built-in templates are read-only, the editor disables changes and replaces save actions with **Close**.

## Creating a custom template

Use **Create Template** when the built-in options are close but not quite right.

![Create template form](/images/template-create.png)

The custom-template editor includes these fields:

* **Name**
* **Description**
* **Instructions**
* **Output Description**
* **Output Template**
* **Examples**
* **Guardrails**
* **Default Output Format**
* **Allow Emojis**
* **Tags**

## Template design guidance

Custom templates work best when you:

* keep the instructions specific
* describe the expected output shape clearly
* add examples when the output format is strict
* add guardrails for anything the model must avoid
* tag templates so they are easier to find later

## When to use the Templates page

Use this page when you want to:

* inspect how built-in prompt generation works
* compare available prompt styles
* create team-specific or task-specific templates
* organize templates by name, tag, or recent use
