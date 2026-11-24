import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { useResource } from './useResource'
import { priorities, statuses, taskPayload } from './tasks'
import Comments from './Comments'
import { Icon, State } from './ui'

const columnStyles = { 'To Do': 'todo', 'In Progress': 'progress', Done: 'done' }

function TaskMeta({ task }) {
  const overdue = task.status !== 'Done' && task.dueDate && task.dueDate < new Date().toISOString().slice(0, 10)
  return <div className="task-meta">
    <span className={'badge priority-' + task.priority.toLowerCase()}>{task.priority} priority</span>
    <span className={overdue ? 'badge overdue' : 'meta-item'}><Icon name="clock" />
      {task.dueDate ? <>{overdue ? 'Overdue · ' : 'Due '}<time dateTime={task.dueDate}>{task.dueDate}</time></> : 'No due date'}
    </span>
    <span className="meta-item"><Icon name="user" />{task.assigneeId ? 'Assignee #' + task.assigneeId : 'Unassigned'}</span>
  </div>
}

function Column({ api, id, status, filters, revision, edit, view }) {
  const [page, setPage] = useState(1)
  const query = new URLSearchParams({ ...filters, status, page, pageSize: 10 })
  const { data, loading, error } = useResource(api, '/api/projects/' + id + '/tasks?' + query, revision)
  return <section className={'column ' + columnStyles[status]}>
    <div className="section-heading column-heading"><span className="status-dot" /><h2>{status}</h2>
      {data && <span className="count" aria-label={data.totalCount + ' tasks'}>{data.totalCount}</span>}</div>
    {loading && <State loading>Loading tasks…</State>}
    {error && <p role="alert">{error}</p>}
    {data?.items.length === 0 && <State>No tasks on this page.</State>}
    {data?.items.map(task => <article className="panel task-card" key={task.id}>
      <h3>{task.title}</h3><TaskMeta task={task} />
      <div className="actions card-actions"><button className="secondary" onClick={() => view({ ...task })}><Icon name="chat" />Details</button>
        <button className="secondary" onClick={() => edit({ ...task })}>Edit / move</button></div>
    </article>)}
    {(page > 1 || data?.totalCount > 10) && <div className="actions pagination">
      <button className="secondary" disabled={loading || page === 1} onClick={() => setPage(page - 1)}>Previous</button>
      <span>Page {page}</span>
      <button className="secondary" disabled={!data || page * 10 >= data.totalCount} onClick={() => setPage(page + 1)}>Next</button>
    </div>}
  </section>
}

function TaskForm({ task, save, remove, close, busy, error }) {
  return <section className="panel editor">
    <p className="eyebrow">Task settings</p>
    <h2>{task.id ? 'Edit or move task' : 'Create task'}</h2>
    <form onSubmit={event => { event.preventDefault(); save(Object.fromEntries(new FormData(event.currentTarget))) }}>
      <label>Title<input name="title" defaultValue={task.title} required maxLength={200} /></label>
      <label>Description<textarea name="description" defaultValue={task.description ?? ''} maxLength={4000} /></label>
      <div className="form-grid">
        <label>Status<select name="status" defaultValue={task.status}>{statuses.map(item => <option key={item}>{item}</option>)}</select></label>
        <label>Priority<select name="priority" defaultValue={task.priority}>{priorities.map(item => <option key={item}>{item}</option>)}</select></label>
        <label>Due date (UTC)<input name="dueDate" type="date" defaultValue={task.dueDate ?? ''} /></label>
        <label>Assignee ID<input name="assigneeId" type="number" min={1} max={2147483647} defaultValue={task.assigneeId ?? ''} /></label>
      </div>
      <p className="field-help">Leave the assignee ID blank to keep the task unassigned.</p>
      {error && <p role="alert">{error}</p>}
      <div className="actions"><button disabled={busy}>{busy ? 'Working…' : 'Save task'}</button>
        <button className="secondary" type="button" disabled={busy} onClick={close}>Cancel</button>
        {task.id && <button className="danger" type="button" disabled={busy} onClick={remove}>Delete task</button>}</div>
    </form>
  </section>
}

export default function Board({ api, id, user }) {
  const [filters, setFilters] = useState({ priority: '', search: '' })
  const [revision, refresh] = useState(0)
  const [editing, edit] = useState(null)
  const [selected, view] = useState(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const project = useResource(api, '/api/projects/' + id)
  function open(task) { setError(''); view(null); edit(task) }
  async function mutate(method, form) {
    setBusy(true); setError('')
    try {
      await api(editing.id ? '/api/tasks/' + editing.id : '/api/projects/' + id + '/tasks',
        { method, body: form ? taskPayload(form, editing) : undefined })
      edit(null); view(null); refresh(value => value + 1)
    } catch (cause) { setError(cause.message) }
    finally { setBusy(false) }
  }
  return <>
    <Link className="back-link" to="/projects"><Icon name="back" />All projects</Link>
    {project.loading && <State loading>Loading project…</State>}
    {project.error && <p role="alert">{project.error}</p>}
    {project.data && <>
      <div className="page-heading board-heading">
        <div><p className="eyebrow">Project board</p><h1>{project.data.name}</h1>
          {project.data.description && <p className="muted">{project.data.description}</p>}</div>
        <button type="button" disabled={busy} onClick={() => open({ status: 'To Do', priority: 'Medium' })}><Icon name="plus" />New task</button>
      </div>
      <form className="actions panel filters" onSubmit={event => {
        event.preventDefault(); setFilters(Object.fromEntries(new FormData(event.currentTarget))); refresh(value => value + 1)
      }}>
        <label className="search-field">Search by title<span className="input-icon"><Icon name="search" /><input name="search" type="search" maxLength={200} placeholder="Find a task…" /></span></label>
        <label>Priority<select name="priority"><option value="">All priorities</option>
          {priorities.map(item => <option key={item}>{item}</option>)}</select></label>
        <button className="secondary">Apply filters</button>
      </form>
      {editing && <TaskForm key={editing.id ?? 'new'} task={editing} busy={busy} error={error}
        save={form => mutate(editing.id ? 'PUT' : 'POST', form)}
        remove={() => { if (window.confirm('Delete this task?')) mutate('DELETE') }} close={() => edit(null)} />}
      {selected && <section className="panel editor task-details">
        <div className="section-heading details-heading"><div><p className="eyebrow">Task details · {selected.status}</p>
          <h2>{selected.title}</h2></div><button className="secondary" onClick={() => view(null)}>Close details</button></div>
        <p className="description">{selected.description || 'No description.'}</p>
        <TaskMeta task={selected} />
        <Comments key={selected.id} api={api} taskId={selected.id} user={user} />
      </section>}
      <div className="board">{statuses.map(status => <Column key={status + revision} api={api} id={id}
        status={status} filters={filters} revision={revision} edit={task => { if (!busy) open(task) }}
        view={task => { if (!busy) { edit(null); view(task) } }} />)}</div>
    </>}
  </>
}
