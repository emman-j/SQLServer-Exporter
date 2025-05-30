using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace SQLServerExporter.Library.Data
{
        public class SQLServerDB : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public SqlConnection SqlConn;
            private bool connectionStatus = false;
            private string _database;
            public string Server { get; set; } //= "10.10.10.25,1433";
            //public string _Database { get; set; } //= "DBApollo2";
            public string UserId { get; set; } //= "sa";
            public string Password { get; set; } //= "s@";
            public int ConnectionTimeout { get; set; } = 45;
            public bool IsConnectionTrusted { get; set; } = false;
            public string database
            {
                get { return _database; }
                set
                {
                    if (_database != value)
                    {
                        _database = value;
                        NotifyPropertyChanged();
                    }
                }
            }
            public bool IsDBConnected
            {
                get { return connectionStatus; }
                set
                {
                    if (connectionStatus != value)
                    {
                        connectionStatus = value;
                        NotifyPropertyChanged();
                    }
                }
            }
            public string ConnectionString
            {
                get
                {
                    if (IsConnectionTrusted)
                    {
                        return $"Server={Server};Database={database};Trusted_Connection=True;MultipleActiveResultSets=true;Connection Timeout={ConnectionTimeout};";
                    }
                    else
                    {
                        return $"Server={Server};Database={database};User ID={UserId};Password={Password};MultipleActiveResultSets=true;Connection Timeout={ConnectionTimeout};";
                    }
                }
            }
            public SQLServerDB() { }
            public SQLServerDB(string server, string database, string username, string password, bool trustedConnection, int timeout = 45)
            {
                Server = server;
                this.database = database;
                UserId = username;
                Password = password;
                ConnectionTimeout = timeout;
                IsConnectionTrusted = trustedConnection;
            }
            public void SetConnectionParameters(string server, string database, string password, string userId, bool trustedconnection)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(server))
                        throw new ArgumentException("Server name cannot be null or empty", nameof(server));

                    if (string.IsNullOrWhiteSpace(database))
                        throw new ArgumentException("Database name cannot be null or empty", nameof(database));

                    if (trustedconnection == false)
                    {
                        if (string.IsNullOrWhiteSpace(userId))
                            throw new ArgumentException("Username/User ID cannot be null or empty", nameof(userId));

                        if (string.IsNullOrWhiteSpace(password))
                            throw new ArgumentException("Password cannot be null or empty", nameof(password));
                        UserId = userId;
                        Password = password;
                    }

                    Server = server;
                    this.database = database;
                    IsConnectionTrusted = trustedconnection;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid Input." + $"\n{ex.Message}", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            public void NotifyPropertyChanged([CallerMemberName] string propertyname = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
            }
            public void OpenConnection()
            {
                int maxRetries = 3;
                int delayBetweenRetries = 2000;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        SqlConn = new SqlConnection(ConnectionString);
                        SqlConn.Open();

                        IsDBConnected = true;
                        return;
                    }
                    #region CATCH  
                    catch (Exception ex)
                    {
                        Debug.WriteLine("-----------------------------Cannot connect to Database------------------------------");
                        Debug.WriteLine($"General Exception: {ex.Message}");
                        IsDBConnected = false;

                        // Wait before retrying
                        if (attempt < maxRetries) { System.Threading.Thread.Sleep(delayBetweenRetries); }

                        else
                        {
                            IsDBConnected = false;
                            throw new Exception("All attempts to connect failed. Please check your connection settings.");
                            //MessageBox.Show("All attempts to connect failed. Please check your connection settings.", "Connection Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //throw; // Rethrow the exception if all attempts fail
                        }
                    }
                    IsDBConnected = false;
                    #endregion
                }
            }
            public async void OpenConnectionAsync()
            {
                int maxRetries = 3;
                int delayBetweenRetries = 2000;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        SqlConn = new SqlConnection(ConnectionString);
                        await SqlConn.OpenAsync();

                        IsDBConnected = true;
                        return;
                    }
                    #region CATCH  
                    catch (Exception ex)
                    {
                        Debug.WriteLine("-----------------------------Cannot connect to Database------------------------------");
                        Debug.WriteLine($"General Exception: {ex.Message}");
                        IsDBConnected = false;

                        if (attempt < maxRetries) { System.Threading.Thread.Sleep(delayBetweenRetries); }

                        //else
                        //{
                        //    IsDBConnected = false;
                        //    throw new Exception("All attempts to connect failed. Please check your connection settings.");
                        //}
                    }
                    IsDBConnected = false;
                    #endregion
                }
            }
            public void CloseConnection()
            {
                try
                {
                    if (IsDBConnected == false) { return; }

                    SqlConn.Close();
                    IsDBConnected = false;
                }
                #region CATCH  
                catch (Exception ex)
                {
                    Debug.WriteLine("-----------------------------Cannot connect to Database------------------------------");
                    Debug.WriteLine($"General Exception: {ex.Message}");
                    IsDBConnected = false;
                }
                #endregion
            }
            public string EnsureConnection(SqlConnection connection, out bool connected)
            {
                connected = false;
                try
                {
                    if (connection == null)
                    {
                        throw new ArgumentNullException(nameof(connection), "The SQL connection is null.");
                    }

                    if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
                    {
                        return "Connection failed";
                    }

                    if (connection.State == ConnectionState.Open)
                    { connected = true;  return "Connected"; }

                }
                catch (SqlException ex)
                {
                    Debug.WriteLine($"SQL Exception: {ex.Message}"); 
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Invalid Operation: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception: {ex.Message}");
                }
                return "Connection failed";
            }
            public async Task<bool> TryReconnectAsync(CancellationToken cancellationToken, int maxRetries = 10, int delayMilliseconds = 2000)
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        SqlConn = new SqlConnection(ConnectionString);
                        await SqlConn.OpenAsync();
                        IsDBConnected = true;
                        return IsDBConnected;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");

                        if (attempt == maxRetries) 
                        {
                            Debug.WriteLine("[TryReconnectAsync] All attempts to connect failed.");
                        }

                        await Task.Delay(delayMilliseconds * attempt, cancellationToken);
                    }
                }
                IsDBConnected = false;
                return false;
            }
        }
}
