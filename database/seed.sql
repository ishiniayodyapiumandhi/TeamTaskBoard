
USE team_task_board;

-- Sample users for local development only.
-- These Firebase UIDs are placeholders, not real accounts.
INSERT INTO users (firebase_uid, name, email)
VALUES
('demo-user-001', 'Demo Manager', 'manager@example.com'),
('demo-user-002', 'Demo Developer', 'developer@example.com');

-- Sample project
INSERT INTO projects (owner_id, name, description)
SELECT id, 'Team Task Board',
       'Sample project for testing task management'
FROM users
WHERE firebase_uid = 'demo-user-001';

-- Sample tasks
INSERT INTO tasks
(project_id, assignee_id, title, description,
 status, priority, due_date)
SELECT p.id, u.id,
       'Design Login Page',
       'Create the login interface using React',
       'To Do', 'High', DATE_ADD(CURDATE(), INTERVAL 5 DAY)
FROM projects p
JOIN users u ON u.firebase_uid = 'demo-user-002'
WHERE p.name = 'Team Task Board'
AND p.owner_id = (
    SELECT id FROM users
    WHERE firebase_uid = 'demo-user-001'
);

INSERT INTO tasks
(project_id, assignee_id, title, description,
 status, priority, due_date)
SELECT p.id, u.id,
       'Implement Backend API',
       'Build REST endpoints using ASP.NET Core',
       'In Progress', 'High', DATE_ADD(CURDATE(), INTERVAL 7 DAY)
FROM projects p
JOIN users u ON u.firebase_uid = 'demo-user-002'
WHERE p.name = 'Team Task Board'
AND p.owner_id = (
    SELECT id FROM users
    WHERE firebase_uid = 'demo-user-001'
);

INSERT INTO tasks
(project_id, assignee_id, title, description,
 status, priority, due_date)
SELECT p.id, u.id,
       'Database Schema',
       'Create MySQL tables and relationships',
       'Done', 'Medium', DATE_ADD(CURDATE(), INTERVAL 2 DAY)
FROM projects p
JOIN users u ON u.firebase_uid = 'demo-user-001'
WHERE p.name = 'Team Task Board'
AND p.owner_id = u.id;
