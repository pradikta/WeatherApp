import { useEffect, useState } from 'react'
import { ApiError } from '../services/weatherApi'

export interface AsyncState<T> {
  data: T | null
  loading: boolean
  error: string | null
}

const idle = { data: null, loading: false, error: null }

/** Runs an abortable async loader whenever `key` or `load` changes; a null key keeps the state idle. `load` must be memoized. */
export function useAsync<T>(key: string | null, load: (signal: AbortSignal) => Promise<T>): AsyncState<T> {
  const [state, setState] = useState<{ key: string | null; value: AsyncState<T> }>({
    key: null,
    value: idle,
  })

  useEffect(() => {
    if (key === null) {
      return
    }

    const controller = new AbortController()
    load(controller.signal)
      .then((data) => setState({ key, value: { data, loading: false, error: null } }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          return
        }
        const message = error instanceof ApiError ? error.message : 'Unable to reach the server.'
        setState({ key, value: { data: null, loading: false, error: message } })
      })

    return () => controller.abort()
  }, [key, load])

  if (key === null) {
    return idle
  }
  // Until the request for the current key settles, report loading instead of the previous result.
  return state.key === key ? state.value : { data: null, loading: true, error: null }
}
