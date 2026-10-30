import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { WeatherNoteForm } from './WeatherNoteForm'

function jsonResponse(body: unknown, status: number) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('WeatherNoteForm', () => {
  it('posts the trimmed note and shows a success message', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(jsonResponse({ id: 1, cityName: 'Jakarta' }, 201))
    const user = userEvent.setup()
    render(<WeatherNoteForm cityName="Jakarta" />)

    await user.type(screen.getByLabelText('Your note'), '  Bring an umbrella  ')
    await user.click(screen.getByRole('button', { name: 'Save Note' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Note saved for Jakarta.')
    expect(screen.getByLabelText('Your note')).toHaveValue('')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/weather/notes')
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual({ cityName: 'Jakarta', note: 'Bring an umbrella' })
  })

  it('shows validation errors from the API and keeps the note', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse({ title: 'Validation failed', errors: { Note: ['Note is required.'] } }, 400),
    )
    const user = userEvent.setup()
    render(<WeatherNoteForm cityName="Jakarta" />)

    await user.type(screen.getByLabelText('Your note'), ' ')
    await user.click(screen.getByRole('button', { name: 'Save Note' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Note is required.')
    expect(screen.getByLabelText('Your note')).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows a friendly message when the service is unavailable', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({ title: 'Service Unavailable' }, 503))
    const user = userEvent.setup()
    render(<WeatherNoteForm cityName="Jakarta" />)

    await user.type(screen.getByLabelText('Your note'), 'Hello')
    await user.click(screen.getByRole('button', { name: 'Save Note' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('temporarily unavailable')
  })

  it('shows a network error message when the request fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))
    const user = userEvent.setup()
    render(<WeatherNoteForm cityName="Jakarta" />)

    await user.type(screen.getByLabelText('Your note'), 'Hello')
    await user.click(screen.getByRole('button', { name: 'Save Note' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to reach the server.')
  })

  it('disables the button while saving', async () => {
    let resolve: (value: Response) => void = () => {}
    vi.spyOn(globalThis, 'fetch').mockReturnValue(new Promise<Response>((r) => (resolve = r)))
    const user = userEvent.setup()
    render(<WeatherNoteForm cityName="Jakarta" />)

    await user.type(screen.getByLabelText('Your note'), 'Hello')
    await user.click(screen.getByRole('button', { name: 'Save Note' }))

    expect(screen.getByRole('button', { name: /Saving/ })).toBeDisabled()

    resolve(jsonResponse({ id: 1, cityName: 'Jakarta' }, 201))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Save Note' })).toBeEnabled())
  })
})
