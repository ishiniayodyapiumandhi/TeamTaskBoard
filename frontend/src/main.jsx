import React, { useEffect, useMemo, useState } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate, useParams } from 'react-router-dom'
import { createUserWithEmailAndPassword, onAuthStateChanged, signInWithEmailAndPassword, signOut } from 'firebase/auth'
import { auth, configured } from './firebase'
import { createApi } from './api'
import Board from './Board'
import { useResource } from './useResource'
import { Icon, State } from './ui'
import './style.css'

function Credentials({ signup = false }) {
  const navigate = useNavigate()
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  async function submit(event) {
    event.preventDefault()
    const values = new FormData(event.currentTarget)
    setBusy(true); setError('')
    try {
      await (signup ? createUserWithEmailAndPassword : signInWithEmailAndPassword)(
        auth, values.get('email'), values.get('password'))
      navigate('/projects', { replace: true })
    } catch (cause) {
      setError(cause.code === 'auth/invalid-credential' ? 'Email or password is incorrect.' : cause.message)
    } finally { setBusy(false) }
  }
  return <section className="panel auth"><span className="symbol"><Icon name="board" /></span>
    <p className="eyebrow">Your workspace</p><h1>{signup ? 'Create your account' : 'Welcome back'}</h1>
    <p className="muted">{signup ? 'Start organizing projects and tracking tasks in one place.' : 'Sign in to pick up where you left off.'}</p>
    <form onSubmit={submit}>
      <label>Email address<input name="email" type="email" autoComplete="email" placeholder="you@example.com" required /></label>
      <label>Password<input name="password" type="password" minLength={signup ? 6 : undefined}
        autoComplete={signup ? 'new-password' : 'current-password'} required /></label>
      {error && <p role="alert">{error}</p>}
      <button disabled={busy}>{busy ? 'Please wait…' : signup ? 'Sign up' : 'Log in'}</button>
    </form>
    <Link to={signup ? '/login' : '/signup'}>{signup ? 'Already have an account?' : 'Create an account'}</Link>
  </section>
}

function Projects({ api }) {
  const [revision, refresh] = useState(0)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const projects = useResource(api, '/api/projects', revision)
  const report = useResource(api, '/api/reports/overdue-by-user', revision)
  async function create(event) {
    event.preventDefault()
    const form = event.currentTarget
    const values = Object.fromEntries(new FormData(form))
    if (!values.name.trim()) { setError('Enter a project name.'); return }
    setBusy(true); setError('')
    try { await api('/api/projects', { method: 'POST', body: values }); form.reset(); refresh(value => value + 1) }
    catch (cause) { setError(cause.message) }
    finally { setBusy(false) }
  }
  return <>
    <div className="page-heading"><p className="eyebrow">Workspace overview</p><h1>Your projects</h1>
      <p className="muted">Open a board, start a project, or see what needs attention.</p></div>
    <div className="dashboard">
      <section className="project-list" aria-labelledby="project-list-title">
        <div className="section-heading"><h2 id="project-list-title">Project boards</h2>
          {projects.data && <span className="count">{projects.data.length}</span>}</div>
        {projects.loading && <State loading>Loading projects…</State>}
        {projects.error && <p role="alert">{projects.error} <button className="secondary" onClick={() => refresh(value => value + 1)}>Retry</button></p>}
        {projects.data?.length === 0 && <State>No projects yet. Create a project to get started.</State>}
        {projects.data?.length > 0 && <ul className="tiles">{projects.data.map(project => <li className="panel project-card" key={project.id}>
          <span className="symbol"><Icon name="folder" /></span>
          <h3><Link to={'/projects/' + project.id}>{project.name}</Link></h3>
          <p>{project.description || 'No description yet.'}</p>
          <Link className="card-link" to={'/projects/' + project.id} aria-label={'Open board: ' + project.name}>Open board <Icon name="arrow" /></Link>
        </li>)}</ul>}
      </section>
      <aside className="dashboard-side" aria-label="Project tools">
    <form className="panel create-project" onSubmit={create}>
      <div className="section-heading"><span className="symbol"><Icon name="plus" /></span><h2>New project</h2></div>
      <label>Name<input name="name" required maxLength={150} /></label>
      <label>Description<textarea name="description" maxLength={4000} /></label>
      {error && <p role="alert">{error}</p>}
      <button disabled={busy}><Icon name="plus" />{busy ? 'Creating…' : 'Create project'}</button>
    </form>
    <section className="panel report"><div className="section-heading"><span className="symbol warning"><Icon name="clock" /></span><h2>Overdue tasks</h2></div>
      <p className="muted">Unfinished tasks past their due date, grouped by assignee.</p>
      {report.loading && <State loading>Loading report…</State>}
      {report.error && <p role="alert">{report.error}</p>}
      {report.data?.length === 0 && <State>No assigned overdue tasks.</State>}
      {report.data?.length > 0 && <ul className="report-list">{report.data.map(row => <li key={row.userId}>
        <span>{row.name}</span><span className="badge overdue">{row.count} overdue</span>
      </li>)}</ul>}
    </section>
      </aside>
    </div>
  </>
}

function ProjectBoard(props) {
  const { id } = useParams()
  return <Board key={id} id={id} {...props} />
}

function App() {
  const [session, setSession] = useState({ loading: true })
  const [error, setError] = useState('')
  useEffect(() => configured ? onAuthStateChanged(auth, user => setSession({ user })) : undefined, [])
  const user = session.user
  const api = useMemo(() => user && createApi(user), [user])
  if (!configured) return <main><p role="alert">Firebase is not configured. Copy frontend/.env.example to frontend/.env.local and fill in the Web App values.</p></main>
  if (session.loading) return <main><State loading>Checking session…</State></main>
  return <>
    <header className="topbar"><div className="topbar-inner"><Link className="brand" to="/projects">
      <span className="brand-symbol"><Icon name="board" /></span><span>TeamFlow</span></Link>{user && <nav aria-label="Workspace">
      <Link className="nav-link" to="/projects">Projects</Link><button className="logout" onClick={() => {
      setError(''); signOut(auth).catch(cause => setError(cause.message))
    }}><Icon name="logout" />Log out</button></nav>}</div></header>
    <main className={user ? 'workspace' : 'auth-page'} key={user?.uid || 'guest'}>
      {error && <p role="alert">{error}</p>}
      <Routes>
        <Route path="/login" element={user ? <Navigate to="/projects" replace /> : <Credentials key="login" />} />
        <Route path="/signup" element={user ? <Navigate to="/projects" replace /> : <Credentials key="signup" signup />} />
        <Route path="/projects" element={user ? <Projects api={api} /> : <Navigate to="/login" replace />} />
        <Route path="/projects/:id" element={user ? <ProjectBoard api={api} user={user} /> : <Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/projects" replace />} />
      </Routes>
    </main>
  </>
}

createRoot(document.getElementById('root')).render(<BrowserRouter><App /></BrowserRouter>)
