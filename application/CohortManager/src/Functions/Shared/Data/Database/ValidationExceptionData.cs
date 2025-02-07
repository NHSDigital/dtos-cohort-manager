namespace Data.Database;

using System;
using System.Data;
using System.Threading.Tasks;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using NHS.CohortManager.CohortDistribution;

public class ValidationExceptionData : IValidationExceptionData
{
    private readonly ILogger<ValidationExceptionData> _logger;
    private readonly IDataServiceClient<ExceptionManagement> _validationExceptionDataServiceClient;
    private readonly IDataServiceClient<ParticipantDemographic> _demographicDataServiceClient;
    private readonly IDataServiceClient<GPPractice> _gpPracticeDataServiceClient;

    public ValidationExceptionData(
        ILogger<ValidationExceptionData> logger,
        IDataServiceClient<ExceptionManagement> validationExceptionDataServiceClient,
        IDataServiceClient<ParticipantDemographic> demographicDataServiceClient,
        IDataServiceClient<GPPractice> gpPracticeDataServiceClient
    )
    {
        _logger = logger;
        _validationExceptionDataServiceClient = validationExceptionDataServiceClient;
        _demographicDataServiceClient = demographicDataServiceClient;
        _gpPracticeDataServiceClient = gpPracticeDataServiceClient;
    }

    public async Task<List<ValidationException>> GetAllExceptions(bool todayOnly, ExceptionSort? orderByProperty)
    {
        var exceptions = todayOnly
            ? await _validationExceptionDataServiceClient.GetByFilter(x => x.DateCreated.Value.Date == DateTime.Today)
            : await _validationExceptionDataServiceClient.GetAll();

        var exceptionList = exceptions.Select(s => s.ToValidationException());
        var propertyName = GetPropertyName(orderByProperty);

        if (propertyName == nameof(ValidationException.DateCreated)) //WP - not a fan of this, but it works, dictionary and tuple were the alternative
        {
            return exceptionList.OrderByDescending(o => o.DateCreated).ToList();
        }

        return exceptionList.OrderBy(o => o.GetType().GetProperty(propertyName).GetValue(o)).ToList();
    }

    public async Task<ValidationException> GetExceptionById(int exceptionId)
    {
        var exception = await _validationExceptionDataServiceClient.GetSingle(exceptionId.ToString());

        long nhsNumber;

        if (!long.TryParse(exception.NhsNumber, out nhsNumber))
        {
            throw new FormatException("Unable to parse NHS Number");
        }

        var participantDemographic = await _demographicDataServiceClient.GetSingleByFilter(x => x.NhsNumber == nhsNumber);
        var gpPracticeDetails = await _gpPracticeDataServiceClient.GetSingleByFilter(x => x.GPPracticeCode == participantDemographic.PrimaryCareProvider);

        return GetExceptionDetails(exception.ToValidationException(), participantDemographic, gpPracticeDetails);
    }

    public async Task<bool> Create(ValidationException exception)
    {
        var exceptionToUpdate = new ExceptionManagement().FromValidationException(exception);
        return await _validationExceptionDataServiceClient.Add(exceptionToUpdate);
    }

    public async Task<bool> RemoveOldException(string nhsNumber, string screeningName)
    {
        var exceptions = await GetExceptionRecords(nhsNumber, screeningName);
        if (exceptions == null)
        {
            return false;
        }

        // we only need to get the last unresolved exception for the nhs number and screening service
        var validationExceptionToUpdate = exceptions.Where(x => DateToString(x.DateResolved) == "9999-12-31")
        .OrderByDescending(x => x.DateCreated).FirstOrDefault();

        if (validationExceptionToUpdate != null)
        {
            validationExceptionToUpdate.DateResolved = DateTime.Today;

            return await _validationExceptionDataServiceClient.Update(validationExceptionToUpdate);
        }
        return false;
    }

    private ValidationException? GetExceptionDetails(ValidationException? exception, ParticipantDemographic? participantDemographic, GPPractice? gPPractice)
    {
        if (exception == null)
        {
            _logger.LogWarning("Exception not found");
            return null;
        }
        exception.ExceptionDetails = new ExceptionDetails
        {
            GivenName = participantDemographic?.GivenName,
            FamilyName = participantDemographic?.FamilyName,
            DateOfBirth = participantDemographic?.DateOfBirth,
            Gender = Enum.TryParse(participantDemographic?.Gender.ToString(), out Gender gender) ? gender : Gender.NotKnown,
            ParticipantAddressLine1 = participantDemographic?.AddressLine1,
            ParticipantAddressLine2 = participantDemographic?.AddressLine2,
            ParticipantAddressLine3 = participantDemographic?.AddressLine3,
            ParticipantAddressLine4 = participantDemographic?.AddressLine4,
            ParticipantAddressLine5 = participantDemographic?.AddressLine5,
            ParticipantPostCode = participantDemographic?.PostCode,
            TelephoneNumberHome = participantDemographic?.TelephoneNumberHome,
            EmailAddressHome = participantDemographic?.EmailAddressHome,
            PrimaryCareProvider = participantDemographic?.PrimaryCareProvider,
            GpPracticeCode = gPPractice?.GPPracticeCode,
            GpAddressLine1 = gPPractice?.AddressLine1,
            GpAddressLine2 = gPPractice?.AddressLine2,
            GpAddressLine3 = gPPractice?.AddressLine3,
            GpAddressLine4 = gPPractice?.AddressLine4,
            GpAddressLine5 = gPPractice?.AddressLine5,
            GpPostCode = gPPractice?.Postcode
        };

        if (participantDemographic == null || gPPractice == null)
        {
            _logger.LogWarning("Missing data: ParticipantDemographic: {ParticipantDemographic}, GPPractice: {GPPractice}", participantDemographic != null, gPPractice != null);
        }
        
        return exception;
    }

    private async Task<List<ExceptionManagement>?> GetExceptionRecords(string nhsNumber, string screeningName)
    {

        var exceptions = await _validationExceptionDataServiceClient.GetByFilter(x => x.NhsNumber == nhsNumber && x.ScreeningName == screeningName);
        return exceptions != null ? exceptions.ToList() : null;

    }

    private static string DateToString(DateTime? datetime)
    {
        if (datetime != null)
        {
            DateTime NonNullableDateTime = datetime.Value;
            return NonNullableDateTime.ToString("yyyy-MM-dd");
        }
        // we throw here to stop processing as the date should never be null
        throw new ArgumentNullException(nameof(datetime), "Failed to parse null datetime");
    }

    private static string GetPropertyName(ExceptionSort? orderByProperty)
    {
        return orderByProperty switch
        {
            ExceptionSort.ExceptionId => nameof(ValidationException.ExceptionId),
            ExceptionSort.NhsNumber => nameof(ValidationException.NhsNumber),
            ExceptionSort.DateCreated => nameof(ValidationException.DateCreated),
            ExceptionSort.RuleDescription => nameof(ValidationException.RuleDescription),
            _ => nameof(ValidationException.DateCreated)
        };
    }

}
