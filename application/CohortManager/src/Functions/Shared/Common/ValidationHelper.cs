namespace Common;

using System.Globalization;

public static class ValidationHelper
{
    // Validates that the date is not in the future and that it is in one of the expected formats
    public static bool ValidatePastDate(string dateString)
    {
        DateTime date;

        if (DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateString, "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
        }
        else if (DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            if (date <= DateTime.Today)
            {
                return true;
            }
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

    public static bool ValidateCurrentPostingAndPrimaryCareProvider(string currentPosting, string primaryCareProvider)
    {
        if (currentPosting == null && primaryCareProvider != null)
        {
            return false;
        }
        return true;
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
