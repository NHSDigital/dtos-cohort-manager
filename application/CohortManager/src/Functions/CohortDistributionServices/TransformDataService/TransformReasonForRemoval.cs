namespace NHS.CohortManager.CohortDistributionService;

using Model;
using Common;
using System.Text.Json;


public class TransformReasonForRemoval : ITransformReasonForRemoval
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ITransformDataLookupFacade _dataLookup;
    private const int ruleId = 1;
    private static readonly string[] noRegisteredGP = ["RDR", "RDI", "RPR"];

    public TransformReasonForRemoval(IExceptionHandler exceptionHandler, ITransformDataLookupFacade dataLookup)
    {
        _exceptionHandler = exceptionHandler;
        _dataLookup = dataLookup;
    }

    /// <summary>
    /// Provides transformations to ensure a dummy GP Practice code is given to RfR participants when required.
    /// This logic involves 4 rules which are triggered in order.
    /// If any of the rules are triggered, the subsequent ones are not triggered and the transformation ends.
    /// </summary>
    /// <param name="participant">The participant</param>
    /// <returns>Either a number of transformations if rules 1 or 2 are triggered, or raises an exception if rules 3 or 4 are triggered</returns>
    public async Task<CohortDistributionParticipant> ReasonForRemovalTransformations(CohortDistributionParticipant participant, CohortDistribution? existingParticipant)
    {
        var participantNotRegisteredToGP = noRegisteredGP.Contains(participant.ReasonForRemoval);
        var validOutcode = !string.IsNullOrEmpty(participant.Postcode) && _dataLookup.ValidateOutcode(participant.Postcode);
        var existingPrimaryCareProvider = existingParticipant == null ? null : existingParticipant.PrimaryCareProvider;

        var rule1 = participantNotRegisteredToGP && validOutcode && !string.IsNullOrEmpty(participant.Postcode);
        var rule2 = participantNotRegisteredToGP && !validOutcode && !string.IsNullOrEmpty(existingPrimaryCareProvider) && !existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule3 = participantNotRegisteredToGP && (string.IsNullOrEmpty(participant.Postcode) || !validOutcode) && !string.IsNullOrEmpty(existingPrimaryCareProvider) && existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule4 = participantNotRegisteredToGP && !validOutcode && string.IsNullOrEmpty(existingPrimaryCareProvider);

        if (rule1 || rule2)
        {
            participant.PrimaryCareProviderEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate;
            participant.ReasonForRemovalEffectiveFromDate = null;
            participant.ReasonForRemoval = null;
            participant.PrimaryCareProvider = GetDummyPrimaryCareProvider(participant.Postcode ?? "", existingPrimaryCareProvider ?? "", validOutcode);

            await _exceptionHandler.CreateTransformExecutedExceptions(participant, "ReasonForRemovalRule", ruleId);

            return participant;
        }
        else if (rule3)
        {
            participant.PrimaryCareProvider = existingParticipant.PrimaryCareProvider;
            participant.PrimaryCareProviderEffectiveFromDate = existingParticipant.PrimaryCareProviderDate?.ToString("yyyy-MM-dd") ?? "";
            participant.ReasonForRemoval = existingParticipant.ReasonForRemoval;
            participant.ReasonForRemovalEffectiveFromDate = existingParticipant.ReasonForRemovalDate?.ToString("yyyy-MM-dd") ?? "";

            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "3.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));

            return participant;
        }
        else if (rule4)
        {
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "4.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));
        }
        else
        {
            return participant;
        }
        return new CohortDistributionParticipant();
    }

    /// <summary>
    /// Creates the dummy primary care provider
    /// If there is a valid postcode, it fetches the BSO code from the BS_SELECT_OUTCODE_MAPPING_LKP table using the outcode
    /// If there is not a valid postcode, it fetches the BSO code from the BS_SELECT_GP_PRACTICE_LKP table using the existing primary care provider
    /// </summary>
    /// <param name="postcode">The participant's postcode</param>
    /// <param name="existingPrimaryCareProvider">The existing primary care provider, which was fetched from the BS_COHORT_DISTRIBUTION table</param>
    /// <param name="validOutcode">Boolean for whether the postcode exists / is valid</param>
    /// <returns>The transformed dummy primary care provider, which is made up of "ZZZ" + BSO code</returns>
    private string GetDummyPrimaryCareProvider(string postcode, string existingPrimaryCareProvider, bool validOutcode)
    {
        var dummyPrimaryCareProvider = "ZZZ";

        if (validOutcode)
        {
            return dummyPrimaryCareProvider + _dataLookup.GetBsoCode(postcode);
        }
        else
        {
            return dummyPrimaryCareProvider + _dataLookup.GetBsoCodeUsingPCP(existingPrimaryCareProvider);
        }
    }
}
