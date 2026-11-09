\# RaceDay (PROG6212 POE)



RaceDay is an event management system for South African road running, walking and cycling events.



\- \*\*Part2/RaceDay.Api\*\*: ASP.NET Core Web API (JWT auth, SQL Server, Azure Blob Storage for images)

\- \*\*Part3/RaceDay.Web\*\*: ASP.NET Core MVC front end that consumes the API (session-based login, Organiser and Participant interfaces)



\## Features

\- Register and log in as an Organiser (needs the organiser code) or a Participant

\- Organisers: dashboard, create/edit/delete events with banner images, categories, view enrolments, capture results

\- Participants: browse and filter events, view details, enrol in a category, My Enrolments, My Results, Profile with picture



\## Run the application



1\. Start the Azure Blob Storage emulator (Azurite): `azurite --silent --location C:\\azurite --blobPort 10000`

2\. Start the API on http://localhost:5065: `cd Part2/RaceDay.Api` then `dotnet run --launch-profile http`

3\. Build and run the MVC app in Docker with a single command from `Part3/RaceDay.Web`:



```

docker build -t raceday-web . \&\& docker run --rm -p 8080:8080 raceday-web

```



4\. Open http://localhost:8080



Organiser registration code: `RACEDAY-ORG-2026`



Note: Blob Storage uses the Azurite emulator because an Azure subscription was not available. Only the connection string in `Part2/RaceDay.Api/appsettings.json` needs to change to use a real Azure storage account.



\## Video

YouTube (unlisted): PASTE LINK HERE

