/*4. - database translators Bridge
 * This file is the "Bridge" between our C# code and the SQLite/SQLServer file. In EF Core, this bridge is called a DbContext.
 */

using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Models;

namespace web_app_Csharp.Data
{
    public class
        BankContext : DbContext //Inheriting from DbContext, This class tells .NET that this file talks to database
    {
        // The constructor passes configuration (like the database file path to the base class
        public BankContext(DbContextOptions<BankContext> options) : base(options)
        {
        }
        
        // DbSet represents a Table in our database
        // We are telling EF Core: "Create a table called Accounts to store CheckingAccount, SavingAccount, RetirementAccount objects."
        // ONE Table to Rule all the account types because we had used inheritance in our main program, so we don't need to create new table sfor each account type.
        //ONE Table makes searching Table much faster
        // Ef Core automatically understands the logic and introduces Discriminator that stores account type in one table.
        /* e.g., Database table : Accounts
         -----------------------------------------------
        | id Owner Balance InterestRate Discriminator   |
         ------------------------------------------------
        | 1  Elon  100000  0.05         CheckingAccount |
         ------------------------------------------------
        | 1  Max  5000000  NULL         SavingAccount   |
        -------------------------------------------------
        */
        //PThe arent SecureAccount
        public DbSet<SecureAccount> Accounts { get; set; } // where Accounts is Table, to store CheckingAccount
        
        /*---------------------------------------------------------------------------------------------------------------------------------------------------
         * Even though you listed 4 DbSets here, EF Core is smart enough to look at your code, realize they are all
         * related via inheritance, and it will still only create ONE table in SQLite. It will use the Discriminator
         * column to keep track of whether a row is a Checking, Saving, or Retirement account.
         ----------------------------------------------------------------------------------------------------------------------------------------------------
         */
        
        //Child classes under the parent SecureAccount, EF needs to know all the concrete child classes, this to create db, without knowing this Ef won't create edb because our SecureAccount is set to Abstract.
        // now it knows what to put in the "Discriminator Column such as CheckingAccount or SavingAccount".
        public DbSet<CheckingAccount> CheckingAccounts { get; set; }
        public DbSet<SavingAccount> SavingAccounts { get; set; }
        public DbSet<RetirementAccount> RetirementAccounts { get; set; }
        
    }
}