// 1. Restaurant manager
// Main file 
/*
 
 Every single C# application needs a starting point (the entry point) to run.
  For a Web API, Program.cs is the engine room ⚙️ where we turn on the web
    server and tell it how to handle incoming HTTP requests.
 
Level,Topic,The Real-World Skill
   Level 1,C# OOP & LINQ, Writing logic that doesn't crash.
   Level 2,.NET & ASP.NET,"Building the API (The ""Waiter"")."
   Level 3,EF Core & SQL,Saving data so it doesn't vanish on restart.
   Level 4,xUnit & Moq,Proving your code works (Critical for Banks).
   Level 5,"Azure, Docker, CI/CD",Deploying the app to the cloud.
*/


/* Think of your web API like a restaurant 🍽️:
 * ASP.NET Core has a specific, industry-standard way of organizing these files
 * so that anyone looking at your project knows exactly where things are.
 * Program.cs is the building manager. It configures the server,
 * turns on the lights, and opens the front door. We usually want
 * to keep it as clean and minimal as possible, so we try to avoid
 * putting our business logic or data classes in here.
 */


// See https://aka.ms/new-console-template for more information
 
//ASP.NET Core setup code 
// 1. Create the builder
var builder = WebApplication.CreateBuilder(args);

// 2. Tell the builder we are using controllers to handle web requests
builder.Services.AddControllers();

// 3. Build the app
var app = builder.Build();

// 4. Tell the app to map incoming URLs to our Controllers
app.MapControllers();

//5. Start the server!
app.Run();










