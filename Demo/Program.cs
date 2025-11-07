using Newtonsoft.Json;
using System.Text.Json;

namespace DemoApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("NuVet Demo Application");
        Console.WriteLine("This app demonstrates vulnerable package detection.");
        
        // Using Newtonsoft.Json (potentially vulnerable version)
        var data = new { Name = "Demo", Version = "1.0.0" };
        var json = JsonConvert.SerializeObject(data);
        Console.WriteLine($"Newtonsoft.Json: {json}");
        
        // Using System.Text.Json (potentially vulnerable version)  
        var jsonData = System.Text.Json.JsonSerializer.Serialize(data);
        Console.WriteLine($"System.Text.Json: {jsonData}");
        
        Console.WriteLine("\nRun 'nuvet scan .' to check for vulnerabilities!");
    }
}