using MySql.Data.MySqlClient;



namespace Server
{
    class DataBaseManager : Singleton<DataBaseManager>
    {
        private const string m_username = ""; // 用户名，默认root
        private const string m_password = ""; // 密码



        public void SetDataBase(string operation, string value, int count)
        {
            if (operation == "insert")
            {
                InsertData("databaseName", "tableName", "columnName", "varchar(100)", value);
            }
            else if (operation == "delete")
            {
                DeleteData("databaseName", "tableName", "columnName", count);
            }
            else if (operation == "update")
            {
                UpdateData("databaseName", "tableName", "columnName", count, value);
            }
            else if (operation == "select")
            {
                SelectData("databaseName", "tableName", out Dictionary<int, string> rowsData, out Dictionary<string, string> columnsData, out string singleData, "columnName", count);
                Console.WriteLine($"查找到的数据是：{singleData}");
            }
            else if (operation == "close")
            {
                Environment.Exit(0);
            }
        }

        private void InsertData(string databaseName, string tableName, string columnName, string columnType, string value)
        {
            bool isExistsDataBase = JudgeIsExists(databaseName);

            if (!isExistsDataBase)
            {
                CreateDatabase(databaseName);
            }

            bool isExistsTable = JudgeIsExists(databaseName, tableName);

            if (!isExistsTable)
            {
                CreateTable(databaseName, tableName);
            }

            /*
              Server=localhost：指定数据库的服务器地址为本地主机，即本地的数据库服务器。
              Port=3306：指定数据库服务器的端口号为 3306，默认情况下 MySQL 数据库的端口号为 3306。
              Uid=root：指定连接数据库所使用的用户名为 "root"，您可以根据实际情况替换为您的用户名。
              Pwd=yourpassword：指定连接数据库所使用的密码，您需要将 "yourpassword" 替换为实际的密码。
              Database=yourdatabasename：指定要连接的数据库名称，您需要将 "yourdatabasename" 替换为实际存在的数据库名称。
              SslMode=Required：指定连接数据库时需要使用 SSL 加密，确保数据传输的安全性。
            */
            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                bool isExistsColumn = JudgeIsExists(databaseName, tableName, columnName);

                if (!isExistsColumn)
                {
                    string commandString = $"ALTER TABLE {tableName} ADD {columnName} {columnType}";

                    using (MySqlCommand command = new MySqlCommand(commandString, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine($"成功创建新列---类型：{columnType}，名称：{columnName}");
                    }
                }

                string insertCommandString = $"INSERT INTO {tableName} ({columnName}) VALUES ('{value}')";

                using (MySqlCommand command = new MySqlCommand(insertCommandString, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"成功插入数据：{value}");
                }
            }
        }

        private void DeleteData(string databaseName, string tableName, string columnName = "", int primaryKeyValue = -1)
        {
            bool isExistsColumn = JudgeIsExists(databaseName, tableName, columnName);

            if (!isExistsColumn)
            {
                return;
            }

            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string commandString = "";

                if (string.IsNullOrEmpty(columnName) && primaryKeyValue != -1)
                {
                    commandString = $"DELETE FROM {tableName} WHERE id = @Id AND {columnName} IN (SELECT name FROM {tableName} WHERE id = @Id)";
                }
                else if (string.IsNullOrEmpty(columnName))
                {
                    commandString = $"ALTER TABLE {tableName} DROP COLUMN {columnName}";
                }
                else if (primaryKeyValue != -1)
                {
                    commandString = $"DELETE FROM {tableName} WHERE id = @Id";
                }

                if (!string.IsNullOrEmpty(commandString))
                {
                    using (MySqlCommand command = new MySqlCommand(commandString, connection))
                    {
                        if (commandString.Contains("@Id"))
                        {
                            command.Parameters.AddWithValue("@Id", primaryKeyValue);
                        }

                        try
                        {
                            int rows = command.ExecuteNonQuery();
                            Console.WriteLine("删除成功的行数：" + rows);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("删除失败：" + ex.ToString());
                        }
                    }
                }
            }
        }

