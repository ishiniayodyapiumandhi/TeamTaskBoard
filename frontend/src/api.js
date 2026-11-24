export function createApi(user, base = import.meta.env.VITE_API_URL || 'https://localhost:7237') {
  return async (path, { method = 'GET', body, signal } = {}) => {
    const url = new URL(path, base)
    let token
    try {
      token = await user.getIdToken()
    } catch {
      throw new Error('Could not get your sign-in token. Check your connection or sign in again.')
    }
    let response
    try {
      response = await fetch(url, {
        method, signal,
        headers: { Authorization: 'Bearer ' + token,
          ...(body ? { 'Content-Type': 'application/json' } : {}) },
        body: body ? JSON.stringify(body) : undefined,
      })
    } catch (cause) {
      if (signal?.aborted) throw cause
      throw new Error('Could not reach the API at ' + url.origin + '. Check that it is running and inspect the browser Network tab for HTTPS or CORS errors.')
    }
    if (response.status === 204) return null
    const data = await response.json().catch(() => null)
    if (!response.ok) {
      const message = response.status >= 500 ? 'The API could not complete the request. Check the backend console.' :
        response.status === 401 ? 'The API rejected your session. Sign in again and retry.' :
          Object.values(data?.errors ?? {}).flat().join(' ') || data?.detail || data?.title || 'Request failed.'
      const trace = typeof data?.traceId === 'string' ? ' Reference: ' + data.traceId : ''
      throw new Error('HTTP ' + response.status + ': ' + message + trace)
    }
    return data
  }
}
