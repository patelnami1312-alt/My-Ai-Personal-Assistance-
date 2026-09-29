import { useCallback, useEffect, useRef, useState } from 'react'
import { api } from '../api'
import { useVoice } from '../hooks/useVoice'
import type { ToolTraceEntry } from '../types'

interface UiMessage {
  role: 'user' | 'assistant'
  content: string
  trace?: ToolTraceEntry[]
}

export function Chat() {
  const [messages, setMessages] = useState<UiMessage[]>([])
  const [input, setInput] = useState('')
  const [conversationId, setConversationId] = useState<string | undefined>()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [voiceReplies, setVoiceReplies] = useState(true)
  const scrollRef = useRef<HTMLDivElement>(null)

  const send = useCallback(async (text: string) => {
    const trimmed = text.trim()
    if (!trimmed || busy) return
    setError(null)
    setInput('')
    setMessages((m) => [...m, { role: 'user', content: trimmed }])
    setBusy(true)
    try {
      const res = await api.sendMessage(trimmed, conversationId)
      setConversationId(res.conversationId)
      setMessages((m) => [...m, { role: 'assistant', content: res.reply, trace: res.toolTrace }])
      if (voiceReplies) speak(res.reply)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Request failed')
    } finally {
      setBusy(false)
    }
  }, [busy, conversationId, voiceReplies])

  const { supported, listening, interimTranscript, start, stop, speak, speaking, cancelSpeech } = useVoice(send)

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, busy])

  return (
    <div>
      <div className="page-head row between">
        <div>
          <h1>Chat</h1>
          <p>Plan trips, build itineraries, and track packing lists.</p>
        </div>
        <div className="row">
          {speaking && <button className="btn ghost" onClick={cancelSpeech}>Stop voice</button>}
          <label className="row" style={{ gap: 6, margin: 0, cursor: 'pointer' }}>
            <input type="checkbox" checked={voiceReplies} onChange={(e) => setVoiceReplies(e.target.checked)} style={{ width: 'auto' }} />
            <span className="faint">Speak replies</span>
          </label>
          <button className="btn ghost" onClick={() => { setMessages([]); setConversationId(undefined); cancelSpeech() }}>New chat</button>
        </div>
      </div>

      <div className="chat">
        <div className="chat-scroll" ref={scrollRef}>
          {messages.length === 0 && (
            <div className="empty">
              Try: "I'm planning a trip to Tokyo from next month" &middot; "Add a flight to my Paris trip" &middot;
              "What's left on my packing list?" &middot; "Add sunscreen to my packing list"
            </div>
          )}
          {messages.map((m, i) => (
            <div key={i} className={`msg ${m.role}`}>
              <div className="who">{m.role === 'user' ? 'You' : 'Travel Agent'}</div>
              <div className="bubble">{m.content}</div>
              {m.trace && m.trace.length > 0 && (
                <div className="trace">
                  {m.trace.map((t, j) => (
                    <div key={j}><code>{t.tool}</code>{t.isError ? ' x' : ' ok'}</div>
                  ))}
                </div>
              )}
            </div>
          ))}
          {listening && (
            <div className="msg user">
              <div className="who">You (listening...)</div>
              <div className="bubble">{interimTranscript || '...'}</div>
            </div>
          )}
          {busy && (
            <div className="msg assistant">
              <div className="who">Travel Agent</div>
              <div className="bubble"><span className="spinner" /> thinking...</div>
            </div>
          )}
        </div>

        <div>
          {error && <div className="err" style={{ marginBottom: 10 }}>{error}</div>}
          <div className="composer">
            <button
              className={`mic${listening ? ' on' : ''}`}
              title={supported ? 'Tap to talk' : 'Voice not supported in this browser'}
              disabled={!supported || busy}
              onClick={() => (listening ? stop() : start())}
            >
              {listening ? '■' : '🎤'}
            </button>
            <textarea
              value={input}
              placeholder="Message your travel agent..."
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault()
                  send(input)
                }
              }}
            />
            <button className="btn primary" disabled={busy || !input.trim()} onClick={() => send(input)}>Send</button>
          </div>
          {!supported && <div className="faint" style={{ marginTop: 6 }}>Voice input needs Chrome or Edge. Text works everywhere.</div>}
        </div>
      </div>
    </div>
  )
}
