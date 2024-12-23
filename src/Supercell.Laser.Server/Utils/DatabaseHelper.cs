namespace Supercell.Laser.Server.Utils
{
    using MySql.Data.MySqlClient;
    using Supercell.Laser.Server.Settings;

    public class DatabaseHelper
    {
        private static string GetConnectionString()
        {
            return $"server=127.0.0.1;"
                + $"user={Configuration.Instance.DatabaseUsername};"
                + $"database={Configuration.Instance.DatabaseName};"
                + $"port=3306;"
                + $"password={Configuration.Instance.DatabasePassword}";
        }

        public static string ExecuteScalar(string query, params (string, object)[] parameters)
        {
            try
            {
                using (MySqlConnection connection = new(GetConnectionString()))
                {
                    connection.Open();

                    MySqlCommand cmd = new(query, connection);
                    foreach ((string paramName, object paramValue) in parameters)
                    {
                        cmd.Parameters.AddWithValue(paramName, paramValue);
                    }

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "N/A";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static bool ExecuteNonQuery(string query, params (string, object)[] parameters)
        {
            try
            {
                using (MySqlConnection connection = new(GetConnectionString()))
                {
                    connection.Open();

                    MySqlCommand cmd = new(query, connection);
                    foreach ((string paramName, object paramValue) in parameters)
                    {
                        cmd.Parameters.AddWithValue(paramName, paramValue);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
    }
}