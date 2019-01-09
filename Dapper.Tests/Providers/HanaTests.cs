﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sap.Data.Hana;
using Xunit;

namespace Dapper.Tests.Providers
{
    public class HanaTests : TestBase
    {
        private static HanaConnection GetOpenHanaConnection()
        {
            string cs = IsAppVeyor
                ? "Server=hxehost:30015;UID=SYSTEM;Password=Mjm007@1234;Current Schema=SYSTEM"
                : "Server=172.16.26.212:39015;UID=SYSTEM;Password=Mjm007@1234;Current Schema=SYSTEM"; // ;Encoding = UNICODE
            var conn = new HanaConnection(cs);
            conn.Open();
            return conn;
        }

        private class Cat
        {
            public int Id { get; set; }
            public string Breed { get; set; }
            public string Name { get; set; }
        }

        private readonly Cat[] Cats =
        {
            new Cat() { Breed = "Abyssinian", Name="KACTUS"},
            new Cat() { Breed = "Aegean cat", Name="KADAFFI"},
            new Cat() { Breed = "American Bobtail", Name="KANJI"},
            new Cat() { Breed = "Balinese", Name="MACARONI"},
            new Cat() { Breed = "Bombay", Name="MACAULAY"},
            new Cat() { Breed = "Burmese", Name="MACBETH"},
            new Cat() { Breed = "Chartreux", Name="MACGYVER"},
            new Cat() { Breed = "German Rex", Name="MACKENZIE"},
            new Cat() { Breed = "Javanese", Name="MADISON"},
            new Cat() { Breed = "Persian", Name="MAGNA"}
        };

        [FactHana]
        public void TestHanaInsert()
        {
            using (var conn = GetOpenHanaConnection())
            {
                var transaction = conn.BeginTransaction();
                var createTableScriptAnsi = "create table [tcat] ( [id] INT not null primary key generated by default as IDENTITY, [breed] VARCHAR(20) NOT NULL, [name] VARCHAR (20) not null);".Replace('[', '"').Replace(']', '"');
                conn.Execute(createTableScriptAnsi);
                conn.Execute("insert into [tcat]([breed], [name]) values('American Bobtail', 'Xanin') ".ReplaceBracketDoubleQuotes());
                conn.Execute("insert into [tcat]([breed], [name]) values('American Bobtail', 'Lord') ".ReplaceBracketDoubleQuotes());

                var resultQuery = conn.Query<Cat>("select * from [tcat]".ReplaceBracketDoubleQuotes());
                Assert.Equal(2, resultQuery.Count());
                Assert.Equal(1, resultQuery.Count(c => c.Id == 1));
                Assert.Equal(1, resultQuery.Count(c => c.Id == 2));
                transaction.Rollback();
            }
            using (var conn = GetOpenHanaConnection())
            {
                var dropTableScrit = "drop table [tcat]".ReplaceBracketDoubleQuotes();
                conn.Query(dropTableScrit);
            }
        }

        [FactHana]
        public void TestHanaInsertReturningId()
        {
            using (var conn = GetOpenHanaConnection())
            {
                var transaction = conn.BeginTransaction();
                var createTableScriptAnsi = "create table [tcat] ( [id] INT not null primary key generated by default as IDENTITY, [breed] VARCHAR(20) NOT NULL, [name] VARCHAR (20) not null);".Replace('[', '"').Replace(']', '"');
                conn.Execute(createTableScriptAnsi);


                var columnId = conn.Query("select [COLUMN_ID] from table_columns where table_name = 'tcat' and column_name='id'".ReplaceBracketDoubleQuotes()).First().COLUMN_ID;

                conn.Execute("insert into [tcat]([breed], [name]) values('American Bobtail', 'Xanin') ".ReplaceBracketDoubleQuotes());
                string scriptSelectSequenceName = ("select \"SEQUENCE_NAME\" from sequences where \"SEQUENCE_NAME\" like '%" + columnId + "%'");

                var sequenceName = conn.Query(scriptSelectSequenceName).First().SEQUENCE_NAME;

                string scriptSelectCurrentValueId = "select \""+sequenceName+"\".currval as \"ValueField\"from \"DUMMY\"";

                var resultQuery = conn.Query(scriptSelectCurrentValueId).First().ValueField ;

                Assert.Equal(1, resultQuery);
                transaction.Rollback();
            }
            using (var conn = GetOpenHanaConnection())
            {
                var dropTableScrit = "drop table [tcat]".ReplaceBracketDoubleQuotes();
                conn.Query(dropTableScrit);
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactHanaAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactHanaAttribute()
            {
                try
                {
                    //using (GetOpenHanaConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"Hana is unavailable: {ex.Message}";
                }
            }


        }


    }

    public static class StringHelperReplace
    {
        public static string ReplaceBracketDoubleQuotes(this string value)
        {
            return value.Replace('[', '"').Replace(']', '"');
        }

    }
}
