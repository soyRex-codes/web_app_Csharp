// 2. Waiter
// Controllers are the waiters taking orders.
// =============================================================================================================================================================
// This is where we will put our web endpoint code.
// to import functionalities from other files, we need to import
// the name of namespace from other files rather than the file name, that's hw c# works.

/* Think of your web API like a restaurant 🍽️:
 * ASP.NET Core has a specific, industry-standard way of organizing these files            
 * so that anyone looking at your project knows exactly where things are.
 * Program.cs is the building manager. It configures the server,
 * turns on the lights, and opens the front door. We usually want
 * to keep it as clean and minimal as possible, so we try to avoid
 * putting our business logic or data classes in here.
 * 
=============================================================================================================================================================
   initial api we will get is http://localhost:5000/api/Bank/debtors
=============================================================================================================================================================
*/


using Microsoft.AspNetCore.Mvc;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Accounts; // By adding using web_app_Csharp.Models;, our controller now has full access to the CheckingAccount class.


namespace web_app_Csharp.Controllers
{
   [ApiController]
   
     //In [Route("api/[controller]")], [controller] part is a special ASP.NET variable that automatically,
     //swaps in the name of our class, minus the word Controller" (so BankController becomes just Bank).
    
   [Route("api/[controller]")] // This makes the base URL: api/Bank/debtors
   public class BankController : ControllerBase
   { 
      
      //===========================================================================================================================================================
     // 1. Dependency Injection Constructor
     // 1.1  CreatING a private field to hold the database connection, MAKING IT 'readOnly' so we don't accidentally overwrite it.
     // How would i modify these private fields and the constructor to ask the application for an ILogger<BankController> alongside BankContext?

     // 1. Create the private readonly fields to store the tools
     // private readonly ToolOne _toolOne; //this is sample pattern, we can use it for any class
     private readonly BankContext _context;
     private readonly ILogger<BankController> _logger;
     
     // 2. Ask for them in the constructor parameters
     //1.2 The Constructor (Dependency Injection)
     //When ASP.NET creates this controller, it automatically passes in the BankContext.
     public BankController(BankContext context, ILogger<BankController> logger) //contructor and class is same thing, it is just a method that is called when an object is created.
     { //ILogger<BankController> logger as parameter because it is a service that is used to log messages not just context because context is used to store data in database.
        _context = context; //this is how we store the database connection in the private field.
        _logger = logger; //this is how we store the logger in the private field.
     }
     //===========================================================================================================================================================
         
