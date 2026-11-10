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

<img width="1911" height="902" alt="image" src="https://github.com/user-attachments/assets/318c7f37-614c-4610-b054-b3fb6ac8a39f" />

Video Walkthrough
https://youtu.be/xwXTqeLnMao 

The video walks through the planning documents, the ERD design decisions, the endpoint plan choices, and runs the SQL script live in SQL Server Management Studio (SSMS)


Part 2 - Build and Test API

<img width="1873" height="648" alt="ci-green-build" src="https://github.com/user-attachments/assets/b74da17b-dff4-4098-a5ba-c467629f4631" />


Part 3
# RaceDay (PROG6212 POE)

RaceDay is an event management system for South African road running, walking and cycling events.

- **Part2/RaceDay.Api**: ASP.NET Core Web API (JWT authentication, SQL Server, Azure Blob Storage for images)
- **Part3/RaceDay.Web**: ASP.NET Core MVC front end that consumes the API (session-based login, Organiser and Participant interfaces)

## Features
- Register and log in as an Organiser (needs the organiser code) or a Participant
- Organisers: dashboard, create/edit/delete events with banner images, categories, view enrolments, capture results
- Participants: browse and filter events, view details, enrol in a category, My Enrolments, My Results, Profile with picture

## Run the application

1. Start the Azure Blob Storage emulator (Azurite):

```
azurite --silent --location C:\azurite --blobPort 10000
```

2. Start the API on http://localhost:5065:

```
cd Part2/RaceDay.Api
dotnet run --launch-profile http
```

3. Build and run the MVC app in Docker from `Part3/RaceDay.Web`:

```
docker build -t raceday-web .
docker run --rm -p 8080:8080 raceday-web
```

4. Open http://localhost:8080

Organiser registration code: `RACEDAY-ORG-2026`

## Notes
- Blob Storage uses the Azurite emulator because an Azure subscription was not available. Only the connection string in `Part2/RaceDay.Api/appsettings.json` needs to change to use a real Azure storage account.
- The MVC app never accesses the database or Azure directly. All data and file handling goes through the Part 2 API.

## Video
YouTube (unlisted):# RaceDay (PROG6212 POE)

RaceDay is an event management system for South African road running, walking and cycling events.

- **Part2/RaceDay.Api**: ASP.NET Core Web API (JWT authentication, SQL Server, Azure Blob Storage for images)
- **Part3/RaceDay.Web**: ASP.NET Core MVC front end that consumes the API (session-based login, Organiser and Participant interfaces)

## Features
- Register and log in as an Organiser (needs the organiser code) or a Participant
- Organisers: dashboard, create/edit/delete events with banner images, categories, view enrolments, capture results
- Participants: browse and filter events, view details, enrol in a category, My Enrolments, My Results, Profile with picture

## Run the application

1. Start the Azure Blob Storage emulator (Azurite):

```
azurite --silent --location C:\azurite --blobPort 10000
```

2. Start the API on http://localhost:5065:

```
cd Part2/RaceDay.Api
dotnet run --launch-profile http
```

3. Build and run the MVC app in Docker from `Part3/RaceDay.Web`:

```
docker build -t raceday-web .
docker run --rm -p 8080:8080 raceday-web
```

4. Open http://localhost:8080

Organiser registration code: `RACEDAY-ORG-2026`

## Notes
- Blob Storage uses the Azurite emulator because an Azure subscription was not available. Only the connection string in `Part2/RaceDay.Api/appsettings.json` needs to change to use a real Azure storage account.
- The MVC app never accesses the database or Azure directly. All data and file handling goes through the Part 2 API.

## Video
YouTube (unlisted): https://youtu.be/gB0dreMeDUg 
