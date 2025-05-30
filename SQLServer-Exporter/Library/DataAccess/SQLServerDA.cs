using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SQLServerExporter.Library.Data;

namespace SQLServerExporter.Library.DataAccess
{
        public class SQLServerDA
        {
            private readonly SQLServerDB database;
            public SQLServerDA(SQLServerDB _database) 
            {
                database = _database;
            }

            private DataTable RemoveColumns(DataTable datatable, List<string> columnsToRemove)
            {
                foreach (string columnName in columnsToRemove)
                {
                    if (datatable.Columns.Contains(columnName))
                    {
                        datatable.Columns.Remove(columnName);
                    }
                }
                return datatable;
            }
            private DataTable RetainColumns(DataTable datatable, List<string> columnsToRetain)
            {
                foreach (DataColumn column in datatable.Columns.Cast<DataColumn>().ToList())
                {
                    if (!columnsToRetain.Contains(column.ColumnName))
                    {
                        datatable.Columns.Remove(column.ColumnName);
                    }
                }

                return datatable;
            }

            #region   -------------------SQl Data Reader/Adapters-------------------
            private DataTable GetDataTable(SqlCommand command)
            {
                try
                { 
                    DataTable dataTable = new DataTable();
                    using (SqlCommand cmd = command)
                    { 
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                    command.Dispose();
                    return dataTable;
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                }
                command?.Dispose();
                return null;
            }
            private async Task<DataTable> GetDataTableAsync(SqlCommand command)
            {
                try
                {
                    DataTable dataTable = new DataTable();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            dataTable.Load(reader);
                        }
                    }
                    command.Dispose();
                    return dataTable;
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                }
                command?.Dispose();
                return null;
            }
            #endregion =======================================================

            #region   ----------------------SQLCommand Builders---------------------
            private SqlCommand SqlCommand_GetTableColumns(string tableName)
            {
                try
                {
                    string query = @"
                        SELECT COLUMN_NAME as 'Columns' 
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @TableName
                        ORDER BY ORDINAL_POSITION";

                    SqlCommand command = new SqlCommand(query, database.SqlConn);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    return command;
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                }
                return null;
            }
            private SqlCommand SqlCommand_GetAllDBTables()
            {
                try
                {
                    string query = @"
                        SELECT TABLE_NAME as 'Tables' 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = 'BASE TABLE' AND
                              TABLE_Name <> 'sysdiagrams'
                        ORDER BY TABLE_NAME";

                    SqlCommand command = new SqlCommand(query, database.SqlConn);

                    return command;
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                }
                return null;
            }
            private SqlCommand SqlCommand_GetBetweenDateRange(string tableName, DateTime fromDate, DateTime toDate)
            {
                try 
                {
                    DataTable datatable = GetTableColumns(tableName);

                    List<string> columnList = new List<string>();
                    foreach (DataRow row in datatable.Rows)
                    {
                        if (string.IsNullOrWhiteSpace(row["Columns"].ToString())) { continue; }
                        if (row["Columns"].ToString() == "TEXT_LOG") { continue; }
                        columnList.Add(row["Columns"].ToString());
                    }
                    string columns = string.Join(",", columnList);

                    // Force cast start time to date only para date lng hanapin. DateTime.Date may time padin 12am
                    string query = $@"
                        SELECT {columns}
                        FROM {tableName}
                        WHERE CAST(START_TIME AS DATE) BETWEEN @StartDate AND @EndDate
                              AND SERIAL_NUMBER NOT LIKE '%OLD%'";

                    SqlCommand command = new SqlCommand(query, database.SqlConn);
                    command.Parameters.AddWithValue("@StartDate", fromDate);
                    command.Parameters.AddWithValue("@EndDate", toDate);

                    return command;
                }
                catch (Exception ex)
                {
                    string methodName = MethodBase.GetCurrentMethod().Name;
                    Debug.Write($">> [{methodName}] Error: " + ex.ToString());
                }
                return null;
            }
            #endregion =======================================================

            public DataTable GetDBTableList()
            {
                SqlCommand command = SqlCommand_GetAllDBTables();
                return GetDataTable(command);
            }
            public DataTable GetTableColumns(string tableName)
            {
                SqlCommand command = SqlCommand_GetTableColumns(tableName);
                return GetDataTable(command);
            }
            public DataTable GetTableByDateRange(string tableName, DateTime fromDate, DateTime toDate)
            {
                SqlCommand command = SqlCommand_GetBetweenDateRange(tableName, fromDate, toDate);
                return GetDataTable(command);
            }
            public async Task<DataTable> GetTableByDateRangeAsync(string tableName, DateTime fromDate, DateTime toDate)
            {
                SqlCommand command = SqlCommand_GetBetweenDateRange(tableName, fromDate, toDate);
                return await GetDataTableAsync(command);
            }
            public DataTable GetTableByDateRangeWithColumns(string tableName, DateTime fromDate, DateTime toDate, List<string> columnsToRetain)
            {
                SqlCommand command = SqlCommand_GetBetweenDateRange(tableName, fromDate, toDate);
                DataTable table = GetDataTable(command);
                return RetainColumns(table, columnsToRetain);
            }
            public async Task<DataTable> GetTableByDateRangeWithColumnsAsync(string tableName, DateTime fromDate, DateTime toDate, List<string> columnsToRetain)
            {
                SqlCommand command = SqlCommand_GetBetweenDateRange(tableName, fromDate, toDate);
                DataTable table = await GetDataTableAsync(command);
                return RetainColumns(table, columnsToRetain);
            }
        }
}
