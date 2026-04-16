-- =============================================================================
-- ACLS Development Seed Data
-- Database: (localdb)\mssqllocaldb  |  ACLS_Dev
-- All users share password: Password123!
--
-- Run with:
--   sqlcmd -S "(localdb)\mssqllocaldb" -d ACLS_Dev -E -I -i seed.sql
--   (from the docs\Startup directory)
--   -I is required to enable QUOTED_IDENTIFIER for EF Core filtered indexes
-- =============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Guard: skip if already seeded
IF EXISTS (SELECT 1 FROM Properties WHERE Name = 'Sunset Apartments')
BEGIN
    PRINT 'Database already seeded — skipping.';
    RETURN;
END

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. PROPERTY
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Properties (Name, Address, CreatedAt, IsActive)
VALUES ('Sunset Apartments', '45 Maple Avenue, Chicago, IL 60601', GETUTCDATE(), 1);

DECLARE @PropertyId INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. BUILDINGS
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Buildings (PropertyId, Name, CreatedAt)
VALUES (@PropertyId, 'Building A', GETUTCDATE());
DECLARE @BuildingAId INT = SCOPE_IDENTITY();

INSERT INTO Buildings (PropertyId, Name, CreatedAt)
VALUES (@PropertyId, 'Building B', GETUTCDATE());
DECLARE @BuildingBId INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. UNITS
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingAId, 'A-101', 1, GETUTCDATE());
DECLARE @UnitA101 INT = SCOPE_IDENTITY();

INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingAId, 'A-102', 1, GETUTCDATE());
DECLARE @UnitA102 INT = SCOPE_IDENTITY();

INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingAId, 'A-201', 2, GETUTCDATE());
DECLARE @UnitA201 INT = SCOPE_IDENTITY();

INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingBId, 'B-101', 1, GETUTCDATE());
DECLARE @UnitB101 INT = SCOPE_IDENTITY();

INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingBId, 'B-201', 2, GETUTCDATE());
DECLARE @UnitB201 INT = SCOPE_IDENTITY();

INSERT INTO Units (BuildingId, UnitNumber, Floor, CreatedAt)
VALUES (@BuildingBId, 'B-202', 2, GETUTCDATE());
DECLARE @UnitB202 INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. USERS  (password for all: Password123!)
--    Role values: 'Manager' | 'MaintenanceStaff' | 'Resident'
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('manager@sunsetapts.com',
        '$2a$12$sxeg4k6gBV1u.hNmOxNIMeJpwIkkNsq9ORraP9Pxr0aCJdyIsTkTG',
        '+1-312-555-0100', 'James', 'Wilson', 'Manager', @PropertyId, 1, GETUTCDATE());
DECLARE @ManagerUserId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('mike@sunsetapts.com',
        '$2a$12$X/Mwhn5faQ9cjlncgDzxo.1RJrLz7NkLibfNg8Mb4JKzmJE1ede8O',
        '+1-312-555-0201', 'Mike', 'Rodriguez', 'MaintenanceStaff', @PropertyId, 1, GETUTCDATE());
DECLARE @MikeUserId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('sarah@sunsetapts.com',
        '$2a$12$g5GMVirs5vEzqGWgaJs6AuqDl45kbrCR9JuP4eQJQRFqy6YTlb8r2',
        '+1-312-555-0202', 'Sarah', 'Chen', 'MaintenanceStaff', @PropertyId, 1, GETUTCDATE());
DECLARE @SarahUserId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('alice@example.com',
        '$2a$12$wUFuHxc1Y1ym1tJMqHdlseD5fs0JRy5A8S0ZhDeUEzbDmQr.4VqyW',
        '+1-312-555-1001', 'Alice', 'Johnson', 'Resident', @PropertyId, 1, GETUTCDATE());
DECLARE @AliceUserId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('bob@example.com',
        '$2a$12$m7UI0rk3GB5OlEaPFo5PGuOjjDGF8udGeMSkygo5aWP8xCKSZnU4S',
        '+1-312-555-1002', 'Bob', 'Smith', 'Resident', @PropertyId, 1, GETUTCDATE());
