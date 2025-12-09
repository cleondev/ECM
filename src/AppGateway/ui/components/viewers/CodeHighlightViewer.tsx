"use client"

import { useMemo } from "react"
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter"
import bash from "react-syntax-highlighter/dist/esm/languages/prism/bash"
import csharp from "react-syntax-highlighter/dist/esm/languages/prism/csharp"
import css from "react-syntax-highlighter/dist/esm/languages/prism/css"
import go from "react-syntax-highlighter/dist/esm/languages/prism/go"
import java from "react-syntax-highlighter/dist/esm/languages/prism/java"
import javascript from "react-syntax-highlighter/dist/esm/languages/prism/javascript"
import json from "react-syntax-highlighter/dist/esm/languages/prism/json"
import kotlin from "react-syntax-highlighter/dist/esm/languages/prism/kotlin"
import markdown from "react-syntax-highlighter/dist/esm/languages/prism/markdown"
import python from "react-syntax-highlighter/dist/esm/languages/prism/python"
import ruby from "react-syntax-highlighter/dist/esm/languages/prism/ruby"
import rust from "react-syntax-highlighter/dist/esm/languages/prism/rust"
import scss from "react-syntax-highlighter/dist/esm/languages/prism/scss"
import sql from "react-syntax-highlighter/dist/esm/languages/prism/sql"
import swift from "react-syntax-highlighter/dist/esm/languages/prism/swift"
import typescript from "react-syntax-highlighter/dist/esm/languages/prism/typescript"
import yaml from "react-syntax-highlighter/dist/esm/languages/prism/yaml"
import { vscDarkPlus } from "react-syntax-highlighter/dist/esm/styles/prism"

const REGISTERED_LANGUAGES: Record<string, unknown> = {
  bash,
  csharp,
  css,
  go,
  java,
  javascript,
  json,
  kotlin,
  markdown,
  python,
  ruby,
  rust,
  scss,
  sql,
  swift,
  ts: typescript,
  tsx: typescript,
  typescript,
  yaml,
}

Object.entries(REGISTERED_LANGUAGES).forEach(([name, definition]) => {
  SyntaxHighlighter.registerLanguage(name, definition)
})

const LANGUAGE_ALIASES: Record<string, string> = {
  js: "javascript",
  jsx: "javascript",
  ts: "typescript",
  tsx: "typescript",
  sh: "bash",
  yml: "yaml",
  md: "markdown",
}

type CodeHighlightViewerProps = {
  code: string
  language?: string
  wrapLongLines?: boolean
}

function normalizeLanguage(language?: string): string | undefined {
  if (!language) {
    return undefined
  }

  const normalized = language.trim().toLowerCase()
  const alias = LANGUAGE_ALIASES[normalized]
  return alias ?? normalized
}

export function CodeHighlightViewer({ code, language, wrapLongLines = true }: CodeHighlightViewerProps) {
  const normalizedLanguage = useMemo(() => normalizeLanguage(language), [language])

  return (
    <SyntaxHighlighter
      language={normalizedLanguage}
      style={vscDarkPlus}
      customStyle={{ borderRadius: "12px", background: "rgb(15, 23, 42)" }}
      wrapLines
      wrapLongLines={wrapLongLines}
      showLineNumbers
    >
      {code}
    </SyntaxHighlighter>
  )
}
