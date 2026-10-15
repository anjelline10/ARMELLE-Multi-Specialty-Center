-- BACK UP YOUR DATABASE FIRST. Execute once in phpMyAdmin, on the existing ARMELLE database.
ALTER TABLE `Patients` ADD COLUMN `Branch` varchar(100) NOT NULL DEFAULT 'North QC';
