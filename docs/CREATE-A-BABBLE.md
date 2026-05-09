---
title: Create a Babble
description: Detailed guide to the Create a Babble page, including recording, uploads, transcript review, and save actions.
author: Prompt Babbler Team
ms.date: 2026-05-09
ms.topic: how-to
keywords:
  - create babble
  - recording
  - transcript
  - upload
estimated_reading_time: 6
---

## Overview

The Create a Babble page is where you capture spoken ideas and convert them into saved text.

![Create a Babble empty state](/images/create-babble-empty.png)

When you are actively working with a transcript, the page looks like this:

![Create a Babble with recorded transcript](/images/create-babble-recorded.png)

## Fields and controls

### Title

The title field is optional. Use it when you already know what the babble is about. If you leave it blank, Prompt Babbler creates a default title when you save.

### Tags

Tags are optional and useful for grouping related babbles. Press `Enter` to add a tag.

### Recording controls

The recording panel contains the main capture actions:

* The microphone button starts and stops recording.
* The recording status text changes as capture starts, stops, or resumes.
* The waveform visualizer appears while recording is active.
* **Upload audio file** lets you submit an existing recording instead of speaking live.

> [!IMPORTANT]
> Prompt Babbler needs browser microphone permission for live recording. If permission is denied, recording does not start.

### Transcript preview

The large transcript area updates with captured speech. Review the text before you save it.

## Save actions

The page gives you three main outcomes:

* **Save Babble** stores the transcript and opens the babble detail page.
* **Save & Generate Prompt** stores the transcript and immediately moves into prompt generation with the template you choose.
* **Clear** removes the unsaved transcript from the page.

## Upload flow

If you already have an audio file, use **Upload audio file** instead of the microphone:

* supported formats include common audio types such as MP3, WAV, WebM, OGG, and M4A
* the file is transcribed and saved as a babble
* the app navigates to the saved babble when transcription completes

## Continue mode

When you open **Continue Babble** from an existing babble, this same page switches into append mode:

* the title changes to **Continue Babble**
* the existing text is preserved
* new recording is appended to the current babble instead of creating a new one
* the main actions become **Update Babble** and **Update & Generate Prompt**

## Best practices

Use the Create a Babble page when you want to:

* capture a rough idea quickly
* transcribe a spoken note before editing
* upload an existing voice memo
* save first and refine later
