import { test } from 'node:test'
import assert from 'node:assert/strict'
import { createApi } from './api.js'

const base = 'https://localhost:7237'
const user = { getIdToken: async () => 'test-token' }

test('project POST sends JSON and a Firebase bearer header', async context => {
  context.mock.method(globalThis, 'fetch', async (url, options) => {
    assert.equal(url.href, base + '/api/projects')
    assert.equal(options.method, 'POST')
    assert.equal(options.headers.Authorization, 'Bearer test-token')
    assert.equal(options.headers['Content-Type'], 'application/json')
    assert.deepEqual(JSON.parse(options.body), { name: 'Demo' })
    return Response.json({ id: 1 }, { status: 201 })
  })
  assert.deepEqual(await createApi(user, base)('/api/projects', { method: 'POST', body: { name: 'Demo' } }), { id: 1 })
})

test('reports status and trace reference without displaying server exception details', async context => {
  context.mock.method(globalThis, 'fetch', async () =>
    Response.json({ detail: 'private exception', traceId: 'test-trace' }, { status: 500 }))
  await assert.rejects(createApi(user, base)('/api/projects'), error =>
    /HTTP 500.*test-trace/.test(error.message) && !error.message.includes('private exception'))
})

test('retains actionable validation errors', async context => {
  context.mock.method(globalThis, 'fetch', async () =>
    Response.json({ errors: { Name: ['Enter a project name.'] } }, { status: 400 }))
  await assert.rejects(createApi(user, base)('/api/projects'), /HTTP 400: Enter a project name/)
})