        private void UpdateData(string databaseName, string tableName, string columnName, int primaryKeyValue, string value)
        {
            bool isExistsColumn = JudgeIsExists(databaseName, tableName, columnName);

            if (!isExistsColumn)
            {
                return;
            }

            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string commandString = $"UPDATE {tableName} SET {columnName} = @newValue WHERE id = @primaryKeyValue";

                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    command.Parameters.AddWithValue("@newValue", value);
                    command.Parameters.AddWithValue("@primaryKeyValue", primaryKeyValue);

                    command.ExecuteNonQuery();

                    Console.WriteLine($"修改{columnName}列的{primaryKeyValue}行为{value}");
                }
            }
        }

        private void SelectData(string databaseName, string tableName, out Dictionary<int, string> rowsData, out Dictionary<string, string> columnsData, out string singleData, string columnName = "", int primaryKeyValue = -1)
        {
            rowsData = new Dictionary<int, string>();
            columnsData = new Dictionary<string, string>();
            singleData = "";

            bool isExistsColumn = JudgeIsExists(databaseName, tableName, columnName);

            if (isExistsColumn)
            {
                string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    if (string.IsNullOrEmpty(columnName) && primaryKeyValue != -1)
                    {
                        string commandString = $"SELECT {columnName} FROM {tableName} WHERE id = {primaryKeyValue}";

                        using (MySqlCommand command = new MySqlCommand(commandString, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        singleData = reader.GetString(0);
                                    }
                                }
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(columnName))
                    {
                        string commandString = $"SELECT id, {columnName} FROM {tableName}";

                        using (MySqlCommand command = new MySqlCommand(commandString, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        rowsData.Add(reader.GetInt32(0), reader.GetString(1));
                                    }
                                }
                            }
                        }
                    }
                    else if (primaryKeyValue != -1)
                    {
                        string commandString = $"SELECT * FROM {tableName} WHERE id = {primaryKeyValue}";

                        using (MySqlCommand command = new MySqlCommand(commandString, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        columnsData.Add("id", reader.GetInt32(0).ToString());

                                        for (int i = 1; i < reader.FieldCount; i++)
                                        {
                                            columnsData.Add(reader.GetName(i), reader.GetString(i));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateDatabase(string databaseName)
        {
            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};SslMode=Required";

            // 创建数据库连接对象
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // 打开数据库连接
                connection.Open();

                // 数据库命令
                string commandString = $"CREATE DATABASE {databaseName}";

                // 创建执行数据库命令的对象
                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"数据库创建成功：{databaseName}");
                }
            }
        }

        private void DeleteDatabase(string databaseName)
        {
            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};SslMode=Required";

            // 创建数据库连接对象
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // 打开数据库连接
                connection.Open();

                // 数据库命令
                string commandString = $"DROP DATABASE {databaseName}";

                // 创建执行数据库命令的对象
                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"数据库删除成功：{databaseName}");
                }
            }
        }

        private void CreateTable(string databaseName, string tableName)
        {
            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string commandString = $"CREATE TABLE {tableName} (id INT AUTO_INCREMENT PRIMARY KEY)";

                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"表创建成功：{tableName}");
                }
            }
        }

        private void DeleteTable(string databaseName, string tableName)
        {
            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string commandString = $"DROP TABLE {tableName}";

                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine($"表删除成功：{tableName}");
                }
            }
        }

        private bool JudgeDataBaseIsExists(string databaseName)
        {
            bool isExists = false;

            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};SslMode=Required";

            // 创建连接对象
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // 打开数据库连接
                connection.Open();

                // 检查数据库是否已存在
                string commandString = $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{databaseName}'";

                // 创建数据库命令对象
                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    object result = command.ExecuteScalar();
                    isExists = result != null;
                }
            }

            return isExists;
        }

        private bool JudgeTableIsExists(string databaseName, string tableName)
        {
            bool isExists = false;

            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            // 创建连接对象
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                // 打开数据库连接
                connection.Open();

                // 检查表是否存在的SQL查询语句
                string commandString = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{databaseName}' AND table_name = '{tableName}'";

                // 创建数据库命令对象
                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    long tableCount = (long)command.ExecuteScalar();
                    isExists = tableCount > 0;
                }
            }

            return isExists;
        }

        private bool JudgeColumnIsExists(string databaseName, string tableName, string columnName)
        {
            bool isExists = false;

            string connectionString = $"Server=localhost;Port=3306;Uid={m_username};Pwd={m_password};Database={databaseName};SslMode=Required";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // 检查列是否存在的SQL查询语句
                string commandString = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{databaseName}' AND table_name = '{tableName}' AND column_name = '{columnName}'";

                // 创建数据库命令对象
                using (MySqlCommand command = new MySqlCommand(commandString, connection))
                {
                    long columnCount = (long)command.ExecuteScalar();
                    isExists = columnCount > 0;
                }
            }

            return isExists;
        }

        private bool JudgeIsExists(string databaseName, string tableName = "", string columnName = "")
        {
            bool isExists = true;

            if (JudgeDataBaseIsExists(databaseName))
            {
                if (!string.IsNullOrEmpty(tableName))
                {
                    if (JudgeTableIsExists(databaseName, tableName))
                    {
                        if (!string.IsNullOrEmpty(columnName))
                        {
                            isExists = JudgeColumnIsExists(databaseName, tableName, columnName);
                        }
                    }
                    else
                    {
                        isExists = false;
                    }
                }
            }
            else
            {
                isExists = false;
            }

            return isExists;
        }
    }
}