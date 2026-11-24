export function startCommentSubscription({ grant, listen, onError, schedule = setTimeout, cancel = clearTimeout }) {
  const controller = new AbortController()
  let unsubscribe
  let renewal
  let pending = false
  let failed = false

  function fail(error) {
    if (controller.signal.aborted || failed) return
    failed = true
    cancel(renewal)
    unsubscribe?.()
    unsubscribe = undefined
    onError(error)
  }

  async function renew() {
    if (controller.signal.aborted || pending) return
    cancel(renewal)
    pending = true
    failed = false
    try {
      await grant(controller.signal)
      if (controller.signal.aborted || failed) return
      if (!unsubscribe) {
        const stop = listen(fail)
        if (failed || controller.signal.aborted) {
          stop()
          return
        }
        unsubscribe = stop
      }
      renewal = schedule(renew, 60_000)
    } catch (error) {
      fail(error)
    } finally {
      pending = false
    }
  }

  renew()
  return {
    renew,
    stop() {
      controller.abort()
      cancel(renewal)
      unsubscribe?.()
    },
  }
}
