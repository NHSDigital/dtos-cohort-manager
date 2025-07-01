namespace NHS.CohortManager.CohortDistributionService;

using Model;
using Common;
using Data.Database;
using System.Text.Json;
using Apache.Arrow.Types;
using Model.Enums;

public class UnTransformRules : IUnTransformRules
{
    private readonly IExceptionHandler _exceptionHandler;
    private const int ruleId = 35;
    public UnTransformRules(IExceptionHandler exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;
    }


    /// <summary>
    /// Checks if too many demographic fields have changed between the current participant and existing participant records.
    /// If significant changes are detected, creates an exception log for untransformed rules like Rule 35.
    /// </summary>
    /// <param name="participant">The current participant data containing potentially updated demographic information.</param>
    /// <param name="existingParticipant">The existing participant record to compare against. Can be null if no existing record exists.</param>
    /// <returns>A new empty <see cref="CohortDistributionParticipant"/> instance.</returns>
    /// <remarks>
    /// This method checks for changes in three key demographic fields:
    /// 1. Family Name
    /// 2. Gender
    /// 3. Date of Birth
    /// An exception log is created if any two of these three fields have changed.
    /// </remarks>
    public async Task<CohortDistributionParticipant> TooManyDemographicsFieldsChanges(CohortDistributionParticipant participant, CohortDistribution? existingParticipant)
    {
        if (existingParticipant?.ParticipantId == null)
        {
            return participant;
        }

        string? newDateOfBirth = participant.DateOfBirth?.Replace("-", "");
        string? existingDateOfBirth = existingParticipant.DateOfBirth?.ToString("yyyyMMdd");

        string existingGenderName = GetGenderName(existingParticipant.Gender);

        // Main validation logic
        bool condition1 = participant.FamilyName != existingParticipant.FamilyName
                       && participant.Gender?.ToString() != existingGenderName;

        bool condition2 = participant.FamilyName != existingParticipant.FamilyName
                       && newDateOfBirth != existingDateOfBirth;

        bool condition3 = participant.Gender?.ToString() != existingGenderName
                       && newDateOfBirth != existingDateOfBirth;

        if (condition1 || condition2 || condition3)
        {
            await _exceptionHandler.CreateExceptionLogsForUnTransformRules(participant, "TooManyDemographicsFieldsChangedConfusionNonFatal", ruleId, (int)ExceptionCategory.Confusion);
        }

        return participant;
    }

    private string GetGenderName(short genderValue)
    {
        if (Enum.IsDefined(typeof(Gender), genderValue))
        {
            return ((Gender)genderValue).ToString();
        }
        return "Invalid";
    }
}
