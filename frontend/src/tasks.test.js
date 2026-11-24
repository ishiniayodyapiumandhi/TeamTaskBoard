import { test } from 'node:test'
import assert from 'node:assert/strict'
import { taskPayload } from './tasks.js'

const form = { title: ' Review ', priority: 'Low', status: 'To Do', dueDate: '2026-11-18' }
test('rejects a new past due date', () => {
  assert.throws(() => taskPayload(form, null, '2026-11-19'), /due date/)
})
test('allows moving an existing overdue task', () => {
  assert.equal(taskPayload({ ...form, status: 'Done' }, form, '2026-11-19').title, 'Review')
})
test('rejects invalid titles and priorities', () => {
  assert.throws(() => taskPayload({ ...form, title: ' ' }), /title/)
  assert.throws(() => taskPayload({ ...form, priority: 'Urgent' }), /priority/)
})
