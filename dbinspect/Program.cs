using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        var path = "E:/monAppGestion/database.db";
        var cs = $"Data Source={path}";
        using var conn = new SqliteConnection(cs);
        conn.Open();

        Console.WriteLine("Tables:");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var name = rdr.IsDBNull(0) ? "" : rdr.GetString(0);
                var sql = rdr.IsDBNull(1) ? "" : rdr.GetString(1);
                Console.WriteLine($"{name}: {sql}");
            }
        }

        Console.WriteLine("--- Ventes rows ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, NumVente, Date FROM Ventes";
            try
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var id = rdr.IsDBNull(0) ? "NULL" : rdr.GetInt32(0).ToString();
                    var num = rdr.IsDBNull(1) ? "" : rdr.GetString(1);
                    var date = rdr.IsDBNull(2) ? "" : rdr.GetString(2);
                    Console.WriteLine($"{id}|{num}|{date}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading Ventes: " + ex.Message);
            }
        }

        Console.WriteLine("--- VenteDetails rows ---");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, IdVente, IdProduit, PrixVente, Qte FROM VenteDetails";
            try
            {
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var id = rdr.IsDBNull(0) ? "NULL" : rdr.GetInt32(0).ToString();
                    var idV = rdr.IsDBNull(1) ? "NULL" : rdr.GetInt32(1).ToString();
                    var idP = rdr.IsDBNull(2) ? "NULL" : rdr.GetInt32(2).ToString();
                    var prix = rdr.IsDBNull(3) ? "NULL" : rdr.GetDouble(3).ToString();
                    var qte = rdr.IsDBNull(4) ? "NULL" : rdr.GetInt32(4).ToString();
                    Console.WriteLine($"{id}|{idV}|{idP}|{prix}|{qte}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading VenteDetails: " + ex.Message);
            }
        }

        conn.Close();
    }
}
