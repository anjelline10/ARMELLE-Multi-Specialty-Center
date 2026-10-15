-- BACK UP YOUR DATABASE FIRST. Select the correct ARMELLE database in phpMyAdmin.
-- Run once in phpMyAdmin against your EXISTING ARMELLE database.
-- Does not delete or modify any existing patient, appointment, or billing rows.
CREATE TABLE IF NOT EXISTS `PatientProfiles` (
 `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `AppUserId` INT NOT NULL,
 `FullName` VARCHAR(120) NOT NULL DEFAULT '',
 `BirthDate` DATETIME(6) NULL,
 `Sex` VARCHAR(40) NOT NULL DEFAULT '',
 `Contact` VARCHAR(20) NOT NULL DEFAULT '',
 `Email` VARCHAR(160) NOT NULL DEFAULT '',
 `Address` VARCHAR(350) NOT NULL DEFAULT '',
 `EmergencyContactName` VARCHAR(120) NOT NULL DEFAULT '',
 `EmergencyRelationship` VARCHAR(70) NOT NULL DEFAULT '',
 `EmergencyContactNumber` VARCHAR(20) NOT NULL DEFAULT '',
 `Hmo` VARCHAR(120) NOT NULL DEFAULT '',
 `HmoMemberNumber` VARCHAR(80) NOT NULL DEFAULT '',
 `PhotoPath` VARCHAR(300) NOT NULL DEFAULT '',
 UNIQUE KEY `IX_PatientProfiles_AppUserId` (`AppUserId`),
 CONSTRAINT `FK_PatientProfiles_AppUsers_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `AppUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `PrescriptionDocuments` (
 `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `ConsultationId` INT NOT NULL,
 `StoredFileName` VARCHAR(255) NOT NULL DEFAULT '',
 `OriginalFileName` VARCHAR(255) NOT NULL DEFAULT '',
 `UploadedAtUtc` DATETIME(6) NOT NULL,
 INDEX `IX_PrescriptionDocuments_ConsultationId` (`ConsultationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
-- Repair NULLs that cause Owner -> Patients to crash:
UPDATE `Patients` SET `Name`=COALESCE(`Name`,''),`Sex`=COALESCE(`Sex`,''),`Contact`=COALESCE(`Contact`,''),`Address`=COALESCE(`Address`,''),`Hmo`=COALESCE(`Hmo`,''),`Branch`=COALESCE(`Branch`,'North QC');
