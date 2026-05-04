import * as React from "react"
import { X } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { getTagColor } from "@/lib/tag-colors"

interface TagInputProps {
  value: string[]
  onChange: (tags: string[]) => void
  placeholder?: string
  maxTags?: number
  maxTagLength?: number
  disabled?: boolean
  id?: string
}

function TagInput({
  value,
  onChange,
  placeholder = "Add a tag…",
  maxTags = 20,
  maxTagLength = 50,
  disabled = false,
  id,
}: TagInputProps) {
  const [inputValue, setInputValue] = React.useState("")
  const inputRef = React.useRef<HTMLInputElement>(null)

  const addTags = React.useCallback(
    (raw: string) => {
      const candidates = raw
        .split(",")
        .map((s) => s.trim().slice(0, maxTagLength))
        .filter(Boolean)

      const next = [...value]
      for (const tag of candidates) {
        if (next.length >= maxTags) break
        if (next.some((t) => t.toLowerCase() === tag.toLowerCase())) continue
        next.push(tag)
      }

      if (next.length !== value.length) {
        onChange(next)
      }
    },
    [value, onChange, maxTags, maxTagLength],
  )

  const removeTag = React.useCallback(
    (index: number) => {
      onChange(value.filter((_, i) => i !== index))
    },
    [value, onChange],
  )

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault()
      if (inputValue.trim()) {
        addTags(inputValue)
        setInputValue("")
      }
    } else if (e.key === "Backspace" && inputValue === "" && value.length > 0) {
      removeTag(value.length - 1)
    } else if (e.key === "Escape") {
      inputRef.current?.blur()
    }
  }

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    const pasted = e.clipboardData.getData("text")
    if (pasted.includes(",")) {
      e.preventDefault()
      addTags(pasted)
      setInputValue("")
    }
  }

  const atLimit = value.length >= maxTags

  return (
    <div
      className={cn(
        "border-input flex min-h-9 w-full flex-wrap items-center gap-1.5 rounded-md border bg-transparent px-3 py-1.5 shadow-xs transition-[color,box-shadow]",
        "has-[input:focus]:border-ring has-[input:focus]:ring-ring/50 has-[input:focus]:ring-[3px]",
        disabled && "pointer-events-none cursor-not-allowed opacity-50",
      )}
      onClick={() => !disabled && inputRef.current?.focus()}
    >
      {value.map((tag, index) => (
        <Badge key={`${tag}-${index}`} variant="secondary" className={`gap-1 pr-1 border-transparent ${getTagColor(tag)}`}>
          {tag}
          {!disabled && (
            <button
              type="button"
              aria-label={`Remove tag: ${tag}`}
              className="hover:bg-muted rounded-sm p-0.5 transition-colors"
              onClick={(e) => {
                e.stopPropagation()
                removeTag(index)
              }}
              tabIndex={-1}
            >
              <X className="size-3" />
            </button>
          )}
        </Badge>
      ))}
      {!disabled && !atLimit && (
        <input
          ref={inputRef}
          id={id}
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          onPaste={handlePaste}
          onBlur={() => {
            if (inputValue.trim()) {
              addTags(inputValue)
              setInputValue("")
            }
          }}
          placeholder={value.length === 0 ? placeholder : ""}
          className="placeholder:text-muted-foreground min-w-[80px] flex-1 bg-transparent text-sm outline-none"
          aria-label="Add tag"
        />
      )}
      {!disabled && atLimit && (
        <span className="text-muted-foreground text-xs">
          Maximum {maxTags} tags
        </span>
      )}
    </div>
  )
}

export { TagInput }
export type { TagInputProps }
