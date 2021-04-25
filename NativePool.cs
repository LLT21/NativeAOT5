using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;

namespace tinykestrel
{
   public static class NativePool
   {
      private static List<IDbConnection> _dbConnections = new List<IDbConnection>();

      public static IDbConnection GetConnection()
      {
         IDbConnection dbConnection;

         lock (_dbConnections)
         {
            if (_dbConnections.Count > 0)
            {
               dbConnection = _dbConnections[0];
               _dbConnections.RemoveAt(0);
            }
            else
            {
               dbConnection = new OdbcConnection(ConnectionStrings.OdbcConnection);
            }
         }

         return dbConnection;
      }

      public static void ReleaseConnection(IDbConnection dbConnection)
      {
         _dbConnections.Add(dbConnection);
      }
   }
}