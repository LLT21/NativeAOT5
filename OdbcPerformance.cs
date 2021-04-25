using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace tinykestrel
{
   public static class OdbcPerformance
   {
      public static void Test()
      {
         Stopwatch stopwatch = new Stopwatch();
         stopwatch.Start();

         for (int i = 1; i <= 50; i++)
         {
            using (IDbConnection connection = new NativeConnection())
            {
               ReadContacts(connection);
               connection.Close();
            }
         }

         stopwatch.Stop();
         Console.WriteLine(stopwatch.ElapsedMilliseconds);

         /*
         stopwatch = new Stopwatch();
         stopwatch.Start();

         for (int i = 1; i <= 50; i++)
         {
            using (IDbConnection connection = new SqlConnection(ConnectionStrings.SqlConnection))
            {
               ReadContacts(connection);
            }
         }

         stopwatch.Stop();
         Console.WriteLine(stopwatch.ElapsedMilliseconds);
         */
      }

      public static string ReadContacts(IDbConnection dbConnection)
      {
         string names = string.Empty;

         using (IDbCommand dbCommand = dbConnection.CreateCommand())
         {
            dbCommand.CommandText = "SELECT * FROM Contact";
            dbCommand.CommandType = CommandType.Text;

            dbConnection.Open();

            using (IDataReader dataReader = dbCommand.ExecuteReader())
            {
               while (dataReader.Read())
               {
                  names += ", " + Convert.ToString(dataReader["Name1"]);
               }
            }
         }

         return names;
      }
   }
}