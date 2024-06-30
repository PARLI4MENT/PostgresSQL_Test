﻿using Npgsql;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PgSql
{
    public class PgSql
    {
        public static string Server { get; private set; } = "192.168.0.142";
        public static string Port { get; private set; } = "5438";
        public static string Database { get; private set; } = "testdb";
        public static string Uid { get; private set; } = "postgres";
        public static string Password { get; private set; } = "passwd0105";

        private static string _tableName = "";


        string strConnMain = $"Server={Server};Port={Port};Database={Database};Uid={Uid};Pwd={Password}";

        public PgSql()
        {

        }

        public PgSql(string Server, string Port, string Uid, string Password)
        {
            PgSql.Server = Server;
            PgSql.Port = Port;
            PgSql.Database = Uid;
            PgSql.Password = Password;
        }

        public delegate void PgDataOut(string tableName, int iteration = 100);

        public async void PgSqlConnect()
        {
            // Connection
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(strConnMain);
            var dataSource = dataSourceBuilder.Build();
            var sqlConnection = await dataSource.OpenConnectionAsync();

            if (sqlConnection.State == ConnectionState.Open)
                Console.WriteLine("State => is Open");
            else
                Console.WriteLine("State => wasn`t Open");

            //PgCheckDb(sqlConnection);

            PgRetriveData(sqlConnection);
            //PgInsertData(sqlConnection);
        }

        private async void PgSqlCreateDatabase()
        {
            string strDbName = "testDB";
            string strConn = "Server=192.168.0.142;Port=5438;Uid=postgres;Pwd=passwd0105;";
            string strComm = @$"CREATE DATABASE {strDbName} WITH OWNER postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;";

            await using (var sqlConn = new NpgsqlConnection(strConn))
            {
                Debug.WriteLine(sqlConn.State.ToString());
                sqlConn.Open();
                Debug.WriteLine(sqlConn.State.ToString());
                var sqlComm = new NpgsqlCommand(strComm, sqlConn);
                sqlComm.ExecuteNonQuery();
                Debug.WriteLine("Query is Done!");
                sqlConn.Close();
                Debug.WriteLine(sqlConn.State.ToString());

                PgSqlCreateTable();
            }

        }

        public async void PgSqlCreateTable()
        {
            string strCreateTable = @"CREATE TABLE ""public"".""testTabel1"" (
                ""ID"" int4 NOT NULL GENERATED ALWAYS AS IDENTITY (INCREMENT 1), ""test1"" bool, ""test2"" char, ""test3"" varchar(255),
                ""test4"" decimal(10,2), ""test5"" float8, ""test6"" int8, ""test7"" text, ""test8"" varchar(255), ""test9"" varchar(255),
                PRIMARY KEY (""ID""));";

            try
            {
                await using (var sqlConn = new NpgsqlConnection(strConnMain))
                {
                    await sqlConn.OpenAsync();
                    await using (var sqlComm = new NpgsqlCommand(strCreateTable, sqlConn))
                    {
                        await sqlComm.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                switch (ex.SqlState)
                {
                    case "42P07":
#if DEBUG
                        Console.WriteLine($"[Msg] => {ex.Message}");
                        Console.WriteLine($"[Source] => {ex.Source}");
                        Console.WriteLine($"[Data] => {ex.Data}");
                        Console.WriteLine($"[SqlState] => {ex.SqlState}");
                        Console.WriteLine($"[BatchCommand] => {ex.BatchCommand}");
                        Console.WriteLine($"[IsTransient] => {ex.IsTransient}");
#endif
                        break;
                    case "3D000":
#if DEBUG
                        Console.WriteLine($"[Msg] => {ex.Message}");
                        Console.WriteLine($"[Source] => {ex.Source}");
                        Console.WriteLine($"[Data] => {ex.Data}");
                        Console.WriteLine($"[SqlState] => {ex.SqlState}");
                        Console.WriteLine($"[BatchCommand] => {ex.BatchCommand}");
                        Console.WriteLine($"[IsTransient] => {ex.IsTransient}");
#endif
                        PgSqlCreateDatabase();
                        break;
                    default:
                        break;
                }
            }
        }

        private async static void PgSqlCheckDb(NpgsqlConnection sqlConnection, [Optional] string tableName)
        {
            // Get all tables in current Database
            try
            {
                #region Get tables

                const string listTables = "SELECT table_name FROM information_schema.tables WHERE table_schema='public'";
                await using (var sqlComm = new NpgsqlCommand(listTables, sqlConnection))
                {
                    await using (var reader = sqlComm.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
#if DEBUG
                            Console.WriteLine("No tables!");
#endif
                            return;
                        }
                        int i = 0;
                        while (await reader.ReadAsync())
                        {
                            //Console.WriteLine(reader.FieldCount.ToString());
                            Console.WriteLine($"[{i++}]\t{reader.GetString(0)}");
                        }

                    }
                }
                #endregion

                #region Get fields from current table

                string fildsTable = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}'";
                await using (var sqlComm = new NpgsqlCommand(fildsTable, sqlConnection))
                {
                    await using (var reader = sqlComm.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
#if DEBUG
                            Console.WriteLine("No filds!");
#endif
                            return;
                        }
                        int i = 0;
                        while (reader.Read())
                        {
                            Console.WriteLine($"[{i++}]\t{reader.GetString(0)} => {reader.GetString(1)}");
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return; }
            #endregion
        }

        private async void PgInsertData(string tableName, int iteration = 100)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            try
            {
                string strCommand = @$"INSERT INTO ""public"".""{tableName}"" (
                        test1, test2, test3, test4, test5, test6, test7, test8, test9)
                        VALUES ({true}, 'c', {DateTime.Now.ToString("yyyy-MM-dd")}, 3.14, 3.14, 1, 'some_text', 'some_text', 'some_text');";

                await using (var sqlConnection = new NpgsqlConnection(strConnMain))
                {

                    await sqlConnection.OpenAsync();
                    int i = 0;
                    await using (var sqlCommand = new NpgsqlCommand(strCommand, sqlConnection))
                        while (i < iteration)
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                            i++;
                        }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return; }
#if DEBUG
            sw.Stop();
            Console.WriteLine("\nAsync requests:");
            Console.WriteLine($"Requests => {iteration} units");
            Console.WriteLine($"Total time => {sw.ElapsedMilliseconds / 1000} sec ({sw.ElapsedMilliseconds} ms)");
            Console.WriteLine($"Average => {(long)iteration / (sw.ElapsedMilliseconds / 1000)} q/s");
#endif
        }

        private void PgInsertDataParallel(string tableName, int iteration)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            string strCommand = @$"INSERT INTO ""public"".""{tableName}"" (
                        test1, test2, test3, test4, test5, test6, test7, test8, test9)
                        VALUES ({true}, 'c', {DateTime.Now.ToString("yyyy-MM-dd")}, 3.14, 3.14, 1, 'some_text', 'some_text', 'some_text');";

            Parallel.For(0, iteration, i =>
            {
                using (var sqlConnection = new NpgsqlConnection(strConnMain))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = new NpgsqlCommand(strCommand, sqlConnection))
                        sqlCommand.ExecuteNonQuery();
                }
            });
