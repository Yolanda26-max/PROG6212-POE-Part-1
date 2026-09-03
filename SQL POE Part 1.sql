IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(20) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles(RoleId)
);
GO

CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       NVARCHAR(150) NOT NULL,
    EventDate       DATE NOT NULL,
    Location        NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users(UserId)
);
GO

CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    NVARCHAR(100) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    Price           DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events(EventId)
);
GO

CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

CREATE TABLE dbo.Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments(EnrolmentId)
);
GO

INSERT INTO dbo.Roles (RoleName) VALUES
    ('Organiser'),
    ('Participant');
GO

INSERT INTO dbo.Users (RoleId, FullName, Email, PasswordHash) VALUES
    (1, 'Thabo Nkosi',     'thabo.nkosi@raceday.co.za',     'HASHED_PASSWORD_1'),
    (1, 'Lindiwe Dlamini', 'lindiwe.dlamini@raceday.co.za', 'HASHED_PASSWORD_2'),
    (2, 'Sipho Mokoena',   'sipho.mokoena@example.com',     'HASHED_PASSWORD_3'),
    (2, 'Anika Pillay',    'anika.pillay@example.com',      'HASHED_PASSWORD_4');
GO

INSERT INTO dbo.Events (OrganiserId, EventName, EventDate, Location, Description) VALUES
    (1, 'Johannesburg City Marathon', '2026-10-18', 'Johannesburg, Gauteng', 'Annual road marathon through the Johannesburg CBD.'),
    (1, 'Soweto Community Park Run',  '2026-09-05', 'Soweto, Gauteng',        'Family-friendly weekly park run event.'),
    (2, 'Cape Peninsula Cycle Tour',  '2026-11-22', 'Cape Town, Western Cape','Scenic road cycling tour around the Cape Peninsula.');
GO

INSERT INTO dbo.Categories (EventId, CategoryName, DistanceKm, Price) VALUES
    (1, '10km Fun Run',   10.00,  150.00),
    (1, 'Half Marathon',  21.10,  250.00),
    (1, 'Full Marathon',  42.20,  350.00),
    (2, '5km Walk',        5.00,   50.00),
    (2, '5km Run',          5.00,   80.00),
    (3, '60km Cycle',     60.00,  300.00),
    (3, '110km Cycle',   110.00,  450.00);
GO

INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status) VALUES
    (3, 2, 'Confirmed'),
    (3, 6, 'Confirmed'),
    (4, 1, 'Confirmed'),
    (4, 4, 'Confirmed');
GO

INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position, Status) VALUES
    (1, '01:45:32', 12, 'Finished'),
    (3, '00:52:10', 5,  'Finished');
GO