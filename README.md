RaceDay — Part 1: System Planning and Database
PROG6212 POE
System Description

RaceDay is a full-stack web-based event management system designed for the South African road running, walking, and cycling community. It allows event organisers to create and manage races and categories, and allows participants to browse events, enrol in categories, and track their personal results.

This part of the Portfolio of Evidence covers the planning phase of the system: the database design (Entity Relationship Diagram), the API endpoint plan, and the SQL script that creates and seeds the database. No application code is written in this part — planning is completed and approved before any API or MVC code is built in Parts 2 and 3.

Roles

RaceDay supports two distinct user roles:

Organiser — can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
Participant — can create an account, browse events, enter events by selecting a category, view their own enrolments, and track their personal results.

Role-based access will be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

Contents of /docs
File	Description
RaceDay_ERD.png	Entity Relationship Diagram — 6 entities (Roles, Users, Events, Categories, Enrolments, Results), showing all primary keys, foreign keys, and cardinalities.
RaceDay_API_Endpoint_Plan.md	Full table of every planned API endpoint, covering Authentication, User Profile, Events, Categories, Event Enrolments, and Results.
RaceDay_Database_Script.sql	SQL script that creates and seeds the RaceDay database schema in SQL Server (SSMS), matching the ERD exactly.
Design Note

The register endpoint in the API plan accepts a role field as part of its request body, while Roles is implemented as a separate lookup table (not a plain string column) in the ERD and SQL script. This is not a mismatch — the API resolves the submitted role name to the correct RoleId before inserting the new user. This design keeps roles extensible (e.g. adding an Admin role later) without changing the Users table structure.

CI/CD
<!-- Insert a screenshot of a successful (green) GitHub Actions build here before submission -->

![CI/CD Build Status](docs/ci-success-screenshot.png)

Video Walkthrough
<!-- Insert your unlisted YouTube link here -->

Video: [INSERT UNLISTED YOUTUBE LINK HERE]

The video walks through the planning documents, the ERD design decisions, the endpoint plan choices, and runs the SQL script live in SQL Server Management Studio (SSMS)
