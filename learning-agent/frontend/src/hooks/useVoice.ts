import { useCallback, useEffect, useRef, useState } from 'react'

/* Minimal typings for the Web Speech API (not in lib.dom for all TS versions). */
interface SpeechRecognitionResultLike {
  0: { transcript: string }
  isFinal: boolean
}
interface SpeechRecognitionEventLike {
  resultIndex: number
  results: { length: number; [i: number]: SpeechRecognitionResultLike }
}
interface SpeechRecognitionLike {
  lang: string
  continuous: boolean
  interimResults: boolean
  start(): void
  stop(): void
  abort(): void
  onresult: ((e: SpeechRecognitionEventLike) => void) | null
  onerror: ((e: { error: string }) => void) | null
  onend: (() => void) | null
}
type SpeechRecognitionCtor = new () => SpeechRecognitionLike

function getRecognitionCtor(): SpeechRecognitionCtor | null {
  const w = window as unknown as {
    SpeechRecognition?: SpeechRecognitionCtor
    webkitSpeechRecognition?: SpeechRecognitionCtor
  }
  return w.SpeechRecognition ?? w.webkitSpeechRecognition ?? null
}

export interface UseVoice {
  supported: boolean
  listening: boolean
  interimTranscript: string
  start: () => void
  stop: () => void
  speak: (text: string) => void
  cancelSpeech: () => void
  speaking: boolean
}

/**
 * Browser-only speech: SpeechRecognition for input, speechSynthesis for output.
 * `onFinal` fires once with the completed transcript when the user stops talking.
 */
export function useVoice(onFinal: (text: string) => void, lang = 'en-US'): UseVoice {
  const ctor = getRecognitionCtor()
  const supported = ctor !== null && 'speechSynthesis' in window
  const recRef = useRef<SpeechRecognitionLike | null>(null)
  const finalRef = useRef('')
  const onFinalRef = useRef(onFinal)
  onFinalRef.current = onFinal

  const [listening, setListening] = useState(false)
  const [interimTranscript, setInterim] = useState('')
  const [speaking, setSpeaking] = useState(false)

  useEffect(() => {
    if (!ctor) return
    const rec = new ctor()
    rec.lang = lang
    rec.continuous = false
    rec.interimResults = true

    rec.onresult = (e) => {
      let interim = ''
      for (let i = e.resultIndex; i < e.results.length; i++) {
        const r = e.results[i]
        if (r.isFinal) finalRef.current += r[0].transcript
        else interim += r[0].transcript
      }
      setInterim(interim)
    }
    rec.onerror = () => setListening(false)
    rec.onend = () => {
      setListening(false)
      setInterim('')
      const text = finalRef.current.trim()
      finalRef.current = ''
      if (text) onFinalRef.current(text)
    }
    recRef.current = rec
    return () => {
      rec.onresult = rec.onerror = rec.onend = null
      rec.abort()
    }
  }, [ctor, lang])

  const start = useCallback(() => {
    if (!recRef.current || listening) return
    finalRef.current = ''
    setInterim('')
    try {
      recRef.current.start()
      setListening(true)
    } catch {
      /* start() throws if already started — ignore */
    }
  }, [listening])

  const stop = useCallback(() => {
    recRef.current?.stop()
  }, [])

  const speak = useCallback((text: string) => {
    if (!('speechSynthesis' in window) || !text) return
    window.speechSynthesis.cancel()
    const u = new SpeechSynthesisUtterance(text)
    u.lang = lang
    u.rate = 1.02
    u.onstart = () => setSpeaking(true)
    u.onend = () => setSpeaking(false)
    u.onerror = () => setSpeaking(false)
    window.speechSynthesis.speak(u)
  }, [lang])

  const cancelSpeech = useCallback(() => {
    if ('speechSynthesis' in window) window.speechSynthesis.cancel()
    setSpeaking(false)
  }, [])

  return { supported, listening, interimTranscript, start, stop, speak, cancelSpeech, speaking }
}
