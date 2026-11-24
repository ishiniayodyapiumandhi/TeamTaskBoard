import { useEffect, useState } from 'react'

export function useResource(api, path, revision = 0) {
  const [state, setState] = useState({})
  useEffect(() => {
    const controller = new AbortController()
    api(path, { signal: controller.signal }).then(data => {
      if (!controller.signal.aborted) setState({ path, revision, data })
    }).catch(error => {
      if (!controller.signal.aborted) setState({ path, revision, error: error.message })
    })
    return () => controller.abort()
  }, [api, path, revision])
  return state.path === path && state.revision === revision ? state : { loading: true }
}
