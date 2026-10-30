import { useId, useState, type FormEvent } from 'react'
import { ApiError, weatherApi } from '../services/weatherApi'

interface WeatherNoteFormProps {
  cityName: string
}

type Feedback = { kind: 'success' | 'error'; messages: string[] } | null

const maxLength = 1000

export function WeatherNoteForm({ cityName }: WeatherNoteFormProps) {
  const [note, setNote] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [feedback, setFeedback] = useState<Feedback>(null)
  const noteId = useId()
  const feedbackId = useId()

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setFeedback(null)

    try {
      const created = await weatherApi.createNote({ cityName, note: note.trim() })
      setNote('')
      setFeedback({ kind: 'success', messages: [`Note saved for ${created.cityName}.`] })
    } catch (error) {
      if (error instanceof ApiError) {
        const messages = error.fieldErrors.length > 0 ? error.fieldErrors : [error.message]
        setFeedback({ kind: 'error', messages })
      } else {
        setFeedback({ kind: 'error', messages: ['Unable to reach the server.'] })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="rounded-2xl bg-white p-4 shadow-sm">
      <h2 className="text-sm font-semibold text-slate-900">Save a weather note</h2>
      <p className="mt-3 text-xs text-slate-600">Add a personal note about the selected city.</p>

      <label htmlFor={noteId} className="mt-3 mb-1.5 block text-xs font-medium text-slate-600">
        Your note
      </label>
      <textarea
        id={noteId}
        value={note}
        maxLength={maxLength}
        rows={3}
        placeholder="e.g. Hot afternoon, bring an umbrella..."
        aria-invalid={feedback?.kind === 'error'}
        aria-describedby={feedback ? feedbackId : undefined}
        onChange={(event) => setNote(event.target.value)}
        className="w-full resize-y rounded-xl border border-slate-500 px-4 py-3 text-sm outline-none transition placeholder:text-slate-500 focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
      />

      <button
        type="submit"
        disabled={submitting}
        className="mt-3 w-full rounded-xl bg-blue-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {submitting ? 'Saving…' : 'Save Note'}
      </button>

      {feedback && (
        <div
          id={feedbackId}
          role={feedback.kind === 'error' ? 'alert' : 'status'}
          className={`mt-3 rounded-xl px-4 py-3 text-sm ${
            feedback.kind === 'success' ? 'bg-emerald-50 text-emerald-800' : 'bg-red-50 text-red-700'
          }`}
        >
          {feedback.messages.map((message) => (
            <p key={message}>{message}</p>
          ))}
        </div>
      )}
    </form>
  )
}
