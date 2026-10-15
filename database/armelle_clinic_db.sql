CREATE DATABASE IF NOT EXISTS armelle_clinic_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE armelle_clinic_db;
CREATE TABLE IF NOT EXISTS AppUsers (Id INT AUTO_INCREMENT PRIMARY KEY, Username VARCHAR(100) NOT NULL UNIQUE, Password VARCHAR(255) NOT NULL, Role VARCHAR(50) NOT NULL);
CREATE TABLE IF NOT EXISTS Patients (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(150) NOT NULL, BirthDate DATETIME NULL, Sex VARCHAR(30), Contact VARCHAR(50), Address VARCHAR(255), Hmo VARCHAR(100));
CREATE TABLE IF NOT EXISTS Doctors (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(150) NOT NULL, Specialty VARCHAR(120), Schedule VARCHAR(150), Status VARCHAR(40));
CREATE TABLE IF NOT EXISTS Appointments (Id INT AUTO_INCREMENT PRIMARY KEY, Patient VARCHAR(150) NOT NULL, Doctor VARCHAR(150), Specialty VARCHAR(120), Schedule DATETIME NOT NULL, Type VARCHAR(40), Status VARCHAR(40));
CREATE TABLE IF NOT EXISTS QueueEntries (Id INT AUTO_INCREMENT PRIMARY KEY, QueueNo VARCHAR(30), Patient VARCHAR(150), Department VARCHAR(120), Status VARCHAR(40), CheckInAt DATETIME NOT NULL);
CREATE TABLE IF NOT EXISTS Consultations (Id INT AUTO_INCREMENT PRIMARY KEY, Patient VARCHAR(150), Doctor VARCHAR(150), Vitals VARCHAR(255), Notes TEXT, Diagnosis TEXT, Prescription TEXT, Date DATETIME NOT NULL);
CREATE TABLE IF NOT EXISTS LabRequests (Id INT AUTO_INCREMENT PRIMARY KEY, Patient VARCHAR(150), Test VARCHAR(150), Result TEXT, Status VARCHAR(40), Requested DATETIME NOT NULL);
CREATE TABLE IF NOT EXISTS Medicines (Id INT AUTO_INCREMENT PRIMARY KEY, GenericName VARCHAR(150), Brand VARCHAR(150), Stock INT NOT NULL DEFAULT 0, Price DECIMAL(12,2) NOT NULL DEFAULT 0, Expiry DATETIME NULL);
CREATE TABLE IF NOT EXISTS Invoices (Id INT AUTO_INCREMENT PRIMARY KEY, Patient VARCHAR(150), Service VARCHAR(150), Amount DECIMAL(12,2) NOT NULL DEFAULT 0, PaymentMethod VARCHAR(40), Status VARCHAR(40), Date DATETIME NOT NULL);
CREATE TABLE IF NOT EXISTS Employees (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(150), Role VARCHAR(100), Department VARCHAR(100), Shift VARCHAR(100), TimeIn DATETIME NULL, TimeOut DATETIME NULL, Rate DECIMAL(12,2) NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS Payrolls (Id INT AUTO_INCREMENT PRIMARY KEY, Employee VARCHAR(150), RegularPay DECIMAL(12,2) NOT NULL DEFAULT 0, OvertimePay DECIMAL(12,2) NOT NULL DEFAULT 0, Deductions DECIMAL(12,2) NOT NULL DEFAULT 0, NetPay DECIMAL(12,2) NOT NULL DEFAULT 0, Status VARCHAR(40));
CREATE TABLE IF NOT EXISTS Expenses (Id INT AUTO_INCREMENT PRIMARY KEY, Category VARCHAR(100), Description VARCHAR(255), Amount DECIMAL(12,2) NOT NULL DEFAULT 0, Date DATETIME NOT NULL);
INSERT IGNORE INTO AppUsers(Id,Username,Password,Role) VALUES(1,'admin','armelle123','Owner/Admin');
INSERT IGNORE INTO Doctors(Id,Name,Specialty,Schedule,Status) VALUES(1,'Dr. Sofia Reyes','General Medicine','Mon-Fri 9:00 AM-4:00 PM','Available');
INSERT IGNORE INTO Medicines(Id,GenericName,Brand,Stock,Price,Expiry) VALUES(1,'Paracetamol','Biogesic',120,8.50,DATE_ADD(NOW(),INTERVAL 1 YEAR));
CREATE TABLE IF NOT EXISTS ClinicServices (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(150) NOT NULL, Category VARCHAR(100), Description TEXT, Price DECIMAL(12,2) NOT NULL DEFAULT 0, Status VARCHAR(40));
CREATE TABLE IF NOT EXISTS ClinicLocations (Id INT AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(150) NOT NULL, Address VARCHAR(255), Hours VARCHAR(150), Contact VARCHAR(100));
CREATE TABLE IF NOT EXISTS ResourceItems (Id INT AUTO_INCREMENT PRIMARY KEY, Title VARCHAR(180) NOT NULL, Category VARCHAR(100), Content TEXT, Published TINYINT(1) NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS SupportConversations (Id INT AUTO_INCREMENT PRIMARY KEY, VisitorName VARCHAR(150), Email VARCHAR(150), Status VARCHAR(60), AssignedTo VARCHAR(100), CreatedAt DATETIME NOT NULL, UpdatedAt DATETIME NOT NULL);
CREATE TABLE IF NOT EXISTS SupportMessages (Id INT AUTO_INCREMENT PRIMARY KEY, ConversationId INT NOT NULL, Sender VARCHAR(50), Message TEXT, SentAt DATETIME NOT NULL, INDEX(ConversationId));
CREATE TABLE IF NOT EXISTS SupportRatings (Id INT AUTO_INCREMENT PRIMARY KEY, ConversationId INT NOT NULL, RatedType VARCHAR(50), Stars INT NOT NULL, Comment TEXT, CreatedAt DATETIME NOT NULL, INDEX(ConversationId));
INSERT IGNORE INTO AppUsers(Id,Username,Password,Role) VALUES(2,'owner','armelle123','Owner'),(3,'root','armelle123','Root');
INSERT IGNORE INTO ClinicServices(Id,Name,Category,Description,Price,Status) VALUES
(1,'General Medicine Consultation','Consultation','Primary care consultation and general health assessment.',650,'Available'),
(2,'Pediatrics Consultation','Consultation','Child health consultation with a pediatric physician.',700,'Available'),
(3,'Cardiology Consultation','Consultation','Specialist cardiovascular consultation.',900,'Available'),
(4,'Complete Blood Count (CBC)','Laboratory','Routine hematology laboratory test.',350,'Available'),
(5,'Dental Consultation','Dentistry','Initial dental assessment and care planning.',500,'Available');
INSERT IGNORE INTO ClinicLocations(Id,Name,Address,Hours,Contact) VALUES(1,'ARMELLE Main Center','Quezon City, Metro Manila','Monday-Saturday, 8:00 AM-6:00 PM','Clinic contact to be configured');
INSERT IGNORE INTO ResourceItems(Id,Title,Category,Content,Published) VALUES
(1,'HMO Assistance','Patient Guide','Please confirm your HMO provider and coverage with ARMELLE before the visit. Final approval depends on the provider and selected service.',1),
(2,'PhilHealth Guidelines','Patient Guide','Bring valid identification and applicable PhilHealth documents. Eligibility and benefit application depend on current clinic and PhilHealth requirements.',1),
(3,'Laboratory Preparation','Laboratory','Preparation varies by test. Follow the instructions given with your laboratory request and confirm fasting requirements before your visit.',1),
(4,'Pharmacy Guide','Pharmacy','Prescription medicines are dispensed based on a valid prescription and current stock availability.',1);
