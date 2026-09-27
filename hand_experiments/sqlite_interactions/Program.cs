
using System.Data;
using Microsoft.Data.Sqlite;

// I want the user to be able to enter in their items that they want to put into the database
// They should also be able to view items.

CreateTable();

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
    using var conn = new SqliteConnection("Data Source=testing123.db");
    conn.Open();

    // Now we have the strange reader things. I think it is like a pointer to my current index?
    //var reader = conn.R
    // Needed to look at example

    using var command = conn.CreateCommand();
    var reader = command.ExecuteReader();

    while (reader.Read()) // I would have never gotten that just from my brain
    {
        // Now I want to actually get the item it is pointing at, and then move forward by one?
        var name = reader.GetString(1);
        Console.WriteLine($"The name is: {name}");   
    }
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
        var name = reader.GetString(1);
        Console.WriteLine($"Hello, {name}");
    }
}


// The idea of this one should be to create the tables inside of this database so that I can then access them
// So I will need to look at the way in which the sqlite library expects to see a command and how to perform that command
// I will also need to think of the actual schema which I need to use.
// (I will attempt to do this by creating the logic without looking at the example above, but I will look to verify)
void CreateTable()
{
    using var conn = new SqliteConnection("Data Source=testing123.db");

    // I now want to have the library actually open the connection
    conn.Open();

    // Now the app is using the library and the library has a connection to that specific file on my harddrive and will interact with it in accordance to the 
    // specifications of the sqlite interactions. (but I don't need to worry about that)

    // Next should be to create the command that I want to do. 
    // string command = """
    //     CREATE TABLE
    // """;
    // but I think this was actually a string on the conn

    // I guess I now also need to make this idempotent because the library command does not do that
    // DataTable dt = conn.GetSchema();
    // if ()

    using var command = conn.CreateCommand(); // Why do I need to do it this way?
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS user(
        name TEXT NOT NULL,
        id INTEGER PRIMARY KEY
        );
    """;

    // Next should I just execute the command?
    command.ExecuteNonQuery();


}