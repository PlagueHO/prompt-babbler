# Tag Editing Flow Research

## Research Questions

1. Can tags be set/edited directly within BabbleCard, BabbleListItem, or BabbleBubbles components?
1. Is tag editing ONLY available in the BabbleEditor?
1. Where does `TagInput` appear (every location where tags are editable)?
1. What is the babble detail page editing flow?
1. Is there a babble creation form that includes tag input?
1. What is the full user journey for setting tags on a babble?

---

## Findings

### 1. BabbleCard (`prompt-babbler-app/src/components/babbles/BabbleCard.tsx`)

**No tag editing. Display only.**

- Imports `TagList` (read-only badge display)
- Renders `<TagList tags={babble.tags} className="mt-2" />`
- The entire card is wrapped in a `<Link to={/babble/${babble.id}}>` — clicking navigates to the detail page
- No click handlers, no inline editing, no `TagInput` import

### 2. BabbleListItem (`prompt-babbler-app/src/components/babbles/BabbleListItem.tsx`)

**No tag editing. Display only.**

- Imports `TagList` (read-only badge display)
- Renders `<TagList tags={babble.tags} className="mt-1" />` conditionally when tags exist
- Only interactive element is a pin/unpin button
- Clicking the title navigates to `/babble/${babble.id}`
- No `TagInput` import, no edit handlers for tags

### 3. BabbleBubbles (`prompt-babbler-app/src/components/babbles/BabbleBubbles.tsx`)

**No tag editing. Display only.**

- Imports `TagList` (read-only badge display)
- Renders `<TagList tags={babble.tags} className="mt-2" />`
- Interactive elements: pin/unpin button, link to babble detail page
- No `TagInput` import, no edit handlers for tags

### 4. BabbleEditor (`prompt-babbler-app/src/components/babbles/BabbleEditor.tsx`)

**YES — this is the primary place babble tags are edited.**

- Imports `TagInput` from `@/components/ui/tag-input`
- Manages local state: `const [tags, setTags] = useState<string[]>(babble.tags ?? [])`
- Renders a full `TagInput` with `maxTags={20}`, `maxTagLength={50}`
- On save, includes `tags` in the updated babble object passed to `onSave`

### 5. TagInput Usage — All Locations

`TagInput` is imported and used in exactly **2 source components** (excluding dist/tests):

| File | Context | Editable? |
|------|---------|-----------|
| `src/components/babbles/BabbleEditor.tsx` | Editing babble text + tags | YES |
| `src/components/templates/TemplateEditor.tsx` | Editing template tags | YES (templates, not babbles) |

The `TagList` component (read-only badge display) is used in:

| File | Context |
|------|---------|
| `src/components/babbles/BabbleCard.tsx` | Display only |
| `src/components/babbles/BabbleListItem.tsx` | Display only |
| `src/components/babbles/BabbleBubbles.tsx` | Display only |
| `src/pages/BabblePage.tsx` | Display (when not editing) |

### 6. BabblePage Detail Flow (`prompt-babbler-app/src/pages/BabblePage.tsx`)

The babble detail page controls the editing flow:

- **State:** `const [isEditing, setIsEditing] = useState(false)`
- **Edit button:** `<Button onClick={() => setIsEditing(true)}>Edit Text</Button>`
- **When NOT editing:** Shows read-only babble text + `<TagList tags={babble.tags} />` (display only)
- **When editing:** Renders `<BabbleEditor babble={babble} onSave={...} onCancel={...} />`
- **Tag display is hidden while editing:** `{(babble.tags?.length ?? 0) > 0 && !isEditing && (<TagList ... />)}`
- **Save handler:** Calls `updateBabble(updated.id, { title: updated.title, text: updated.text, tags: updated.tags })`

The flow is:

1. User views babble detail page → tags shown via `TagList` (read-only badges)
1. User clicks "Edit Text" button → `isEditing = true`
1. `BabbleEditor` renders with both text area AND `TagInput`
1. User can add/remove tags in the `TagInput`
1. User clicks "Save" → `handleSave` updates babble including tags via API
1. `isEditing = false` → back to read-only view with updated tags

### 7. Babble Creation Flow (`prompt-babbler-app/src/pages/RecordPage.tsx`)

**No tag input during creation.**

- The RecordPage handles creating new babbles (recording voice → transcription → save)
- The `saveBabble` function calls `createBabble({ title, text })` — **no tags parameter**
- The `createBabble` API accepts `{ title: string; text: string; tags?: string[] }` but RecordPage never passes tags
- There is NO `TagInput` on the RecordPage
- After saving, user is navigated to `/babble/${babble.id}` where they can then edit to add tags

### 8. API Contract

From `api-client.ts`:

- `createBabble(request: { title: string; text: string; tags?: string[] })` — tags optional at creation
- `updateBabble(id, request: { title: string; text: string; tags?: string[] })` — tags optional at update

From `useBabbles.ts`:

- Both `createBabble` and `updateBabble` pass through `tags` when provided

---

## Key Conclusions

1. **Tag editing for babbles is ONLY available in the `BabbleEditor` component**, which appears exclusively on the `BabblePage` (babble detail view) when `isEditing` is true.

1. **BabbleCard, BabbleListItem, and BabbleBubbles are strictly display-only** — they use `TagList` (renders read-only `Badge` elements) and have no edit capabilities for tags.

1. **Tags cannot be set during babble creation** — the RecordPage does not include a `TagInput` and does not pass tags when calling `createBabble`.

1. **The only user journey to set tags on a babble is:**
   - Create babble (via RecordPage or file upload) → no tags
   - Navigate to babble detail page (`/babble/:id`)
   - Click "Edit Text" button
   - Add tags via `TagInput` in `BabbleEditor`
   - Click "Save"

1. **The `TemplateEditor` also uses `TagInput`** but for template tags, not babble tags — this is a separate domain.

---

## Follow-on Questions (Relevant to Scope)

- Should tags be settable during babble creation on RecordPage? (Currently not possible)
- Should there be inline tag editing on the BabblePage without entering full edit mode? (Currently requires "Edit Text" to access tags)

---

## References

- `prompt-babbler-app/src/components/babbles/BabbleCard.tsx` — lines 1–44
- `prompt-babbler-app/src/components/babbles/BabbleListItem.tsx` — lines 1–59
- `prompt-babbler-app/src/components/babbles/BabbleBubbles.tsx` — lines 1–89
- `prompt-babbler-app/src/components/babbles/BabbleEditor.tsx` — lines 1–56
- `prompt-babbler-app/src/pages/BabblePage.tsx` — lines 1–350 (key: L46, L288, L304-312)
- `prompt-babbler-app/src/pages/RecordPage.tsx` — lines 1–250 (key: L133)
- `prompt-babbler-app/src/components/ui/tag-list.tsx` — lines 1–21 (read-only Badge display)
- `prompt-babbler-app/src/components/ui/tag-input.tsx` — TagInput component definition
- `prompt-babbler-app/src/hooks/useBabbles.ts` — lines 192-210 (createBabble/updateBabble)
- `prompt-babbler-app/src/services/api-client.ts` — lines 220-230 (createBabble API)
- `prompt-babbler-app/src/components/templates/TemplateEditor.tsx` — lines 290-320 (template TagInput)