DECLARE @BobUserId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, PasswordHash, Phone, FirstName, LastName, Role, PropertyId, IsActive, CreatedAt)
VALUES ('carol@example.com',
        '$2a$12$Szh/Rm0zhkN6L161AOs.kOj2.9AvZUVpFjsft.dlc9J9m8Iwmj2zC',
        '+1-312-555-1003', 'Carol', 'Martinez', 'Resident', @PropertyId, 1, GETUTCDATE());
DECLARE @CarolUserId INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. STAFF MEMBERS
--    Skills stored as JSON array string: '["Skill1","Skill2"]'
--    Availability values: 'AVAILABLE' | 'BUSY' | 'OFF_DUTY'
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO StaffMembers (UserId, JobTitle, Skills, Availability, AverageRating, LastAssignedAt)
VALUES (@MikeUserId, 'Plumber', '["Plumbing","HVAC"]', 'BUSY', 4.50, DATEADD(day, -2, GETUTCDATE()));
DECLARE @MikeStaffId INT = SCOPE_IDENTITY();

INSERT INTO StaffMembers (UserId, JobTitle, Skills, Availability, AverageRating, LastAssignedAt)
VALUES (@SarahUserId, 'Electrician', '["Electrical","General"]', 'AVAILABLE', 4.80, DATEADD(day, -13, GETUTCDATE()));
DECLARE @SarahStaffId INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. RESIDENTS
--    LeaseStart / LeaseEnd stored as SQL date type
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO Residents (UserId, UnitId, LeaseStart, LeaseEnd)
VALUES (@AliceUserId, @UnitA101, '2024-01-01', '2025-12-31');
DECLARE @AliceResidentId INT = SCOPE_IDENTITY();

INSERT INTO Residents (UserId, UnitId, LeaseStart, LeaseEnd)
VALUES (@BobUserId, @UnitA102, '2024-06-01', '2025-05-31');
DECLARE @BobResidentId INT = SCOPE_IDENTITY();

INSERT INTO Residents (UserId, UnitId, LeaseStart, LeaseEnd)
VALUES (@CarolUserId, @UnitB201, '2023-09-01', '2025-08-31');
DECLARE @CarolResidentId INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. COMPLAINTS
--    Status values:  'OPEN' | 'ASSIGNED' | 'EN_ROUTE' | 'IN_PROGRESS' | 'RESOLVED' | 'CLOSED'
--    Urgency values: 'LOW' | 'MEDIUM' | 'HIGH' | 'SOS_EMERGENCY'
--    RequiredSkills: JSON array string
-- ─────────────────────────────────────────────────────────────────────────────

-- #1  OPEN — Alice, leaking faucet (no staff assigned yet)
INSERT INTO Complaints (PropertyId, UnitId, ResidentId, AssignedStaffMemberId,
                        Title, Description, Category, RequiredSkills,
                        Urgency, Status, PermissionToEnter,
                        Eta, CreatedAt, UpdatedAt, ResolvedAt,
                        Tat, ResidentRating, ResidentFeedbackComment, FeedbackSubmittedAt)
VALUES (@PropertyId, @UnitA101, @AliceResidentId, NULL,
        'Leaking Faucet in Kitchen',
        'The kitchen faucet has been dripping constantly for 3 days. Water is pooling under the sink cabinet.',
        'Plumbing', '["Plumbing"]',
        'LOW', 'OPEN', 1,
        NULL, DATEADD(day, -3, GETUTCDATE()), DATEADD(day, -3, GETUTCDATE()), NULL,
        NULL, NULL, NULL, NULL);
DECLARE @Complaint1Id INT = SCOPE_IDENTITY();

