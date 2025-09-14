CREATE DATABASE RahmBdDb;
GO
USE RahmBdDb;
GO

-- Admins & Users
CREATE TABLE Admins (
  Id INT IDENTITY PRIMARY KEY,
  Name NVARCHAR(100) NOT NULL,
  Email NVARCHAR(200) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(200) NOT NULL
);

CREATE TABLE Users (
  Id INT IDENTITY PRIMARY KEY,
  Name NVARCHAR(100) NOT NULL,
  MobileNo NVARCHAR(30) NOT NULL UNIQUE,
  Email NVARCHAR(200) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(200) NOT NULL
);

-- Locations (per user)
CREATE TABLE Locations (
  Id INT IDENTITY PRIMARY KEY,
  UserId INT NOT NULL,
  Road NVARCHAR(200) NOT NULL,
  District NVARCHAR(100) NOT NULL,
  Division NVARCHAR(100) NOT NULL,
  Lat FLOAT NULL,
  Lng FLOAT NULL,
  CONSTRAINT FK_Location_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Location_User ON Locations(UserId);

-- Health centers (owned by you, used by map & stock)
CREATE TABLE HealthCenters (
  Id INT IDENTITY PRIMARY KEY,
  Name NVARCHAR(150) NOT NULL,
  Road NVARCHAR(200) NOT NULL,
  District NVARCHAR(100) NOT NULL,
  Division NVARCHAR(100) NOT NULL,
  Lat FLOAT NOT NULL,
  Lng FLOAT NOT NULL
);
CREATE INDEX IX_HealthCenter_Coords ON HealthCenters(Lat, Lng);

-- Diseases / Vaccines / Inventory
CREATE TABLE Diseases (
  Id INT IDENTITY PRIMARY KEY,
  Name NVARCHAR(120) NOT NULL
);

CREATE TABLE Vaccines (
  Id INT IDENTITY PRIMARY KEY,
  DiseaseId INT NOT NULL,
  Name NVARCHAR(120) NOT NULL,
  CONSTRAINT FK_Vaccine_Disease FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE NO ACTION
);

CREATE TABLE VaccineInventories (
  Id INT IDENTITY PRIMARY KEY,
  HealthCenterId INT NOT NULL,
  VaccineId INT NOT NULL,
  QuantityAvailable INT NOT NULL DEFAULT(0),
  CONSTRAINT FK_VI_Center FOREIGN KEY (HealthCenterId) REFERENCES HealthCenters(Id) ON DELETE CASCADE,
  CONSTRAINT FK_VI_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccines(Id) ON DELETE CASCADE
);
CREATE INDEX IX_VI_Vaccine_Qty ON VaccineInventories(VaccineId, QuantityAvailable);

-- Vaccination logs
CREATE TABLE VaccinationLogs (
  Id INT IDENTITY PRIMARY KEY,
  UserId INT NOT NULL,
  VaccineId INT NOT NULL,
  HealthCenterId INT NOT NULL,
  VaccinatedAt DATETIME2 NOT NULL,
  CONSTRAINT FK_VLog_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_VLog_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccines(Id) ON DELETE NO ACTION,
  CONSTRAINT FK_VLog_Center FOREIGN KEY (HealthCenterId) REFERENCES HealthCenters(Id) ON DELETE NO ACTION
);

-- Meds & disease logs
CREATE TABLE Medications (
  Id INT IDENTITY PRIMARY KEY,
  DiseaseId INT NOT NULL,
  MedName NVARCHAR(150) NOT NULL,
  CONSTRAINT FK_Med_Disease FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE CASCADE
);

CREATE TABLE DiseaseLogs (
  Id INT IDENTITY PRIMARY KEY,
  DiseaseId INT NOT NULL,
  UserId INT NOT NULL,
  ReportedAt DATETIME2 NOT NULL,
  CONSTRAINT FK_DLog_Disease FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE NO ACTION,
  CONSTRAINT FK_DLog_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Health tips
CREATE TABLE HealthTips (
  Id INT IDENTITY PRIMARY KEY,
  DiseaseId INT NOT NULL,
  TipText NVARCHAR(500) NOT NULL,
  CONSTRAINT FK_Tip_Disease FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE CASCADE
);

-- User requests
-- Type: 1=NearbyCenter, 2=HealthTip, 3=VaccineAvailability
-- Status: 0=Pending, 1=Done, 2=Rejected
CREATE TABLE UserRequests (
  Id INT IDENTITY PRIMARY KEY,
  UserId INT NOT NULL,
  Type INT NOT NULL,
  Status INT NOT NULL,
  CreatedAt DATETIME2 NOT NULL,
  DiseaseId INT NULL,
  VaccineId INT NULL,
  HealthCenterId INT NULL,
  CONSTRAINT FK_UReq_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  CONSTRAINT FK_UReq_Disease FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE SET NULL,
  CONSTRAINT FK_UReq_Vaccine FOREIGN KEY (VaccineId) REFERENCES Vaccines(Id) ON DELETE SET NULL,
  CONSTRAINT FK_UReq_Center FOREIGN KEY (HealthCenterId) REFERENCES HealthCenters(Id) ON DELETE SET NULL
);
CREATE INDEX IX_UReq_TypeStatus ON UserRequests(Type, Status);

-- Campaigns & deliveries (admin-only)
CREATE TABLE NotificationCampaigns (
  Id INT IDENTITY PRIMARY KEY,
  CreatedByAdminId INT NOT NULL,
  Title NVARCHAR(200) NOT NULL,
  Body NVARCHAR(1000) NOT NULL,
  Channel NVARCHAR(10) NOT NULL, -- 'SMS' | 'Email' | 'Both'
  Division NVARCHAR(100) NULL,
  District NVARCHAR(100) NULL,
  ScheduledAt DATETIME2 NULL,
  Status NVARCHAR(20) NOT NULL,  -- 'Draft' | 'Scheduled' | 'Completed' | 'Cancelled'
  CONSTRAINT FK_Camp_Admin FOREIGN KEY (CreatedByAdminId) REFERENCES Admins(Id) ON DELETE NO ACTION
);

CREATE TABLE NotificationDeliveries (
  Id INT IDENTITY PRIMARY KEY,
  CampaignId INT NOT NULL,
  UserId INT NOT NULL,
  Channel NVARCHAR(10) NOT NULL,
  ToAddress NVARCHAR(200) NOT NULL,
  Status NVARCHAR(20) NOT NULL,   -- 'Queued' | 'Sent' | 'Failed'
  CONSTRAINT FK_Deliv_Camp FOREIGN KEY (CampaignId) REFERENCES NotificationCampaigns(Id) ON DELETE CASCADE,
  CONSTRAINT FK_Deliv_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);