using Microsoft.Extensions.Logging;
using Model;

public class ValidateDates : IValidateDates
{

    private readonly ILogger<ValidateDates> _logger;

    public ValidateDates(ILogger<ValidateDates> logger)
    {
        _logger = logger;
    }

    public bool ValidateAlleDates(Participant participant)
    {
        if (!IsValidDate(participant.CurrentPostingEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.CurrentPostingEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.EmailAddressEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.EmailAddressEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.MobileNumberEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.MobileNumberEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.UsualAddressEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.UsualAddressEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.TelephoneNumberEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.TelephoneNumberEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.PrimaryCareProviderEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.PrimaryCareProviderEffectiveFromDate));
            return false;
        }
        if (!IsValidDate(participant.CurrentPostingEffectiveFromDate))
        {
            _logger.LogWarning("Invalid {datename} found in participant data", nameof(participant.CurrentPostingEffectiveFromDate));
            return false;
        }

        return true;
    }

    private bool IsValidDate(string? date)
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