
using Microsoft.Data.Sqlite;

// I want the user to be able to enter in their items that they want to put into the database
// They should also be able to view items.

while (true)
{

    Console.WriteLine("Select action:");
    Console.WriteLine("1. Add to Database");
    Console.WriteLine("2. Read Database");
    string? userInput = Console.ReadLine();
    int.TryParse(userInput, out int userResult);
    if (userResult == 1)
    {
        AddValueToDataBase();
    }
    if (userResult == 2)
    {
        ReadDataBase();
    }
    if (userResult == 3)
    {
        DocumentedExample();
    }
}
void AddValueToDataBase()
{
    
}

void ReadDataBase()
{
    
}

void DocumentedExample()
{
    using var conn = new SqliteConnection("Data Source=testing123.db");
    conn.Open();

    using var command = conn.CreateCommand();
    command.CommandText = """
        Select name
        From user
        WHERE id = $id
    """;

    int id = 3;
    command.Parameters.AddWithValue("$id", id);
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        var name = reader.GetString(0);
        Console.WriteLine($"Hello, {name}");
    }
}

