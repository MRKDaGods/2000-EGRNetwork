using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Threading;

namespace MRK.Data
{
    public class OnDemandDatabase
    {
        private const int Timeout = 5000;
        private const int SleepInterval = 500;

        private readonly string _dataSource;
        private SqliteConnection _connection;
        private Thread _thread;
        private DateTime _lastCommandTime;
        private readonly Reference<bool> _threadRunning;
        private readonly object _lock;

        public OnDemandDatabase(string db)
        {
            _dataSource = db;
            _threadRunning = new Reference<bool>();
            _lock = new object();
        }

        private void CreateConnection()
        {
            _connection = new SqliteConnection(new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                DataSource = _dataSource
            }.ToString());

            _connection.Open();
        }

        private void ThreadLoop()
        {
            while (_threadRunning.Value)
            {
                if (_connection == null
                    || _connection.State == ConnectionState.Closed
                    || (_connection.State == ConnectionState.Open && (DateTime.Now - _lastCommandTime).TotalMilliseconds > Timeout))
                {
                    //kill
                    CloseConnection();
                    _threadRunning.Value = false;
                }

                Thread.Sleep(SleepInterval);
            }

            _thread = null;
        }

        private void CheckConnection()
        {
            lock (_lock)
            {
                if (!_threadRunning.Value)
                {
                    _threadRunning.Value = true;
                    _thread = new Thread(ThreadLoop);
                    _thread.Start();

                    //open connection to db
                    CreateConnection();
                }
            }
        }

        private void Execute()
        {
            _lastCommandTime = DateTime.Now;

            //ensure that we have a connection
            //TODO: make connection thread static for concurrent OPs on the same db
            CheckConnection();
        }

        public void ExecuteNonQuery(string command)
        {
            Execute();

            SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }

        public SqliteDataReader ExecuteReader(string command)
        {
            Execute();

            SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = command;
            return cmd.ExecuteReader();
        }

        private void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
                _connection = null;
            }
        }

        public void Shutdown()
        {
            if (!_threadRunning.Value) return;

            lock (_lock)
            {
                lock (_threadRunning)
                {
                    _threadRunning.Value = false;
                }

                _thread.Join();
                CloseConnection();
            }
        }
    }
}