-- #2  ASSIGNED — Bob, electrical outlet (Mike assigned, en route ETA 4 hours)
INSERT INTO Complaints (PropertyId, UnitId, ResidentId, AssignedStaffMemberId,
                        Title, Description, Category, RequiredSkills,
                        Urgency, Status, PermissionToEnter,
                        Eta, CreatedAt, UpdatedAt, ResolvedAt,
                        Tat, ResidentRating, ResidentFeedbackComment, FeedbackSubmittedAt)
VALUES (@PropertyId, @UnitA102, @BobResidentId, @MikeStaffId,
        'Electrical Outlet Not Working',
        'The outlet next to the bathroom mirror stopped working suddenly. I cannot use the hairdryer or electric shaver.',
        'Electrical', '["Electrical"]',
        'MEDIUM', 'ASSIGNED', 0,
        DATEADD(hour, 4, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE()), NULL,
        NULL, NULL, NULL, NULL);
DECLARE @Complaint2Id INT = SCOPE_IDENTITY();

-- #3  IN_PROGRESS — Carol, HVAC not cooling (Mike assigned and actively working)
INSERT INTO Complaints (PropertyId, UnitId, ResidentId, AssignedStaffMemberId,
                        Title, Description, Category, RequiredSkills,
                        Urgency, Status, PermissionToEnter,
                        Eta, CreatedAt, UpdatedAt, ResolvedAt,
                        Tat, ResidentRating, ResidentFeedbackComment, FeedbackSubmittedAt)
VALUES (@PropertyId, @UnitB201, @CarolResidentId, @MikeStaffId,
        'HVAC Not Cooling Apartment',
        'Air conditioning unit is running continuously but producing no cold air. Indoor temperature is 85°F. I have a baby at home.',
        'HVAC', '["HVAC","Plumbing"]',
        'HIGH', 'IN_PROGRESS', 1,
        NULL, DATEADD(day, -2, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE()), NULL,
        NULL, NULL, NULL, NULL);
DECLARE @Complaint3Id INT = SCOPE_IDENTITY();

-- #4  RESOLVED — Alice, buzzing fluorescent light (Sarah resolved it 8 days ago)
INSERT INTO Complaints (PropertyId, UnitId, ResidentId, AssignedStaffMemberId,
                        Title, Description, Category, RequiredSkills,
                        Urgency, Status, PermissionToEnter,
                        Eta, CreatedAt, UpdatedAt, ResolvedAt,
                        Tat, ResidentRating, ResidentFeedbackComment, FeedbackSubmittedAt)
VALUES (@PropertyId, @UnitA101, @AliceResidentId, @SarahStaffId,
        'Buzzing Fluorescent Light in Hallway',
        'The hallway ceiling light flickers and makes a constant loud buzzing noise. It is worse after 8pm.',
        'Electrical', '["Electrical"]',
        'LOW', 'RESOLVED', 1,
        NULL, DATEADD(day, -10, GETUTCDATE()), DATEADD(day, -8, GETUTCDATE()), DATEADD(day, -8, GETUTCDATE()),
        48.00, NULL, NULL, NULL);
DECLARE @Complaint4Id INT = SCOPE_IDENTITY();

-- #5  CLOSED — Bob, broken tile (Sarah resolved + resident gave feedback)
INSERT INTO Complaints (PropertyId, UnitId, ResidentId, AssignedStaffMemberId,
                        Title, Description, Category, RequiredSkills,
                        Urgency, Status, PermissionToEnter,
                        Eta, CreatedAt, UpdatedAt, ResolvedAt,
                        Tat, ResidentRating, ResidentFeedbackComment, FeedbackSubmittedAt)
VALUES (@PropertyId, @UnitA102, @BobResidentId, @SarahStaffId,
        'Broken Tile in Bathroom Floor',
        'A floor tile cracked and is now a tripping hazard with sharp exposed edges. Located directly in front of the shower.',
        'General', '["General"]',
        'MEDIUM', 'CLOSED', 1,
        NULL, DATEADD(day, -15, GETUTCDATE()), DATEADD(day, -12, GETUTCDATE()), DATEADD(day, -13, GETUTCDATE()),
        36.00, 5, 'Sarah did an amazing job! Arrived on time, fixed it perfectly, and cleaned up after herself. Highly professional.',
        DATEADD(day, -12, GETUTCDATE()));
