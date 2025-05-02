using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.DepotManagement;

public class DepotsTableBuilder
{
    private readonly string _masterConnectionString;
    private readonly ILogger<DepotsTableBuilder> _logger;

    public DepotsTableBuilder(string masterConnectionString, ILogger<DepotsTableBuilder> logger)
    {
        _masterConnectionString = masterConnectionString;
        _logger = logger;
    }

    public void BuildTenantDatabases()
    {
        var tenantConnectionStrings = GetTenantConnectionStrings();

        foreach (var connStr in tenantConnectionStrings)
        {
            try
            {
                EnsureDepotsTable(connStr);
                _logger.LogInformation($"Depots table processed (tenant DB): {connStr}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{connStr}] Error processing tenant DB");
            }
        }
    }

    public void BuildMasterDatabase()
    {
        try
        {
            EnsureDepotsTable(_masterConnectionString);
            _logger.LogInformation("Depots table processed (master DB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing master DB");
        }
    }

    private List<string> GetTenantConnectionStrings()
    {
        var result = new List<string>();

        using (var connection = new SqlConnection(_masterConnectionString))
        {
            connection.Open();
            var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var connectionString = reader["ConnectionString"]?.ToString();
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        result.Add(connectionString);
                    }
                }
            }
        }

        return result;
    }

    private void EnsureDepotsTable(string connectionString)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            var cmdCheck = new SqlCommand(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Depots'", connection);

            int tableCount = (int)cmdCheck.ExecuteScalar();

            if (tableCount == 0)
            {
                var cmdCreate = new SqlCommand(@"
                    CREATE TABLE [dbo].[Depots] (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Active] BIT NOT NULL DEFAULT(1),
                        [IsDeleted] BIT NOT NULL DEFAULT(0),
                        [CreatedAt] DATETIMEOFFSET(7) NOT NULL,
                        [CreatedBy] NVARCHAR(255) NULL,
                        [Name] NVARCHAR(MAX) NULL
                    )", connection);

                cmdCreate.ExecuteNonQuery();

                _logger.LogInformation("Depots table created.");
            }
            else
            {
                var expectedColumns = new Dictionary<string, string>
                {
                    ["Active"] = "BIT NOT NULL DEFAULT(1)",
                    ["IsDeleted"] = "BIT NOT NULL DEFAULT(0)",
                    ["CreatedAt"] = "DATETIMEOFFSET(7) NOT NULL",
                    ["CreatedBy"] = "NVARCHAR(255) NULL",
                    ["Name"] = "NVARCHAR(MAX) NULL"
                };

                foreach (var kvp in expectedColumns)
                {
                    var columnName = kvp.Key;

                    var cmdColumnCheck = new SqlCommand(@"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Depots' AND COLUMN_NAME = @ColumnName", connection);
                    cmdColumnCheck.Parameters.AddWithValue("@ColumnName", columnName);

                    int colExists = (int)cmdColumnCheck.ExecuteScalar();

                    if (colExists == 0)
                    {
                        var alterCmd = new SqlCommand(
                            $"ALTER TABLE [dbo].[Depots] ADD [{columnName}] {kvp.Value}", connection);
                        alterCmd.ExecuteNonQuery();

                        _logger.LogInformation($"Column added: {columnName} ({kvp.Value})");
                    }
                }
            }

            var cmdCountRows = new SqlCommand("SELECT COUNT(*) FROM [dbo].[Depots]", connection);
            int rowCount = (int)cmdCountRows.ExecuteScalar();

            if (rowCount == 0)
            {
                var cmdInsertDefaults = new SqlCommand(@"
                    INSERT INTO [dbo].[Depots] (Active, IsDeleted, CreatedAt, CreatedBy, Name)
                    VALUES
                        (1, 0, SYSDATETIMEOFFSET(), 'System', 'Initial Depot 1'),
                        (1, 0, SYSDATETIMEOFFSET(), 'System', 'Initial Depot 2')", connection);

                int inserted = cmdInsertDefaults.ExecuteNonQuery();
                _logger.LogInformation($"Depots 기본 데이터 {inserted}건 삽입 완료");
            }
        }
    }

    public static void Run(IServiceProvider services, bool forMaster)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<DepotsTableBuilder>>();
            var config = services.GetRequiredService<IConfiguration>();
            var masterConnectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(masterConnectionString))
            {
                throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");
            }

            var builder = new DepotsTableBuilder(masterConnectionString, logger);

            if (forMaster)
            {
                builder.BuildMasterDatabase();
            }
            else
            {
                builder.BuildTenantDatabases();
            }
        }
        catch (Exception ex)
        {
            var fallbackLogger = services.GetService<ILogger<DepotsTableBuilder>>();
            fallbackLogger?.LogError(ex, "Error while processing Depots table.");
        }
    }
}