#if DEBUG
            sw.Stop();
            Console.WriteLine("\nParallels requests:");
            Console.WriteLine($"Requests => {iteration} units");
            Console.WriteLine($"Total time => {sw.ElapsedMilliseconds / 1000} sec ({sw.ElapsedMilliseconds} ms)");
            Console.WriteLine($"Average => {(long)iteration / (sw.ElapsedMilliseconds / 1000)} q/s");
#endif
        }

        private async static void PgRetriveData(NpgsqlConnection sqlConnection)
        {
            await using (var sqlComm = new NpgsqlCommand("SELECT * FROM \"public\".\"testTabel1\"", sqlConnection))
            {
                await using (NpgsqlDataReader reader = await sqlComm.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
#if DEBUG
                        Console.WriteLine("No rows!");
#endif
                        return;
                    }

                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine(reader.GetString(0));
                    }
                }
            }
        }

        public async void PgClearData(string tableName)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            string strCommand = $@"DELETE FROM""public"".""{tableName}""";

            await using (var sqlConnection = new NpgsqlConnection(strConnMain))
            {
                await sqlConnection.OpenAsync();
                await using (var sqlCommand = new NpgsqlCommand(strCommand, sqlConnection))
                    sqlCommand.ExecuteNonQuery();
            }
#if DEBUG
            sw.Stop();
            Console.WriteLine($"\nClear data from {tableName}");
            Console.WriteLine($"Total time => {sw.ElapsedMilliseconds / 1000} sec ({sw.ElapsedMilliseconds} ms)");
#endif
        }
    }
}