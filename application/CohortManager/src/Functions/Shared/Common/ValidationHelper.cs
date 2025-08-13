namespace Common;

using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Hl7.Fhir.Validation;
using Model;

public static class ValidationHelper
{

    private static readonly string[] DateFormats = ["yyyyMMdd", "yyyyMM", "yyyy", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "d/MM/yyyy hh:mm:ss tt", "dd/MM/yyyy HH:mm:ss tt"];
    private static readonly string NilReturnFileNhsNumber = "0000000000";

    /// <summary>
    /// Validates that a date is not in the future and
    /// in a valid format.
    /// </summary>
    /// <returns>bool, whether or not the date is valid</returns>
    /// <remarks>
    /// If you create a new date, make sure to provide a valid format in
    /// the ToString parameter, otherwise it will default to the default
    /// based on the culture info.
    /// </remarks>
    public static bool ValidatePastDate(string dateString)
    {
        var date = ParseDate(dateString);

        if (date.HasValue)
        {
            return date < DateTime.UtcNow.Date.AddDays(1);
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

    /// <summary>
    /// Validates the postcode according to the offical rules for valid
    /// UK postcodes, also accepts dummy postcodes as valid.
    /// </summary>
    /// <param name="postcode">Postcode string (not null)</param>
    /// <returns>bool, whether or not the postcode is valid</returns>
    /// <remarks>
    /// further information for postcode validation can be found in
    /// ADR-008 on confluence.
    /// </remarks>
    public static bool ValidatePostcode(string postcode)
    {
        string validPostcodePattern = "^([A-Za-z][A-Ha-hJ-Yj-y]?[0-9][A-Za-z0-9]? ?[0-9][A-Za-z]{2}|[Gg][Ii][Rr] ?0[Aa]{2})$";
        string dummyPostcodePattern = "^ZZ99 ?[0-9][A-Z]{2}$";

        Match validPostcodeMatch = Regex.Match(postcode, validPostcodePattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        Match dummyPostcodeMatch = Regex.Match(postcode, dummyPostcodePattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        if (validPostcodeMatch.Success || dummyPostcodeMatch.Success)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the outcode (1st part of postcode) from the postcode and if the postcode is a dummy code
    /// </summary>
    /// <param name="postcode">a non-null string representing the postcode</param>
    /// <remarks>
    /// Works for valid UK postcodes and dummy postcodes.
    /// Works with or without a space separator between outcode and incode.
    /// </remarks>
    public static (string? outcode, bool isDummyPostCode) ParseOutcode(string postcode)
    {
        string pattern = @"^([A-Za-z][A-Za-z]?[0-9][A-Za-z0-9]?) ?[0-9][A-Za-z]{2}$";
        string specialDummyOutCode = "ZZZSECUR";

        Match match = Regex.Match(postcode, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

        if (postcode == specialDummyOutCode)
        {
            return (postcode, true);
        }

        if (!match.Success)
        {
            return (null, false);
        }



        string outcode = match.Groups[1].Value;

        return (outcode, false);
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
