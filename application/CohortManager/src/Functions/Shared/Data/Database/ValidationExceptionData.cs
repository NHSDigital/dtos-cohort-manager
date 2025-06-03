namespace Data.Database;

using System;
using System.Data;
using System.Threading.Tasks;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Model;
using Model.Enums;

public class ValidationExceptionData : IValidationExceptionData
{
    private readonly ILogger<ValidationExceptionData> _logger;
    private readonly IDataServiceClient<ExceptionManagement> _validationExceptionDataServiceClient;
    private readonly IDataServiceClient<ParticipantDemographic> _demographicDataServiceClient;
    public ValidationExceptionData(
        ILogger<ValidationExceptionData> logger,
        IDataServiceClient<ExceptionManagement> validationExceptionDataServiceClient,
        IDataServiceClient<ParticipantDemographic> demographicDataServiceClient
    )
    {
        _logger = logger;
        _validationExceptionDataServiceClient = validationExceptionDataServiceClient;
        _demographicDataServiceClient = demographicDataServiceClient;
    }

    public async Task<List<ValidationException?>> GetAllExceptions(bool todayOnly, ExceptionSort? orderByProperty, ExceptionCategory exceptionCategory)
    {
        var category = (int)exceptionCategory;

        var exceptions = todayOnly
            ? await _validationExceptionDataServiceClient.GetByFilter(x => x.DateCreated.Value.Date == DateTime.Today && x.Category.Value == category)
            : await _validationExceptionDataServiceClient.GetByFilter(x => x.Category.Value == category);

        var exceptionList = exceptions.Select(s => s.ToValidationException());

        return SortExceptions(orderByProperty, exceptionList);
    }

    public async Task<ValidationException?> GetExceptionById(int exceptionId)
    {
        var exception = await _validationExceptionDataServiceClient.GetSingle(exceptionId.ToString());

        if (exception == null)
        {
            _logger.LogInformation("Exception not found");
            return null;
        }

        long nhsNumber;

        if (!long.TryParse(exception.NhsNumber, out nhsNumber))
        {
            throw new FormatException("Unable to parse NHS Number");
        }

        var participantDemographic = await _demographicDataServiceClient.GetSingleByFilter(x => x.NhsNumber == nhsNumber);

        return GetExceptionDetails(exception.ToValidationException(), participantDemographic);
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

    private ValidationException? GetExceptionDetails(ValidationException? exception, ParticipantDemographic? participantDemographic)
    {
        if (exception == null)
        {
            _logger.LogInformation("Exception not found");
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
            GpPracticeCode = participantDemographic?.PrimaryCareProvider
        };

        if (participantDemographic == null)
        {
            _logger.LogWarning("Missing data: ParticipantDemographic: {ParticipantDemographic}", participantDemographic != null);
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

    private static List<ValidationException> SortExceptions(ExceptionSort? sortBy, IEnumerable<ValidationException> list)
    {
        return sortBy switch
        {
            // Sort by date created, oldest first
            ExceptionSort.DateCreatedOldest => list.OrderBy(x => x.DateCreated).ToList(),

            // Sort by date created, newest first
            ExceptionSort.DateCreatedNewest => list.OrderByDescending(x => x.DateCreated).ToList(),

            // Sort by exception status raised, then by date created
            ExceptionSort.ExceptionStatusRaised => list
                .OrderByDescending(x => !x.ServiceNowId.IsNullOrEmpty())
                .ThenByDescending(x => x.DateCreated).ToList(),

            // Sort by exception status not raised, then by date created
            ExceptionSort.ExceptionStatusNotRaised => list
                .OrderByDescending(x => x.ServiceNowId.IsNullOrEmpty())
                .ThenByDescending(x => x.DateCreated).ToList(),

            // By default sort by date created, newest first
            _ => list.OrderByDescending(x => x.DateCreated).ToList()
        };
    }
}
