import { useEffect, useState } from 'react'
import { api } from '../../api'
import type { UserProfile } from '../../types'

export function Settings() {
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [prefText, setPrefText] = useState('')

  useEffect(() => {
    api.profile().then((p) => {
      setProfile(p)
      setPrefText(p.preferences.join('\n'))
    }).catch((e) => setError(String(e)))
  }, [])

  const save = async () => {
    if (!profile) return
    setError(null); setSaved(false)
    try {
      const updated = await api.updateProfile({
        name: profile.name,
        timeZone: profile.timeZone,
        language: profile.language,
        preferences: prefText.split('\n').map((s) => s.trim()).filter(Boolean),
      })
      setProfile(updated)
      setPrefText(updated.preferences.join('\n'))
      setSaved(true)
    } catch (e) { setError(String(e)) }
  }

  if (!profile) return <div className="card">{error ? <div className="err">{error}</div> : 'Loading…'}</div>

  return (
    <div>
      <div className="page-head"><h1>Settings</h1><p>Your profile shapes how the assistant talks to you.</p></div>

      <div className="card grid" style={{ maxWidth: 520, gap: 14 }}>
        <div><label>What should I call you?</label>
          <input value={profile.name} onChange={(e) => setProfile({ ...profile, name: e.target.value })} /></div>
        <div><label>Timezone (IANA id, e.g. America/New_York)</label>
          <input value={profile.timeZone} onChange={(e) => setProfile({ ...profile, timeZone: e.target.value })} /></div>
        <div><label>Language code</label>
          <input value={profile.language} onChange={(e) => setProfile({ ...profile, language: e.target.value })} /></div>
        <div><label>Preferences (one per line)</label>
          <textarea value={prefText} onChange={(e) => setPrefText(e.target.value)}
            placeholder={'Keep answers concise\nUse 24-hour time'} style={{ minHeight: 90 }} /></div>

        {error && <div className="err">{error}</div>}
        <div className="row">
          <button className="btn primary" onClick={save}>Save</button>
          {saved && <span className="faint" style={{ color: 'var(--green)' }}>Saved ✓</span>}
        </div>
      </div>
    </div>
  )
}
