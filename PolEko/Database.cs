﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PolEko;

public static class Database
{
  public static async Task CreateTables(SqliteConnection connection, IEnumerable<Type> types)
  {
    await connection.OpenAsync();
    var command = connection.CreateCommand();
    command.CommandText =
      @"
        CREATE TABLE IF NOT EXISTS devices(
            ip_address TEXT NOT NULL,
            port INTEGER NOT NULL,
            familiar_name TEXT,
            type TEXT,
            PRIMARY KEY (ip_address, port)
        );
      ";
    await command.ExecuteNonQueryAsync();
    
    foreach (var type in types)
    {
      var query = ProcessMeasurementType(type);
      command.CommandText = query;
      await command.ExecuteNonQueryAsync();
    }
  }

  /// <summary>
  /// Method that takes in a <c>Type</c> derived from <c>Measurement</c> and returns a SQLite query
  /// </summary>
  /// <param name="type">Type that derives from <c>Measurement</c></param>
  /// <returns>SQLite database creation query according to <c>Measurement</c> type</returns>
  /// <exception cref="InvalidCastException">Thrown if <c>Type</c> passed in does not derive from <c>Measurement</c></exception>
  private static string ProcessMeasurementType(Type type)
  {
    if (!type.IsSubclassOf(typeof(Measurement)))
      throw new InvalidCastException("Registered measurement types must derive from Measurement");

    StringBuilder stringBuilder = new(@$"CREATE TABLE IF NOT EXISTS {type.Name}s(");
    
    foreach (var property in type.GetProperties())
    {
      var name = property.Name.ToLower();
      if (name is "timestamp" or "error") continue;
      stringBuilder.Append($"{name} {GetSQLiteType(property.PropertyType)},{Environment.NewLine}");
    }

    stringBuilder.Append(@"timestamp TEXT NOT NULL,
      error INTEGER NOT NULL,
      ip_address TEXT NOT NULL,
      port INTEGER NOT NULL,
      PRIMARY KEY (timestamp, ip_address, port),
      FOREIGN KEY (ip_address, port) REFERENCES devices(ip_address, port));");
    
    return stringBuilder.ToString();
  }

  // ReSharper disable once InconsistentNaming
  /// <summary>
  /// Method that parses .NET types to SQLite types according to https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
  /// </summary>
  /// <param name="type"><c>Type</c> to be parsed</param>
  /// <returns>SQLite data type</returns>
  /// <exception cref="ArgumentException">Thrown if <c>Type</c> passed in is unsupported by SQLite</exception>
  private static string GetSQLiteType(Type type)
  {
    if (type == typeof(string)
        || type == typeof(char)
        || type == typeof(DateOnly)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(Guid)
        || type == typeof(decimal)
        || type == typeof(TimeOnly)
        || type == typeof(TimeSpan))
    {
      return "TEXT";
    }

    if (type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(ushort)
        || type == typeof(short)
        || type == typeof(uint)
        || type == typeof(int)
        || type == typeof(ulong)
        || type == typeof(long)
        || type == typeof(bool))
    {
      return "INTEGER";
    }
    
    if (type == typeof(byte[]))
    {
      return "BLOB";
    }
    
    if (type == typeof(double) || type == typeof(float))
    {
      return "REAL";
    }
    
    throw new ArgumentException($"{type} is not supported in SQLite");
  }
}