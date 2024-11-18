namespace Common;

using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Reflection.Metadata;

public static class ValidationHelper
{

    private static readonly string[] DateFormats = ["yyyyMMdd", "yyyyMM", "yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "d/MM/yyyy hh:mm:ss tt", "dd/MM/yyyy HH:mm:ss tt", "yyyy"];
    private static readonly string NilReturnFileNhsNumber = "0000000000";
    // Validates that the date is not in the future and that it is in one of the expected formats
    public static bool ValidatePastDate(string dateString)
    {
        var date = ParseDate(dateString);
        if(date.HasValue)
        {
            return date < DateTime.Today;
        }
        return false;
    }
    public static bool IsValidDate(string dateString)
    {
        var date = ParseDate(dateString);
        return date.HasValue;
    }
    public static bool ValidateNHSNumber(string nhsNumber)
    {
        // Check the NHS number is a number
        if (!long.TryParse(nhsNumber, out _))
        {
            return false;
        }

        if (nhsNumber.Length != 10)
        {
            return false;
        }

        // Check if NHS number is from a nil return file
        if (nhsNumber == NilReturnFileNhsNumber)
        {
            return false;
        }

        //check digit (checksum) -- https://www.datadictionary.nhs.uk/attributes/nhs_number.html
        int sum = 0;
        int factor = 10;
        for (int i = 0; i < 9; i++)
        {
            int digit;
            if (!ParseInt32(nhsNumber[i], out digit))
            {
                return false;
            }
            sum += digit * factor;
            factor--;
        }

        string checkDigit = (11 - (sum % 11)).ToString();
        if (checkDigit == "10") return false;
        if (checkDigit == "11") checkDigit = "0";
        if (nhsNumber[9].ToString() == checkDigit)
        {
            return true;
        }
        return false;

    }

    private static bool ParseInt32(char value, out int integerValue)
    {
        integerValue = (int)char.GetNumericValue(value);
        if (integerValue < 0 || integerValue > 9)
        {
            integerValue = -1;
            return false;
        }
        return true;
    }

    private static DateTime? ParseDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date;
        }
        return null;
    }
}
