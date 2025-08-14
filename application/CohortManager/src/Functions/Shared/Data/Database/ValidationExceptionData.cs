namespace Data.Database;

using System;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
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

    public async Task<List<ValidationException>?> GetAllFilteredExceptions(ExceptionStatus? exceptionStatus, SortOrder? sortOrder, ExceptionCategory exceptionCategory)
    {
        var category = (int)exceptionCategory;
        var exceptions = await _validationExceptionDataServiceClient.GetByFilter(x => x.Category != null && x.Category.Value == category);
        var exceptionList = exceptions.Select(s => s.ToValidationException());

        return SortExceptions(sortOrder, exceptionList, exceptionStatus);
    }

    public async Task<ValidationException?> GetExceptionById(int exceptionId)
    {
        var exception = await _validationExceptionDataServiceClient.GetSingle(exceptionId.ToString());

        if (exception == null)
        {
            _logger.LogInformation("Exception not found");
            return null;
        }

        if (!long.TryParse(exception.NhsNumber, out long nhsNumber))
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
            validationExceptionToUpdate.DateResolved = DateTime.UtcNow.Date;
            validationExceptionToUpdate.RecordUpdatedDate = DateTime.UtcNow;
            return await _validationExceptionDataServiceClient.Update(validationExceptionToUpdate);
        }
        return false;
    }

    public async Task<ServiceResponseModel> UpdateExceptionServiceNowId(int exceptionId, string serviceNowId)
    {
        try
        {
            serviceNowId = serviceNowId?.Trim() ?? string.Empty;
            var validationError = ValidateServiceNowId(serviceNowId);
            if (validationError != null)
            {
                return CreateErrorResponse(validationError, HttpStatusCode.BadRequest, validationError);
            }

            var exception = await _validationExceptionDataServiceClient.GetSingle(exceptionId.ToString());
            if (exception == null)
            {
                var errorMessage = $"Exception with ID {exceptionId} not found";
                return CreateErrorResponse(errorMessage, HttpStatusCode.NotFound);
            }

            if (serviceNowId == exception.ServiceNowId)
            {
                var errorMessage = $"ServiceNowId {serviceNowId} is the same as the existing value";
                return CreateErrorResponse(errorMessage, HttpStatusCode.BadRequest);
            }

            exception.ServiceNowId = serviceNowId;
            exception.RecordUpdatedDate = DateTime.UtcNow;

            var updateResult = await _validationExceptionDataServiceClient.Update(exception);
            if (!updateResult)
            {
                var errorMessage = $"Failed to update exception {exceptionId} in data service";
                return CreateErrorResponse(errorMessage, HttpStatusCode.InternalServerError);
            }

            return CreateSuccessResponse();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error updating ServiceNowID for exception {exceptionId}";
            _logger.LogError(ex, errorMessage);
            return CreateErrorResponse(errorMessage, HttpStatusCode.InternalServerError);
        }
    }

    private static ServiceResponseModel CreateSuccessResponse()
    {
        return new ServiceResponseModel
        {
            Success = true,
            StatusCode = HttpStatusCode.OK
        };
    }

    private ServiceResponseModel CreateErrorResponse(string errorMessage, HttpStatusCode statusCode, string? validationErrorMessage = null)
    {
        _logger.LogWarning("Service error occurred: {ErrorMessage}", validationErrorMessage ?? errorMessage);
        return new ServiceResponseModel
        {
            Success = false,
            ErrorMessage = errorMessage,
            StatusCode = statusCode
        };
    }

    private static string? ValidateServiceNowId(string serviceNowId)
    {
        if (string.IsNullOrWhiteSpace(serviceNowId))
            return "ServiceNowID is required.";
        if (serviceNowId.Contains(' '))
            return "ServiceNowID cannot contain spaces.";
        if (serviceNowId.Length < 9)
            return "ServiceNowID must be at least 9 characters long.";
        if (!serviceNowId.All(char.IsLetterOrDigit))
            return "ServiceNowID must contain only alphanumeric characters.";
        return null;
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
            SupersededByNhsNumber = participantDemographic?.SupersededByNhsNumber,
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

    private static List<ValidationException> SortExceptions(SortOrder? sortOrder, IEnumerable<ValidationException> list, ExceptionStatus? status)
    {
        var filteredList = status switch
        {
            ExceptionStatus.Raised => list.Where(x => !string.IsNullOrEmpty(x.ServiceNowId)),
            ExceptionStatus.NotRaised => list.Where(x => string.IsNullOrEmpty(x.ServiceNowId)),
            _ => list
        };

        Func<ValidationException, DateTime?> dateProperty = status == ExceptionStatus.Raised
            ? x => x.ServiceNowCreatedDate
            : x => x.DateCreated;

        return sortOrder == SortOrder.Ascending
            ? [.. filteredList.OrderBy(dateProperty)]
            : [.. filteredList.OrderByDescending(dateProperty)];
    }
}
