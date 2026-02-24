# Support Ticket Management API

## Support Ticket Management — Student Project Assignment

## Overview: 
-  Build a backend for a company helpdesk system where employees raise tickets, support staff handle them, and managers track everything. The API follows REST principles.

## Goals & Requirements:
-  Implement authentication (JWT) and secure password hashing (bcrypt).
-  Enforce Role-Based Access Control (MANAGER, SUPPORT, USER) at the API layer.
-  Implement the database schema with at least these tables: Users, Roles, Tickets, TicketComments, TicketStatusLogs.
-  Implement ticket lifecycle and logging: OPEN → IN_PROGRESS → RESOLVED → CLOSED, with each change recorded in TicketStatusLogs.
-  Validate inputs (title >=5 chars, description >=10 chars) and require enum values for status/priority.
-  Provide a Swagger UI for API exploration.
  
## Deliverables:
-  Working backend with routes for authentication, user management (MANAGER), ticket creation/viewing/assignment/status changes, and comments.
-  Swagger documentation accessible at /docs. (optional)
-  README with setup and run instructions.

## Grading checklist:
-  Correct RBAC enforcement and proper HTTP status codes (401/403/404/400/201/204).
-  Proper DB relationships and constraints.
-  Validation and status transition enforcement.
-  Secure password handling and JWT usage.
