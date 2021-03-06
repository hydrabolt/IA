﻿using System;

namespace IA
{
    public class SqlInformation
    {
        public string dataSource = "";
        public int port = 3306;

        public string database = "";

        public string username = "root";
        public string password = "";

        public SqlInformation(Action<SqlInformation> info)
        {
            info.Invoke(this);
        }

        public string GetConnectionString()
        {
            return string.Format("datasource={0};port={1};Initial Catalog='{2}';username={3};password={4};CharSet=utf8mb4;Allow User Variables=True;", dataSource, port, database, username, password);
        }
    }
}