// 3. Food Dishes
// This is where our data structures like SecureAccount and CheckingAccount will live.
// Models-> Account.cs are the ingredients/classes with logic

namespace web_app_Csharp.Models
{
   //     var myAccount = new SecureAccount(); // Always keep this code to the top level as C# read scode form top to bottom and if this code moves to bottom, compiler gets confused and spits random error.
   //
   // /* 1. Encapsulation - Hiding secret data with private methods
   //  *
   //  * 1. I want you to write a class called SecureAccount in your IDE (or here). It should have a Private field for Balance.
   //          It should have a Public method GetBalance() that allows people to see the money but not touch it.
   //             It should have a Deposit(decimal amount) method that only adds money if the amount is positive.
   //  */
   public abstract class SecureAccount //By making SecureAccount abstract, you have forced every developer on your team to be specific. They must create a CheckingAccount or SavingsAccount. They cannot create a generic, half-finished object.
   {
      /*
       * Why this matters for your Interview
         If an interviewer asks: "Why didn't you just make Balance public?"
      
         Your Answer: 
         
         "Because of Encapsulation. If I make it public, any junior dev could accidentally set Balance = -1000. By making it private,
                        I force them to use my Deposit() method, which contains the Validation Logic to ensure the data stays safe."
       */
      //changed protected decimal Balance to public decimal Balance, so it can be accessed by ASP.NET web Api.
      public decimal Balance; //Balance was changed from private to protected so children can touch and use it.
      // We use the underscore "_" to signal "This is private so no one alters it apart from the owner of the class".

      public decimal GetBalance() // a public method that allows people to see the money but not touch it
      {
         Console.WriteLine($"Your Balance is: {Balance}");
         return Balance;
      }

      // Marked virtual so children can override it
      public virtual void Deposit (decimal amount) // virtual: Tells the Parent class, "My children are allowed to change this method."
      {
         if (amount < 0)
         {
            Console.WriteLine("Error: invalid amount");
            return; //stop execution immediately
         }
         Balance += amount; // THE LOGIC: Add input (amount) to storage (Balance).
         
         Console.WriteLine($"Your new Balance is : {Balance}");
      }

      public virtual void Withdraw (decimal amount)
      {
         if (amount < 0)
         {
            Console.WriteLine($"Invalid_amount: {amount}");
            return;
         }
         if (amount > Balance)
         {
            Console.WriteLine($"Not enough Balance in your account: {amount}");
            return;
         }
         Balance -= amount;
         Console.WriteLine($"Your new Balance is : {Balance}");
      }
   }

   /* 2. Inheritance & 3. Polymorphism: one method that is reused instead of copying the same method 5 time for similar purpose
    *
    * Level Up: Inheritance (The "Don't Repeat Yourself" Rule)
      Now that you have a SecureAccount, imagine your boss says:
      "Great! Now I need a CheckingAccount, a SavingsAccount, and a RetirementAccount."
      
      The Rookie Way: Copy-paste the code 3 times.
      The Pro Way: Inheritance.
      
      We create one "Base" class (Parent), and the others "Inherit" (Child) the features.
      
      Your Challenge:
      Create a class SavingsAccount that inherits from SecureAccount.
      Hint: use the : symbol. (class SavingsAccount : SecureAccount)
      In the new class, try to add a new property called InterestRate.
    */

   public class CheckingAccount : SecureAccount // this : colon connected the child CheckingAccount to the parent class SecureAccount, so that saved us from writing everything again.
   {
      // It inherits everything from base/parent class SecureAccount
      //adding the properties our LINQ relies on such as Owner and email address
      public string? Owner { get; set; } // Owner is Uppercase/PascalCase because it's a PROPERTY
      public string? EmailAddress { get; set; }
   }

   public class SavingAccount : SecureAccount
   {
      public decimal InterestRate { get; set; } // We USE a PROPERTY {get; set;}, NOT A FIELD // This allows us to add logic later (like validation).
   }

   public class RetirementAccount : SecureAccount
   {
      public decimal InterestRate { get; set; }
      
      public override void Deposit(decimal amount)
      {
         decimal bonus = (amount / 100) * 10;
         decimal total_Deposit = amount + bonus; // Balance was changed to protected so it can be accessed in child class.
         base.Deposit(total_Deposit); // 3. THE MAGIC KEYWORD: "base" , We call the Parent's method to do the actual work. This ensures all the "Safety Checks" (like amount < 0) , in the parent class still run!
         /*
          * Why base.Deposit() wins interviews
            If an interviewer asks: "Why did you call base.Deposit() instead of just adding it to Balance yourself?"
            
            Your Answer:
            
            "Because of DRY (Don't Repeat Yourself). The Parent class already has validation logic (like checking for negative numbers). If I rewrite the logic here, I might introduce bugs. By calling base, I reuse the tested, safe logic of the Parent."
          */
         Console.WriteLine($"Your employer matched: {bonus}");
      }
   }
}