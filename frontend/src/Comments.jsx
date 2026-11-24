import React, { useEffect, useState } from 'react'
import { addDoc, collection, limit, onSnapshot, orderBy, query, serverTimestamp } from 'firebase/firestore'
import { firestore } from './firebase'
import { startCommentSubscription } from './commentSubscription'
import { Icon, State } from './ui'

export default function Comments({ api, taskId, user }) {
  const [state, setState] = useState({ loading: true })
  const [attempt, retry] = useState(0)
  const [draft, setDraft] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const path = '/api/tasks/' + taskId + '/comment-access'

  useEffect(() => {
    const comments = collection(firestore, 'taskAccess', String(taskId), 'comments')
    setState({ loading: true })
    const subscription = startCommentSubscription({
      grant: signal => api(path, { method: 'POST', signal }),
      listen: fail => onSnapshot(query(comments, orderBy('createdAt', 'desc'), limit(100)),
        snapshot => setState({ items: snapshot.docs.map(doc => ({ id: doc.id, ...doc.data() })) }), fail),
      onError: cause => setState({ error: cause.message }),
    })
    function resume() {
      if (document.visibilityState === 'visible') subscription.renew()
    }
    document.addEventListener('visibilitychange', resume)
    window.addEventListener('online', resume)
    return () => {
      document.removeEventListener('visibilitychange', resume)
      window.removeEventListener('online', resume)
      subscription.stop()
    }
  }, [api, path, taskId, attempt])

  async function submit(event) {
    event.preventDefault()
    const text = draft.trim()
    if (!text) return
    setBusy(true); setError('')
    try {
      await addDoc(collection(firestore, 'taskAccess', String(taskId), 'comments'),
        { text, authorUid: user.uid, createdAt: serverTimestamp() })
      setDraft('')
    } catch (cause) { setError(cause.message) }
    finally { setBusy(false) }
  }

  return <section className="comments" aria-label="Comments">
    <div className="section-heading"><span className="symbol"><Icon name="chat" /></span><h3>Comments</h3></div>
    {state.loading && <State loading>Connecting to comments…</State>}
    {state.error && <p role="alert">{state.error} <button className="secondary" onClick={() => retry(value => value + 1)}>Retry</button></p>}
    {state.items && <>
      {!state.items.length && <State>No comments yet. Start the conversation.</State>}
      {state.items.length > 0 && <ul className="comment-list">{state.items.map(comment => <li key={comment.id}>
        <span className="symbol"><Icon name="user" /></span>
        <div className="bubble"><div className="comment-meta"><strong>{comment.authorUid === user.uid ? 'You' : 'Author'}</strong>
          {comment.createdAt && <time dateTime={comment.createdAt.toDate().toISOString()}>{comment.createdAt.toDate().toLocaleString()}</time>}</div>
          <p>{comment.text}</p></div>
      </li>)}</ul>}
      <form className="composer" onSubmit={submit}>
        <label>New comment<textarea maxLength={2000} rows={3} placeholder="Write a comment…" required value={draft} onChange={event => setDraft(event.target.value)} /></label>
        {error && <p role="alert">{error}</p>}
        <button disabled={busy || !draft.trim()}>{busy ? 'Posting…' : 'Post comment'}<Icon name="arrow" /></button>
      </form>
    </>}
  </section>
}
