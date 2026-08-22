
// I want the user to be able to enter in their items that they want to put into the database
// They should also be able to view items.
while(true)
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
}

void AddValueToDataBase()
{
    
}

void ReadDataBase()
{
    
}