      //2. The Endpoint
      [HttpGet("debtors")] //URL will be: http://localhost:5233/api/Bank/debtors
      public ActionResult<List<string>> GetDebtors()
      {
         //3. Our exact LINQ code!
         // // LINQ example 1:
         var emailAddress0FPeopleInDebt = _context.CheckingAccounts.Where(a => a.Balance < 0).Select(a => a.EmailAddress).ToList();
               // // LINQ Concept - help write less verbose code taking less space and better readability
               //
               // // In C# we use Lambda expression => to find the account with Balance >= 1000, which is better than writing 6 lines using a loop and if else.
               // //Translation: From accounts , keep Where a(the account) has a Balance greater than 1000, then make it a List.
               // var richPeople = _accounts.Where(a => a.Balance > 1000).ToList();
               //
               // /*
               // LINQ example 2:
               // why use .Select instead of just returning the whole Account object?
               // -> Because returning the whole object account, take a significant amount of memory
               // but using .Select(a => a.EmailAddress), Entity Framework translates this into a SQL query that only selcts that one specfic column (SELCT EmailAddress From Accounts).
               // this makes the API significantly faster to retrieve data.
               // */
               //
               // // scenario : all email address of customers who are in debt and had Balance < 0.
               // //1. just getting the account with Balance <0.
               // // We use 'var' to keep it clean. The compiler knows it's a List<string>.
               // var EmailAddress_0f_People_in_debt = _accounts.Where(a => a.Balance < 0).ToList();
               // //2. getting only Email string not the whole account object where Balance <0.
               // var EmailAddress_0f_People_in_debt = _accounts.Where(a => a.Balance < 0).Select(a => a.EmailAddress).ToList(); 
         
         
         //4. Enterprise Safety Check
         // what if No ONE is in debt? We shouldn't return broken screen.
         if (emailAddress0FPeopleInDebt.Count == 0)
         {
            return NotFound("Good News, non is broke and no one is in debt as of today.");
         }
         
         //5. The 200 OK Response
         // This automatically turns your C# List into a JSON array for the web.
         return Ok(emailAddress0FPeopleInDebt);
      }

// =============================================================================================================================================================
      // Adding more CRUD operations such as Create Account, deposit, withdraw.
         // C# syntax for creating, new account uses new AccountType {property1 = value1, property2 = value2, ...}
// =============================================================================================================================================================
         //2.1. The Endpoint for creating new account
// =============================================================================================================================================================
         //URL will be: http://localhost:5233/api/Bank/CheckingAccount
         [HttpPost("CheckingAccount")]
         public ActionResult<CheckingAccount> CreateAccount([FromBody] CheckingAccount newAccount)
         {
            if (newAccount == null)
            {
               return BadRequest("Account data is required.");
            }
            if (string.IsNullOrWhiteSpace(newAccount.Owner))
            {
               return BadRequest("Owner name is required.");
            }
            if (string.IsNullOrWhiteSpace(newAccount.EmailAddress))
            {
               return BadRequest("Email address is required.");
            }

            _context.CheckingAccounts.Add(newAccount);
            _context.SaveChanges();
            
            _logger.LogInformation("Account created successfully for {Owner}", newAccount.Owner);
            return CreatedAtAction(nameof(GetAllOwners), new { id = newAccount.Id }, newAccount);
         }
// =============================================================================================================================================================         
         //2.2. The Endpoint for deposit
// =============================================================================================================================================================         
         [HttpPost("deposit")]
          public ActionResult Deposit(string owner, decimal amount)
          {
             if (string.IsNullOrWhiteSpace(owner))
             {
                return BadRequest("Owner name is required.");
             }
             if (amount <= 0)
             {
                return BadRequest("Deposit amount must be greater than zero.");
             }

             var account = _context.CheckingAccounts.FirstOrDefault(a => a.Owner == owner);
             if (account == null)
             {
                _logger.LogWarning("Deposit failed: user {Owner} not found", owner);
                return NotFound($"Account not found for owner: {owner}");
             }

             try
             {
                account.Deposit(amount);
                _context.SaveChanges();
                _logger.LogInformation("Deposit of {Amount} successful for {Owner}", amount, owner);
                return Ok(new { message = "Deposit successful", owner, amount, newBalance = account.Balance });
             }
             catch (InvalidOperationException ex)
             {
                _logger.LogWarning("Deposit rejected for {Owner}: {Message}", owner, ex.Message);
                return BadRequest(ex.Message);
             }
          }
// =============================================================================================================================================================
         //2.3. The Endpoint for Withdraw
// =============================================================================================================================================================         
         [HttpPost("withdraw_funds")]
          public ActionResult Withdraw(string owner, decimal amount)
          {
             if (string.IsNullOrWhiteSpace(owner))
             {
                return BadRequest("Owner name is required.");
             }
             if (amount <= 0)
             {
                return BadRequest("Withdrawal amount must be greater than zero.");
             }

             var account = _context.CheckingAccounts.FirstOrDefault(a => a.Owner == owner);
             if (account == null)
             {
                _logger.LogWarning("Withdrawal failed: user {Owner} not found", owner);
                return NotFound($"Account not found for owner: {owner}");
             }

             try
             {
                account.Withdraw(amount);
                _context.SaveChanges();
                _logger.LogInformation("Withdrawal of {Amount} successful for {Owner}", amount, owner);
                return Ok(new { message = "Withdrawal successful", owner, amount, newBalance = account.Balance });
             }
             catch (InvalidOperationException ex)
             {
                _logger.LogWarning("Withdrawal rejected for {Owner}: {Message}", owner, ex.Message);
                return BadRequest(ex.Message);
             }
          }
// =============================================================================================================================================================         
         //2.4. The Endpoint for reading data   
// =============================================================================================================================================================         
 
         [HttpGet("CheckingAccount")] //HttpGet is used to  read data
         //URL for get all account Owners is http://localhost:5233/api/Bank/accounts
         public ActionResult<List<string>> GetAllOwners()
         {
            var owners = _context.CheckingAccounts.Select(a => a.Owner).ToList(); // we return OK because Without Ok(), the React app wouldn't reliably know if the request succeeded or failed. The status code is the universal signal that every client in the world understands.
            return Ok(owners);
         }
   }
}

/*
1. we did Routing, we know how to create custom URL
2. Data transformation: we didn't send password and Balance over internet, we selectively extracted only email using /Select()
3. HTTP Semantics: we used NOTFound() and Ok() instead of just crashing if the list was empty.
*/ 
// =============================================================================================================================================================