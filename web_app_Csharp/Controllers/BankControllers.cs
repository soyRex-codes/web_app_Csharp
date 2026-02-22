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
using System.Collections.Generic;
using System.Linq;
using web_app_Csharp.Models; // By adding using web_app_Csharp.Models;, our controller now has full access to the CheckingAccount class.


namespace web_app_Csharp.Controllers
{
   [ApiController]
   
     //In [Route("api/[controller]")], [controller] part is a special ASP.NET variable that automatically,
     //swaps in the name of our class, minus the word Controller" (so BankController becomes just Bank).
    
   [Route("api/[controller]")] // This makes the base URL: api/Bank/debtors
   public class BankController : ControllerBase
   {
      //1. Our fake "Database" living in memory
      // // Lis of all bank account we have in the bank at this moment
      private static List<CheckingAccount> _accounts = new List<CheckingAccount>
      {
         new CheckingAccount { Owner = "Elon", Balance = 100000, EmailAddress = "elon123@gmail.com" },
         new CheckingAccount { Owner = "Max", Balance = 50000, EmailAddress = "Max123@gmail123.com" },
         new CheckingAccount { Owner = "bejos", Balance = 50, EmailAddress = "bejos@gmail123.com" },
         new CheckingAccount { Owner = "Cliff", Balance = 20000, EmailAddress = "cliff123@gmail.com" },
         new CheckingAccount { Owner = "Bliff", Balance = -20000, EmailAddress = "bliff123@gmail.com" }
      };
      
      //2. The Endpoint
      //URL will be: GET /api/Bank/debtors
      [HttpGet("debtors")]
      public ActionResult<List<string>> GetDebtors()
      {
         //3. Our exact LINQ code!
         // // LINQ example 1:
         var EmailAddress0FPeopleInDebt = _accounts.Where(a => a.Balance < 0).Select(a => a.EmailAddress).ToList();
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
         if (EmailAddress0FPeopleInDebt.Count == 0)
         {
            return NotFound("Good News, non is broke and no one is in debt as of today.");
         }
         
         //5. The 200 OK Response
         // This automatically turns your C# List into a JSON array for the web.
         return Ok(EmailAddress0FPeopleInDebt);
      }

// =============================================================================================================================================================
      // Adding more CRUD operations such as Create Account, deposit, withdraw.
         // C# syntax for creating, new account uses new AccountType {property1 = value1, property2 = value2, ...}
// =============================================================================================================================================================
         //2.1. The Endpoint for creating new account
// =============================================================================================================================================================
         //URL will be: GET /Bank/CheckingAccount
         [HttpPost("CheckingAccount")] //[HttpPost] is used to Create new account, Deposit balance and Withdraw balance
        // =============================================================================================================================================================
         // URL for Create Account is POST /api/Bank/CheckingAccount
        // =============================================================================================================================================================
         // ASP.NET fills these data fro account from the JSON body
         // ActionResult<...> must be a data type (a class/model) — it describes what kind of data you're returning.
         // Here CheckingAccount is the data we are returning.
         public ActionResult<CheckingAccount> CreateAccount([FromBody] CheckingAccount newAccount) //ASP.NET fill the dat such as Owner name, Balance, and Email addsress automatically from the client request.         
         {/*
            hardcoding "Sam" means every call creates the same person. In a real API,
            the client sends the data. ASP.NET can do this automatically using a parameter.
               // newAccount is ALREADY filled with Owner, Balance, EmailAddress
               // because ASP.NET read the JSON that the client sent!
            */
            _accounts.Add(newAccount);
            return Ok("Account Created Successfully");
         }
// =============================================================================================================================================================         
         //2.2. The Endpoint for deposit
// =============================================================================================================================================================         
         //URL will be: GET /Bank/deposit
         [HttpPost("deposit")] //deposit is Route name While Deposite is method name that performs all the operations
         //[HttpPost] is used to Create make edits to the account, Deposit balance and Withdraw balance
         // URL for Depoite is POST /api/Bank/deposit?owner=Elon&amount=500
         public ActionResult Deposit(string owner, decimal amount) // owner is lowecase/camelCase because it's a PARAMETER  //ASP.NET fill this automatically from the client request.
         {
            //step1: Step 1: Client sends ?owner=Elon&amount=500 in URL or via react frontend, first we find the owner of the account in our database
            // using LINQ method
            var account = _accounts.FirstOrDefault(a=> a.Owner == owner); //FirstOrDefault is used to find the first account that matches the given condition. If no account is found, it returns null.
            // step2: we check if the owner exists, great we move to this account.Deposit(amount); if not, we return and stop execution.
            if(account == null)
            {
               return NotFound("Account Not Found");
            }
            // why I didn't use else: because else is not necessary here as in C#, if the if statement was true than, the method will retrun something early and the moment there is a retrun,
            // the method stops execution, so we have passed if statement and reach to this statemnt, that means if was false and we have found an account to make an wothdrawl from.
            account.Deposit(amount);
            return Ok("Deposit Successful");
         }
// =============================================================================================================================================================
         //2.3. The Endpoint for Withdraw
// =============================================================================================================================================================         
         //URL will be: GET /Bank/Withdraw
         [HttpPost("withdraw_funds")] // withdraw_Funds with Http is a Route name while Withdraw is Method name that perfoms all the operation on the amount receives.
         //[HttpPost] is used to Create make edits to the account, Deposit balance and Withdraw balance
         // URL for Withdraw is POST /api/Bank/withdraw?owner=Elon&amount=200
         public ActionResult Withdraw(string owner, decimal amount) // owner is lowecase/camelCase because it's a PARAMETER.
         {
            //step1: Step 1: Client sends ?owner=Elon&amount=500 in URL or via react frontend, first we find the owner of the account in our database
            // using LINQ method
            var account = _accounts.FirstOrDefault(a=> a.Owner == owner); //FirstOrDefault is used to find the first account that matches the given condition. If no account is found, it returns null.
            // step2: we check if the owner exists
            if(account == null)
            {
               return NotFound("Account Not Found"); //If we ever reach return , that moment method stops execution and nothing else runs after this.
            }
            // why I didn't use else: because else is not necessary here as in C#, if the if statement was true than, the method will retrun something early and the moment there is a retrun,
            // the method stops execution, so we have passed if statement and reach to this statemnt, that means if was false and we have found an account to make an wothdrawl from.
            account.Withdraw(amount);
            return Ok("withdraw Successful");
         }
// =============================================================================================================================================================         
         //2.4. The Endpoint for reading data   
// =============================================================================================================================================================         
 
         [HttpGet("CheckingAccount")] //HttpGet is used to  read data
         //URL for get all account Owners is GET /api/Bank/accounts
         public ActionResult<List<string>> GetAllOwners()
         {
            return Ok(_accounts.Select(a => a.Owner).ToList()); // we return OK because Without Ok(), the React app wouldn't reliably know if the request succeeded or failed. The status code is the universal signal that every client in the world understands.
         }
   }
}

/*
1. we did Routing, we know how to create custom URL
2. Data transformation: we didn't send password and Balance over internet, we selectively extracted only email using /Select()
3. HTTP Semantics: we used NOTFound() and Ok() instead of just crashing if the list was empty.
*/ 
// =============================================================================================================================================================