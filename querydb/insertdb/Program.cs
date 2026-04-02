using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        var cs = "Data Source=E:/monAppGestion/database.db";
        using var conn = new SqliteConnection(cs);
        conn.Open();

        using var tr = conn.BeginTransaction();
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tr;
            cmd.CommandText = "INSERT INTO Ventes (NumVente, Date) VALUES (@num, @date);";
            cmd.Parameters.AddWithValue("@num", "AUTO_TEST");
            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT last_insert_rowid();";
            var id = (long)cmd.ExecuteScalar();

            cmd.CommandText = "INSERT INTO VenteDetails (IdVente, IdProduit, PrixVente, Qte) VALUES (@idv, @idp, @prix, @qte);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@idv", id);
            cmd.Parameters.AddWithValue("@idp", 1);
            cmd.Parameters.AddWithValue("@prix", 9.99);
            cmd.Parameters.AddWithValue("@qte", 2);
            cmd.ExecuteNonQuery();
        }
        tr.Commit();

        Console.WriteLine("Inserted test vente and detail.");

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM VenteDetails WHERE IdVente = (SELECT MAX(Id) FROM Ventes WHERE NumVente='AUTO_TEST')";
            var c = cmd.ExecuteScalar();
            Console.WriteLine("Detail count for last test sale: " + c);
        }

        conn.Close();
    }
}
