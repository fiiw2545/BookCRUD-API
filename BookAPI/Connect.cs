using Npgsql;

namespace BookAPI
{
    public class Connect
    {
        public NpgsqlConnection GetConnection()
        {
            try
            {
                string? connectionString =
                    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception(
                        "DB_CONNECTION_STRING is not configured."
                    );
                }

                NpgsqlConnection conn =
                    new NpgsqlConnection(connectionString);

                conn.Open();

                return conn;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

