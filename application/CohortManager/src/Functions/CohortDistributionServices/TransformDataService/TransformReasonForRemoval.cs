namespace NHS.CohortManager.CohortDistribution;

using Model;
using Common;
using Data.Database;
using System.Text.Json;

public class TransformReasonForRemoval : ITransformReasonForRemoval
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBsTransformationLookups _transformationLookups;
    private readonly ITransformDataLookupFacade _dataLookup;
    public TransformReasonForRemoval(IExceptionHandler exceptionHandler, IBsTransformationLookups transformationLookups, ITransformDataLookupFacade dataLookup)
    {
        _exceptionHandler = exceptionHandler;
        _transformationLookups = transformationLookups;
        _dataLookup = dataLookup;
    }

    /// <summary>
    /// Provides transformations to ensure a dummy GP Practice code is given to RfR participants when required.
    /// This logic involves 4 rules which are triggered in order.
    /// If any of the rules are triggered, the subsequent ones are not triggered and the transformation ends.
    /// </summary>
    /// <param name="participant">The participant</param>
    /// <returns>Either a number of transformations if rules 1 or 2 are triggered, or raises an exception if rules 3 or 4 are triggered</returns>
    public async Task<CohortDistributionParticipant> ReasonForRemovalTransformations(CohortDistributionParticipant participant)
    {
        var participantNotRegisteredToGP = new string[] { "RDR", "RDI", "RPR" }.Contains(participant.ReasonForRemoval);
        var validOutcode = !string.IsNullOrEmpty(participant.Postcode) && _dataLookup.ValidateOutcode(participant.Postcode);
        var existingPrimaryCareProvider = _transformationLookups.GetPrimaryCareProvider(participant.NhsNumber);

        var rule1 = participantNotRegisteredToGP && validOutcode && !string.IsNullOrEmpty(participant.Postcode);
        var rule2 = participantNotRegisteredToGP && !validOutcode && !string.IsNullOrEmpty(existingPrimaryCareProvider) && !existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule3 = participantNotRegisteredToGP && !validOutcode && !string.IsNullOrEmpty(existingPrimaryCareProvider) && existingPrimaryCareProvider.StartsWith("ZZZ");
        var rule4 = participantNotRegisteredToGP && !validOutcode && string.IsNullOrEmpty(existingPrimaryCareProvider);

        if (rule1 || rule2)
        {
            participant.PrimaryCareProviderEffectiveFromDate = participant.ReasonForRemovalEffectiveFromDate;
            participant.ReasonForRemovalEffectiveFromDate = null;
            participant.ReasonForRemoval = null;
            participant.PrimaryCareProvider = GetDummyPrimaryCareProvider(participant.Postcode ?? "", existingPrimaryCareProvider, validOutcode);
            return participant;
        }
        else if (rule3)
        {
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "3.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));
            throw new TransformationException("Chained rule 3.ParticipantNotRegisteredToGPWithReasonForRemoval raised an exception");
        }
        else if (rule4)
        {
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, "", "4.ParticipantNotRegisteredToGPWithReasonForRemoval", participant.ScreeningName ?? "", JsonSerializer.Serialize(participant));
            throw new TransformationException("Chained rule 4.ParticipantNotRegisteredToGPWithReasonForRemoval raised an exception");
        }
        else return participant;
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

        if (!string.IsNullOrEmpty(existingPrimaryCareProvider))
        {
            return dummyPrimaryCareProvider + _transformationLookups.GetBsoCodeUsingPCP(existingPrimaryCareProvider);
        }

        return dummyPrimaryCareProvider;
    }
}