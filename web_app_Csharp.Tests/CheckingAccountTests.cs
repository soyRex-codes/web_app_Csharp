/*
 * Step 4: Run the Test
   here at line:  public void Deposit_ValidAmount_IncreasesBalance().
   Pressing the Green Play Button in the left margin next to the code, allows us to run dedicated tests on the code and 
   once we run it, A new panel will pop up at the bottom of Rider called "Unit Tests or Smilar".
   and If the test passes, we will see a beautiful Green Checkmark/Success.
 */

using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Features.Accounts;
using Xunit;

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
            };

            //2. ACT: EXECUTE the business logic
            account.Deposit(500m); // where m is? In C#, m stands for Money (or decimal)

            //3. Assert: Prove the outcome
            // ASSert.EQUAL(EXPECTED VALUE< ACTUAL VALUE)
            Assert.Equal(1500m, account.Balance);
        }
    }

    public class CheckingAccountTests2
    {
        [Fact]
        public void Deposit_NegativeAmount_ThrowsInvalidOperationException()
        {
            // ARRANGE: Set up the scenario
            var account = new CheckingAccount
            {
                Owner = "Test user2",
            };

            // ACT & ASSERT: Verify that depositing a negative amount throws
            var exception = Assert.Throws<InvalidOperationException>(() => account.Deposit(-500m));

            // Verify the exception message is meaningful
            Assert.Contains("negative", exception.Message, StringComparison.OrdinalIgnoreCase);

            // Verify the balance was NOT modified
            Assert.Equal(1000m, account.Balance);
        }
    }

    public class CheckingAccountTests3
    {
        [Fact]
        public void Withdraw_MoreThanBalance_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var account = new CheckingAccount
            {
                Owner = "Test user3",
            };

            account.Deposit(100m);
            
            // ACT & ASSERT: Verify overdraft throws
            var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(500m));

            Assert.Contains("balance", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(100m, account.Balance);
        }

        [Fact]
        public void Withdraw_ValidAmount_DecreasesBalance()
        {
            // ARRANGE
            var account = new CheckingAccount
            {
                Owner = "Test user4",
            };
            account.Deposit(1000m);
            account.Withdraw(300m);

            // ASSERT
            Assert.Equal(700m, account.Balance);
        }
    }
}