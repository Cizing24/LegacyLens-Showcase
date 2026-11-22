using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LegacyLens.Infrastructure
{
    public sealed class Database
    {
        public string ConnectionString { get; private set; }
        public Database(string connectionString) { ConnectionString = connectionString; }
        public static Database Configured
        {
            get { return new Database(ConfigurationManager.ConnectionStrings["LegacyLens"].ConnectionString); }
        }
        public SqlConnection Open()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
        public static SqlParameter P(string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
            if (size > 0) parameter.Size = size;
            return parameter;
        }
        public DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var connection = Open())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var result = new DataTable();
                    adapter.Fill(result);
                    return result;
                }
            }
        }
    }
}