DECLARE @Complaint5Id INT = SCOPE_IDENTITY();

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. WORK NOTES (for the IN_PROGRESS and RESOLVED complaints)
-- ─────────────────────────────────────────────────────────────────────────────

-- Work notes on complaint #3 (HVAC, Mike)
INSERT INTO WorkNotes (ComplaintId, StaffMemberId, Content, CreatedAt)
VALUES (@Complaint3Id, @MikeStaffId,
        'Inspected the unit. Refrigerant level is critically low — likely a slow leak in the evaporator coil. Sourcing replacement parts.',
        DATEADD(hour, -3, GETUTCDATE()));

INSERT INTO WorkNotes (ComplaintId, StaffMemberId, Content, CreatedAt)
VALUES (@Complaint3Id, @MikeStaffId,
        'Parts ordered. Return visit scheduled for tomorrow morning to complete the recharge and coil repair.',
        DATEADD(hour, -1, GETUTCDATE()));

-- Work note on complaint #4 (resolved light, Sarah)
INSERT INTO WorkNotes (ComplaintId, StaffMemberId, Content, CreatedAt)
VALUES (@Complaint4Id, @SarahStaffId,
        'Replaced faulty ballast in fluorescent fixture. Tested — no more buzzing or flickering. Issue resolved.',
        DATEADD(day, -8, GETUTCDATE()));

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. OUTAGES
--    OutageType values: 'Electricity' | 'Water' | 'Gas' | 'Internet' | 'Elevator' | 'Other'
-- ─────────────────────────────────────────────────────────────────────────────

-- Upcoming scheduled water maintenance (2 days from now, 4-hour window)
INSERT INTO Outages (PropertyId, DeclaredByManagerUserId, Title, OutageType,
                     Description, StartTime, EndTime, DeclaredAt, NotificationSentAt)
VALUES (@PropertyId, @ManagerUserId,
        'Scheduled Water Maintenance — All Buildings',
        'Water',
        'Annual plumbing system inspection and pressure test. All water (hot and cold) will be shut off during this window. Please store drinking water in advance. Laundry facilities will be unavailable.',
        DATEADD(day, 2, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)),
        DATEADD(hour, 4, DATEADD(day, 2, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2))),
        GETUTCDATE(), GETUTCDATE());

-- Past power outage (5 days ago, resolved)
INSERT INTO Outages (PropertyId, DeclaredByManagerUserId, Title, OutageType,
                     Description, StartTime, EndTime, DeclaredAt, NotificationSentAt)
VALUES (@PropertyId, @ManagerUserId,
        'Emergency Power Outage — Building A',
        'Electricity',
        'Unexpected power failure caused by a transformer fault on Maple Avenue. ComEd was notified immediately. Emergency lighting is active. Building generator covers hallways and the elevator.',
        DATEADD(hour, -5, DATEADD(day, -5, GETUTCDATE())),
        DATEADD(hour, 1, DATEADD(day, -5, GETUTCDATE())),
        DATEADD(hour, -5, DATEADD(day, -5, GETUTCDATE())),
        DATEADD(hour, -5, DATEADD(day, -5, GETUTCDATE())));

COMMIT TRANSACTION;

PRINT '';
PRINT '✓ Seed data inserted successfully into ACLS_Dev.';
PRINT '';
PRINT 'Logins (password: Password123!)';
PRINT '  manager@sunsetapts.com  — Manager';
PRINT '  mike@sunsetapts.com     — Maintenance Staff';
PRINT '  sarah@sunsetapts.com    — Maintenance Staff';
PRINT '  alice@example.com       — Resident (Unit A-101)';
PRINT '  bob@example.com         — Resident (Unit A-102)';
PRINT '  carol@example.com       — Resident (Unit B-201)';
