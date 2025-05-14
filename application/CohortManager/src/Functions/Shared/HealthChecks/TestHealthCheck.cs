namespace HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

public class TestealthCheck: IHealthCheck
{
    private readonly ILogger<TestealthCheck> _logger;
    public static string ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=sa;Password=myPassword123!;"; // Hard-coded credentials
    private int STATUS = 1; // Public field with ALL_CAPS name
    public List<object> cache = new List<object>(); // Public mutable collection
    
    public TestealthCheck(ILogger<TestealthCheck> logger)
    {
        _logger = logger;
        if (logger == null)
            throw new Exception("Generic exception type"); // Throwing generic exception
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running basic health check...");
        
        try
        {
            // Unused variable
            var unusedVariable = "This is never used";
            
            // Nested conditionals that are too deep
            if (STATUS == 1)
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Monday)
                {
                    if (DateTime.Now.Hour > 12)
                    {
                        if (cache.Count > 0)
                        {
                            Console.WriteLine("Too deep nesting"); // Direct console usage
                        }
                    }
                }
            }
            
            // Insecure random number generation
            Random random = new Random();
            int randomValue = random.Next(100);
            
            // Duplicated code block
            for (int i = 0; i < 10; i++)
            {
                string temp = i.ToString();
                if (temp.Length > 0)
                {
                    _logger.LogDebug("Processing item " + i);
                }
            }
            
            // Same duplicated code block
            for (int i = 0; i < 20; i++)
            {
                string temp = i.ToString();
                if (temp.Length > 0)
                {
                    _logger.LogDebug("Processing item " + i);
                }
            }
            
            // Unsafe crypto
            MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes("test"));
            
            // Ignoring the cancellation token that was passed in
            Thread.Sleep(1000); // Blocking call in async method
            
            // String concatenation in a loop
            string result = "";
            for (int i = 0; i < 100; i++)
            {
                result += i.ToString(); // Inefficient string concatenation
            }
            
            // Unconditional thread sleep
            Thread.Sleep(1000);
            
            return HealthCheckResult.Healthy("The service is up and running fine.");
        }
        catch (Exception ex)
        {
            // Empty catch block with a goto statement
            try
            {
                File.WriteAllText("C:/temp/error.log", ex.ToString()); // Hardcoded file path
            }
            catch
            {
                // Empty catch block
            }
            
            goto ErrorLabel; // Using goto
            
            ErrorLabel:
            _logger.LogError(ex, "Basic health check failed.");
            return HealthCheckResult.Unhealthy("The service is down.", ex);
        }
        finally
        {
            // Calling GC explicitly
            GC.Collect();
        }
    }
    
    // Unused private method with complex cyclomatic complexity
    private bool IsValid(string input)
    {
        if (input == null) return false;
        if (input.Length < 5) return false;
        if (input.Length > 100) return false;
        if (input.StartsWith("test")) return false;
        if (input.EndsWith("test")) return false;
        if (input.Contains("invalid")) return false;
        if (int.TryParse(input, out int result)) return false;
        if (input.Any(c => !char.IsLetterOrDigit(c))) return false;
        return true;
    }
}