export const statuses = ['To Do', 'In Progress', 'Done']
export const priorities = ['Low', 'Medium', 'High']

export function taskPayload(form, existing, today = new Date().toISOString().slice(0, 10)) {
  const title = form.title.trim()
  if (!title || title.length > 200) throw new Error('Enter a title of 1–200 characters.')
  if (!statuses.includes(form.status) || !priorities.includes(form.priority)) throw new Error('Select a valid status and priority.')
  if (form.dueDate && form.dueDate < today && form.dueDate !== existing?.dueDate) {
    throw new Error('Choose a due date today or later (UTC).')
  }
  const assigneeId = form.assigneeId ? Number(form.assigneeId) : null
  if (assigneeId !== null && (!Number.isInteger(assigneeId) || assigneeId < 1 || assigneeId > 2147483647)) {
    throw new Error('Enter a valid assignee ID.')
  }
  return { title, description: form.description || null, status: form.status, priority: form.priority,
    dueDate: form.dueDate || null, assigneeId }
}
