import React from 'react'

const icons = {
  board: 'M3 5h18v14H3z M9 5v14 M15 5v14',
  folder: 'M3 7h7l2 2h9v11H3z',
  plus: 'M12 5v14 M5 12h14',
  arrow: 'M5 12h14 M13 6l6 6-6 6',
  back: 'M19 12H5 M11 18l-6-6 6-6',
  clock: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M12 7v5l3 2',
  user: 'M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0z M4 21v-2a5 5 0 0 1 5-5h6a5 5 0 0 1 5 5v2',
  chat: 'M21 11a8 8 0 0 1-8 8H8l-5 3V11a8 8 0 0 1 8-8h2a8 8 0 0 1 8 8z',
  logout: 'M10 4H4v16h6 M9 12h12 M17 8l4 4-4 4',
  search: 'M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0z M15 15l5 5',
}

export function Icon({ name }) {
  return <svg className="icon" width="20" height="20" viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round"
    aria-hidden="true" focusable="false"><path d={icons[name]} /></svg>
}

export function State({ children, loading = false }) {
  return <p className={'state' + (loading ? ' loading' : '')} role={loading ? 'status' : undefined}>{children}</p>
}
