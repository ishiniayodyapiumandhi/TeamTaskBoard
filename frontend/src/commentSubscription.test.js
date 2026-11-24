import { test } from 'node:test'
import assert from 'node:assert/strict'
import { startCommentSubscription } from './commentSubscription.js'

test('renews the grant without replacing the listener, and reconnects after a terminal error', async () => {
  let grants = 0
  let listeners = 0
  let stops = 0
  let fail
  let scheduled
  const errors = []
  const subscription = startCommentSubscription({
    grant: async () => { grants++ },
    listen: handler => {
      fail = handler
      listeners++
      return () => { stops++ }
    },
    onError: error => errors.push(error.message),
    schedule: (callback, delay) => {
      assert.equal(delay, 60_000)
      scheduled = callback
    },
    cancel: () => { scheduled = undefined },
  })
  await Promise.resolve()
  await scheduled()
  assert.equal(grants, 2)
  assert.equal(listeners, 1)
  fail(new Error('permission denied'))
  assert.equal(stops, 1)
  assert.deepEqual(errors, ['permission denied'])
  await subscription.renew()
  assert.equal(grants, 3)
  assert.equal(listeners, 2)
  subscription.stop()
  assert.equal(stops, 2)
  assert.equal(scheduled, undefined)
})

test('closing details during a pending grant does not attach a listener', async () => {
  let signal
  let complete
  const subscription = startCommentSubscription({
    grant: value => {
      signal = value
      return new Promise(resolve => { complete = resolve })
    },
    listen: () => assert.fail('Unexpected listener after closing details'),
    onError: () => assert.fail('Unexpected error after closing details'),
  })
  subscription.stop()
  assert.equal(signal.aborted, true)
  complete()
  await Promise.resolve()
})
