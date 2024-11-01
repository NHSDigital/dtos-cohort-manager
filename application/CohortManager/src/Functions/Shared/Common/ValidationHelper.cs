namespace Common;

using System.Globalization;
using Model;

public static class ValidationHelper
{
    // Validates that the date is not in the future and that it is in one of the expected formats
    public static bool ValidatePastDate(string dateString)
    {
        string[] formats = ["yyyyMMdd"];

        if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date < DateTime.Today;
        }
        return false;
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
}
