using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        // Resolve repository database path by checking several candidate locations relative to the running binary
        string ResolveDatabasePath()
        {
            var candidates = new string[]
            {
                System.IO.Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\database.db"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\database.db"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "..\\..\\database.db"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "..\\database.db"),
                System.IO.Path.Combine(Environment.CurrentDirectory, "database.db"),
                System.IO.Path.Combine(Environment.CurrentDirectory, "querydb", "database.db")
            };

            foreach (var c in candidates)
            {
                try
                {
                    var full = System.IO.Path.GetFullPath(c);
                    if (System.IO.File.Exists(full) && new System.IO.FileInfo(full).Length > 0)
                        return full;
                }
                catch { }
            }

            // fallback to database.db in repository root relative to base dir
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\database.db"));
        }

        var dbPath = ResolveDatabasePath();
        Console.WriteLine($"Using database file: {dbPath}");
        var cs = $"Data Source={dbPath}";
        using var conn = new SqliteConnection(cs);
        conn.Open();

        Console.WriteLine("Checking Ventes table for Versement column...");

        bool hasVersement = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Ventes');";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var colName = rdr.IsDBNull(1) ? "" : rdr.GetString(1);
                if (string.Equals(colName, "Versement", StringComparison.OrdinalIgnoreCase))
                {
                    hasVersement = true;
                    break;
                }
            }
        }

        if (!hasVersement)
        {
            Console.WriteLine("Adding Versement column to Ventes...");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "ALTER TABLE Ventes ADD COLUMN Versement REAL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("Versement column added.");
        }

        // Ensure __EFMigrationsHistory exists and contains entries for existing migrations
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL CONSTRAINT PK__EFMigrationsHistory PRIMARY KEY, ProductVersion TEXT NOT NULL);";
            cmd.ExecuteNonQuery();
        }

        // Insert migration records if missing
        void InsertIfMissing(string migrationId)
        {
            using var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = "SELECT COUNT(1) FROM __EFMigrationsHistory WHERE MigrationId = $id";
            cmdCheck.Parameters.AddWithValue("$id", migrationId);
            var exists = Convert.ToInt32(cmdCheck.ExecuteScalar() ?? 0) > 0;
            if (!exists)
            {
                using var cmdIns = conn.CreateCommand();
                cmdIns.CommandText = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ($id, $pv);";
                cmdIns.Parameters.AddWithValue("$id", migrationId);
                cmdIns.Parameters.AddWithValue("$pv", "8.0.0");
                cmdIns.ExecuteNonQuery();
                Console.WriteLine($"Inserted migration record: {migrationId}");
            }
        }

        // mark initial create and current migration as applied
        InsertIfMissing("20260328074638_InitialCreate");
        InsertIfMissing("20260404131414_AddVersementToVentes");

        Console.WriteLine("Done.");
        conn.Close();
    }
}
