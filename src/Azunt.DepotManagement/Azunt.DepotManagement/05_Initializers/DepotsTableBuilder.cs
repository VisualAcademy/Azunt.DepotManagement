using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.DepotManagement;

public class DepotsTableBuilder
{
    private readonly string _connectionString;
    private readonly ILogger<DepotsTableBuilder> _logger;

    public DepotsTableBuilder(string connectionString, ILogger<DepotsTableBuilder> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public void BuildMasterDatabase()
    {
        try
        {
            EnsureDepotsTable(_connectionString);
            _logger.LogInformation("Depots table processed (master DB).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Depots table (master DB).");
        }
    }

    public void BuildTenantDatabases()
    {
        var tenantConnectionStrings = GetTenantConnectionStrings();

        for (int i = 0; i < tenantConnectionStrings.Count; i++)
        {
            var connStr = tenantConnectionStrings[i];
            var tenantIndex = i + 1;

            try
            {
                EnsureDepotsTable(connStr);
                _logger.LogInformation("Depots table processed (tenant DB #{Index}).", tenantIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tenant DB #{Index}.", tenantIndex);
            }
        }
    }

    private List<string> GetTenantConnectionStrings()
    {
        var result = new List<string>();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var connStr = reader["ConnectionString"]?.ToString();
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                result.Add(connStr);
            }
        }

        return result;
    }

    private void EnsureDepotsTable(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var checkTableCmd = new SqlCommand(@"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'Depots';", connection);

        int exists = (int)checkTableCmd.ExecuteScalar();

        if (exists == 0)
        {
            using var createCmd = new SqlCommand(@"
-- [0][0] 창고: Depots
CREATE TABLE [dbo].[Depots]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Active] BIT NOT NULL CONSTRAINT [DF_Depots_Active] DEFAULT ((1)),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Depots_IsDeleted] DEFAULT ((0)),
    [CreatedAt] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_Depots_CreatedAt] DEFAULT (SYSDATETIMEOFFSET()),
    [CreatedBy] NVARCHAR(255) NULL,
    [Name] NVARCHAR(MAX) NULL
);", connection);

            createCmd.ExecuteNonQuery();
            _logger.LogInformation("Depots table created.");
        }
        else
        {
            var expectedColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Active"] = "BIT NULL",
                ["IsDeleted"] = "BIT NULL",
                ["CreatedAt"] = "DATETIMEOFFSET(7) NULL",
                ["CreatedBy"] = "NVARCHAR(255) NULL",
                ["Name"] = "NVARCHAR(MAX) NULL"
            };

            foreach (var column in expectedColumns)
            {
                using var checkColumnCmd = new SqlCommand(@"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'Depots'
  AND COLUMN_NAME = @ColumnName;", connection);

                checkColumnCmd.Parameters.AddWithValue("@ColumnName", column.Key);

                int columnExists = (int)checkColumnCmd.ExecuteScalar();

                if (columnExists == 0)
                {
                    using var alterCmd = new SqlCommand($@"
ALTER TABLE [dbo].[Depots]
ADD [{column.Key}] {column.Value};", connection);

                    alterCmd.ExecuteNonQuery();
                    _logger.LogInformation("Column [{Column}] added to Depots table.", column.Key);
                }
            }

            EnsurePrimaryKeyOnId(connection);
            EnsureActiveDefault(connection);
            EnsureIsDeletedDefault(connection);
            EnsureCreatedAtDefault(connection);
        }

        var cmdCountRows = new SqlCommand("SELECT COUNT(*) FROM [dbo].[Depots]", connection);
        int rowCount = (int)cmdCountRows.ExecuteScalar();

        if (rowCount == 0)
        {
            using var cmdInsertDefaults = new SqlCommand(@"
INSERT INTO [dbo].[Depots] ([Active], [IsDeleted], [CreatedAt], [CreatedBy], [Name])
VALUES
    (1, 0, SYSDATETIMEOFFSET(), N'System', N'Initial Depot 1'),
    (1, 0, SYSDATETIMEOFFSET(), N'System', N'Initial Depot 2');", connection);

            int inserted = cmdInsertDefaults.ExecuteNonQuery();
            _logger.LogInformation("Inserted default depots: {Count} rows.", inserted);
        }
    }

    private void EnsurePrimaryKeyOnId(SqlConnection connection)
    {
        using var cmd = new SqlCommand(@"
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'Depots'
      AND COLUMN_NAME = 'Id'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = OBJECT_ID(N'[dbo].[Depots]')
          AND kc.type = 'PK'
    )
    BEGIN
        ALTER TABLE [dbo].[Depots]
        ADD PRIMARY KEY CLUSTERED ([Id] ASC);
    END
END", connection);

        cmd.ExecuteNonQuery();
    }

    private void EnsureActiveDefault(SqlConnection connection)
    {
        using var cmd = new SqlCommand(@"
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'Depots'
      AND COLUMN_NAME = 'Active'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON dc.parent_object_id = c.object_id
           AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Depots]')
          AND c.name = N'Active'
    )
    BEGIN
        ALTER TABLE [dbo].[Depots]
        ADD CONSTRAINT [DF_Depots_Active] DEFAULT ((1)) FOR [Active];
    END
END", connection);

        cmd.ExecuteNonQuery();
    }

    private void EnsureIsDeletedDefault(SqlConnection connection)
    {
        using var cmd = new SqlCommand(@"
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'Depots'
      AND COLUMN_NAME = 'IsDeleted'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON dc.parent_object_id = c.object_id
           AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Depots]')
          AND c.name = N'IsDeleted'
    )
    BEGIN
        ALTER TABLE [dbo].[Depots]
        ADD CONSTRAINT [DF_Depots_IsDeleted] DEFAULT ((0)) FOR [IsDeleted];
    END
END", connection);

        cmd.ExecuteNonQuery();
    }

    private void EnsureCreatedAtDefault(SqlConnection connection)
    {
        using var cmd = new SqlCommand(@"
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'Depots'
      AND COLUMN_NAME = 'CreatedAt'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON dc.parent_object_id = c.object_id
           AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Depots]')
          AND c.name = N'CreatedAt'
    )
    BEGIN
        ALTER TABLE [dbo].[Depots]
        ADD CONSTRAINT [DF_Depots_CreatedAt] DEFAULT (SYSDATETIMEOFFSET()) FOR [CreatedAt];
    END
END", connection);

        cmd.ExecuteNonQuery();
    }

    public static void Run(IServiceProvider services, bool forMaster, string? optionalConnectionString = null)
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<DepotsTableBuilder>>();
            var config = services.GetRequiredService<IConfiguration>();

            string connectionString;
            if (!string.IsNullOrWhiteSpace(optionalConnectionString))
            {
                connectionString = optionalConnectionString!;
            }
            else
            {
                var tempConnectionString = config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(tempConnectionString))
                {
                    throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");
                }

                connectionString = tempConnectionString;
            }

            var builder = new DepotsTableBuilder(connectionString, logger);

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
            fallbackLogger?.LogError(ex, "Error running DepotsTableBuilder.Run");
        }
    }
}