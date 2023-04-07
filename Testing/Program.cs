using Microsoft.Data.Sqlite;

class Program
{
  private const string ConnectionString = "Data Source=Measurements.db";
  
  // Program that spawns 172800 measurements in SmartProMeasurements table with random temperatures from the range of 
  // 2000-3000 and dates starting from now, each a second apart from each other, effectively giving 2 days of measurements
  public static async Task Main()
  {
    await using var connection = new SqliteConnection(ConnectionString);
    await connection.OpenAsync();
    await CreateTablesAsync(connection);
    await BulkInsertAsync(connection);
  }

  private static async Task CreateTablesAsync(SqliteConnection connection)
  {
    var command = connection.CreateCommand();
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS devices(
            IpAddress TEXT NOT NULL,
            Port INTEGER NOT NULL,
            FamiliarName TEXT,
            Type TEXT,
            PRIMARY KEY (IpAddress, Port)
        );
      ";

    await command.ExecuteNonQueryAsync();

    command.CommandText = "INSERT INTO devices VALUES ('127.0.0.1', 56000, NULL, 'SmartProDevice')";
    
    await command.ExecuteNonQueryAsync();

    command.CommandText = @"CREATE TABLE IF NOT EXISTS SmartProMeasurements(
        IsRunning INTEGER NOT NULL,
        Temperature INTEGER NOT NULL,
        Error INTEGER NOT NULL,
        TimeStamp TEXT NOT NULL,
        NetworkError INTEGER NOT NULL,
        IpAddress TEXT NOT NULL,
        Port INTEGER NOT NULL,
        PRIMARY KEY (TimeStamp, IpAddress, Port),
        FOREIGN KEY (IpAddress, Port) REFERENCES devices(IpAddress, Port) ON DELETE CASCADE ON UPDATE CASCADE);";
    
    await command.ExecuteNonQueryAsync();
  }

  private static async Task BulkInsertAsync(SqliteConnection connection)
  {
    await using var transaction = await connection.BeginTransactionAsync();
    var command = connection.CreateCommand();
    command.CommandText =
      @"INSERT INTO SmartProMeasurements VALUES (1, $temperature, 0, $timestamp, 0, '127.0.0.1', 56000)";
    
    var timestampParameter = command.CreateParameter();
    timestampParameter.ParameterName = "$timestamp";
    command.Parameters.Add(timestampParameter);
    var temperatureParameter = command.CreateParameter();
    temperatureParameter.ParameterName = "$temperature";
    command.Parameters.Add(temperatureParameter);

    var dateTime = DateTime.Now;
    var random = new Random();

    for (var i = 0; i < 172800; i++)
    {
      dateTime = dateTime.AddSeconds(1);
      timestampParameter.Value = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
      temperatureParameter.Value = random.Next(2000, 3000);
      await command.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
  }
}