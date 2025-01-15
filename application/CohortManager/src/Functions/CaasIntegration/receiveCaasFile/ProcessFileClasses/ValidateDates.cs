namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Extensions.Logging;
using Model;

public class ValidateDates : IValidateDates
{

    private readonly ILogger<ValidateDates> _logger;

    public ValidateDates(ILogger<ValidateDates> logger)
    {
        _logger = logger;
    }

    public bool ValidateAllDates(Participant participant)
    {
        if (!IsValidDate(participant.CurrentPostingEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {CurrentPostingEffectiveFromDate} found in participant data", nameof(participant.CurrentPostingEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.EmailAddressEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {EmailAddressEffectiveFromDate} found in participant data", nameof(participant.EmailAddressEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.MobileNumberEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {MobileNumberEffectiveFromDate} found in participant data", nameof(participant.MobileNumberEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.UsualAddressEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {UsualAddressEffectiveFromDate} found in participant data", nameof(participant.UsualAddressEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.TelephoneNumberEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {TelephoneNumberEffectiveFromDate} found in participant data", nameof(participant.TelephoneNumberEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.PrimaryCareProviderEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {PrimaryCareProviderEffectiveFromDate} found in participant data", nameof(participant.PrimaryCareProviderEffectiveFromDate));
            return false;
        }

        return true;
    }

    private static bool IsValidDate(string? date)
    {
        if (date == null)
        {
            return true;
        }
        if (date.Length > 8)
        {
            return false;
        }
        return true;

    }
}