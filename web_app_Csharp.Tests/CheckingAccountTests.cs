/*
 * Step 4: Run the Test
   here at line:  public void Deposit_ValidAmount_IncreasesBalance().
   Pressing the Green Play Button in the left margin next to the code, allows us to run dedicated tests on the code and 
   once we run it, A new panel will pop up at the bottom of Rider called "Unit Tests or Smilar".
   and If the test passes, we will see a beautiful Green Checkmark/Success.
 */

using Microsoft.EntityFrameworkCore;
using Xunit;
using web_app_Csharp.Models;

namespace web_app_Csharp.Tests
{
    public class CheckingAccountTests1
    {
        [Fact] // The [Fact] attribute tells the test runner: "Hey, run this method as a test!"
        public void Deposite_ValidAmount_IncreaseBalance()
        {
            //1. ARRANGE: Lets Set up the scenario
            var account = new CheckingAccount
            {
                Owner = "Test user1",
                Balance = 1000m
            };
            decimal depositAmount = 500m; // where m is? In C#, m stands for Money (or decimal)

            //2. ACT: EXECUTE the business logic
            account.Deposit(depositAmount);

            //3. Assert: Prove the outcome
            // ASSert.EQUAL(EXPECTED VALUE< ACTUAL VALUE)
            Assert.Equal(1500m, account.Balance);
        }
    }

    public class CheckingAccountTests2
    {
        [Fact] // The [Fact] attribute tells the test runner: "Hey, run this method as a test!"
        public void Deposite_negativeAmount_DoesNotChangeBalance()
        {
            //1. ARRANGE: Lets Set up the scenario
            var account = new CheckingAccount
            {
                Owner = "Test user2",
                Balance = 1000m //$1000
            };
            decimal depositAmount = -500m; // where m is? In C#, m stands for Money (or decimal)
            /*
             * If we type 500.50 in C#, the compiler assumes it is a double (a standard floating-point number). But double is dangerous for financial applications because of how computers handle binary math.
               
               If we ask a computer to add 0.1 + 0.2 using double, it often results in 0.30000000000000004. If we do that millions of times in a bank, we lose actual dollars to rounding errors.
               
               By adding m (e.g., 500.50m), we force C# to use the decimal type, which uses base-10 math. It is perfectly precise.
               
               In a technical interview for a financial institution, using m instead of a standard decimal proves understanding of enterprise data integrity.
             */

            //2. ACT: EXECUTE the business logic
            account.Deposit(depositAmount);

            //3. Assert: Prove the outcome
            // ASSert.EQUAL(EXPECTED VALUE< ACTUAL VALUE)
            Assert.Equal(1000, account.Balance);
        }
    }
}