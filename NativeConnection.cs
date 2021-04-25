using System.Data;
using System.Threading.Tasks;

namespace tinykestrel
{
   public class NativeConnection : IDbConnection
   {
      private IDbConnection _dbConnection;

      public NativeConnection()
      {
         _dbConnection = NativePool.GetConnection();
      }

      public string ConnectionString
      {
         get
         {
            return _dbConnection.ConnectionString;
         }
         set
         {
            _dbConnection.ConnectionString = value;
         }
      }

      public int ConnectionTimeout
      {
         get
         {
            return _dbConnection.ConnectionTimeout;
         }
      }

      public string Database
      {
         get
         {
            return _dbConnection.Database;
         }
      }

      public ConnectionState State
      {
         get
         {
            return _dbConnection.State;
         }
      }

      public IDbTransaction BeginTransaction()
      {
         return _dbConnection.BeginTransaction();
      }

      public IDbTransaction BeginTransaction(IsolationLevel il)
      {
         return _dbConnection.BeginTransaction(il);
      }

      public void ChangeDatabase(string databaseName)
      {
         _dbConnection.ChangeDatabase(databaseName);
      }

      public void Close()
      {
         NativePool.ReleaseConnection(_dbConnection);
      }

      public IDbCommand CreateCommand()
      {
         return _dbConnection.CreateCommand();
      }

      public void Open()
      {
         if (_dbConnection.State == ConnectionState.Closed)
         {
            _dbConnection.Open();
         }
      }

      public void Dispose()
      {
         if (_dbConnection.State == ConnectionState.Open)
         {
            NativePool.ReleaseConnection(_dbConnection);
         }
      }

      public async Task OpenAsync()
      {
         await ((System.Data.Common.DbConnection)_dbConnection).OpenAsync();
      }
   }
}