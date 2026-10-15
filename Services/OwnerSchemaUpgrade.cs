
using Armelle.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;

namespace Armelle.Services
{
    public static class OwnerSchemaUpgrade
    {
        public static void Apply(ClinicDb db)
        {
            var connection = db.Database.GetDbConnection();

            bool wasClosed =
                connection.State != ConnectionState.Open;

            if (wasClosed)
                connection.Open();

            try
            {
                // Keep existing Patients.Branch upgrade
                if (!ColumnExists(
                    connection, "Patients", "Branch"))
                {
                    Execute(connection, @"
                        ALTER TABLE `Patients`
                        ADD COLUMN `Branch`
                        varchar(100) NOT NULL
                        DEFAULT 'North QC';
                    ");
                }

                // Prescription release is stored privately; never automatically emailed.
                Execute(connection, @"
                    CREATE TABLE IF NOT EXISTS `PrescriptionReleases` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `ConsultationId` int NOT NULL,
                        `PhysicianName` varchar(200) NOT NULL,
                        `LicenseNumber` varchar(100) NOT NULL,
                        `RecordedBy` varchar(100) NOT NULL,
                        `ReleasedAt` datetime(6) NOT NULL,
                        `EstimatedMedicineTotal` decimal(18,2) NOT NULL DEFAULT 0,
                        `PriceBreakdown` longtext NOT NULL,
                        PRIMARY KEY (`Id`),
                        UNIQUE KEY `UX_PrescriptionReleases_ConsultationId` (`ConsultationId`)
                    ) ENGINE=InnoDB;
                ");

                // Owner profiles stored in MySQL
                Execute(connection, @"
                    CREATE TABLE IF NOT EXISTS `OwnerProfiles`
                    (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `AppUserId` int NOT NULL,
                        `DisplayName` varchar(120) NOT NULL,
                        `Position` varchar(160) NOT NULL,
                        `PhotoPath` varchar(500) NOT NULL DEFAULT '',
                        PRIMARY KEY (`Id`),
                        UNIQUE KEY `UX_OwnerProfiles_AppUserId`
                            (`AppUserId`),
                        CONSTRAINT `FK_OwnerProfiles_AppUsers`
                            FOREIGN KEY (`AppUserId`)
                            REFERENCES `AppUsers` (`Id`)
                            ON DELETE CASCADE
                    ) ENGINE=InnoDB;
                ");
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }

        private static bool ColumnExists(
            System.Data.Common.DbConnection connection,
            string table,
            string column)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = @table
                  AND COLUMN_NAME = @column;
            ";

            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@table";
            tableParameter.Value = table;
            command.Parameters.Add(tableParameter);

            var columnParameter = command.CreateParameter();
            columnParameter.ParameterName = "@column";
            columnParameter.Value = column;
            command.Parameters.Add(columnParameter);

            return Convert.ToInt64(
                command.ExecuteScalar()) > 0;
        }

        private static void Execute(
            System.Data.Common.DbConnection connection,
            string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
