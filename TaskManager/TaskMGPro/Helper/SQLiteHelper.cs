using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMGPro.Helper
{
    public class SQLiteHelper
    {
        private static SQLiteHelper _instance;
        private static readonly object _lock = new object();
        private readonly string connectionString;
        private bool _isInitialized = false;

        public SQLiteHelper()
        {
            string dbPath = Path.Combine(Path.GetTempPath(), "TaskManager", "Caches", "Db");
            if (!Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
            connectionString = $"Data Source={Path.Combine(dbPath, "database.db")}";
        }
        // 私有构造函数

        public static SQLiteHelper Instance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SQLiteHelper();
                        _instance.InitializeDatabase(); // 初始化数据库
                    }
                }
            }
            return _instance;
        }

        private void InitializeDatabase()
        {
            if (!_isInitialized)
            {
                LoadSqlScript("init.sql");
                _isInitialized = true; // 标记为已初始化
            }
        }

        private void LoadSqlScript(string fileName)
        {
            // 获取项目根目录
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(rootDirectory, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"SQL script file not found: {filePath}");
            }

            string sql = File.ReadAllText(filePath);
            Execute(sql);
        }
        public int Execute(string sql, object? model = null)
        {
            var reslut = 0;
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    SqliteParameter[] parameters = null;
                    if (model != null)
                    {
                        parameters = model.ToSqliteParameters();
                    }
                    if (parameters != null)
                    {
                        foreach (var v in parameters)
                        {
                            command.Parameters.Add(v);
                        }
                    }
                    reslut = command.ExecuteNonQuery();
                }
            }
            return reslut;
        }

        public List<T> Query<T>(string sql, Func<IDataReader, T> mapper, params (string, object)[] parameters)
        {
            var results = new List<T>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    foreach (var (paramName, paramValue) in parameters)
                    {
                        command.Parameters.AddWithValue(paramName, paramValue);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(mapper(reader));
                        }
                    }
                }
            }

            return results;
        }
        public List<T> Query<T>(string sql, params (string, object)[] parameters)
        {
            var results = new List<T>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    foreach (var (paramName, paramValue) in parameters)
                    {
                        command.Parameters.AddWithValue(paramName, paramValue);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(Mapper.Map<T>(reader));
                        }
                    }
                }
            }

            return results;
        }
        public List<T> Query<T>(string sql, object? model)
        {
            var results = new List<T>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    SqliteParameter[] parameters = null;
                    if (model != null)
                    {
                        parameters = model.ToSqliteParameters();
                    }
                    if (parameters != null)
                    {
                        foreach (var v in parameters)
                        {
                            command.Parameters.Add(v);
                        }
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(Mapper.Map<T>(reader));
                        }
                    }
                }
            }

            return results;
        }
    }
}
