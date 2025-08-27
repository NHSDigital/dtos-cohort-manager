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
                return CreateErrorResponse(validationError, HttpStatusCode.BadRequest);
            }

            var exception = await _validationExceptionDataServiceClient.GetSingle(exceptionId.ToString());
            if (exception == null)
            {
                return CreateErrorResponse($"Exception with ID {exceptionId} not found", HttpStatusCode.NotFound);
            }

            bool serviceNowIdChanged = serviceNowId != exception.ServiceNowId;

            exception.ServiceNowId = serviceNowId;
            exception.RecordUpdatedDate = DateTime.UtcNow;

            var updateResult = await _validationExceptionDataServiceClient.Update(exception);
            if (!updateResult)
            {
                return CreateErrorResponse($"Failed to update exception {exceptionId} in data service", HttpStatusCode.InternalServerError);
            }

            string successMessage = serviceNowIdChanged ? "ServiceNowId updated successfully" : "ServiceNowId unchanged, but record updated date has been updated";

            return CreateSuccessResponse(successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ServiceNowId for exception {ExceptionId}", exceptionId);
            return CreateErrorResponse($"Error updating ServiceNowId for exception {exceptionId}", HttpStatusCode.InternalServerError);
        }
    }

    public async Task<List<ValidationException>?> GetReportExceptions(DateTime? reportDate, ExceptionCategory exceptionCategory)
    {
        var isSpecificReportCategory = exceptionCategory == ExceptionCategory.Confusion || exceptionCategory == ExceptionCategory.Superseded;
        var filterByDate = reportDate.HasValue;
        var startDate = reportDate?.Date ?? DateTime.MinValue;
        var endDate = startDate.AddDays(1);

        var allExceptions = await _validationExceptionDataServiceClient.GetByFilter(x =>
            x.Category.HasValue && (x.Category.Value == (int)ExceptionCategory.Confusion || x.Category.Value == (int)ExceptionCategory.Superseded));

        var categoryFilteredExceptions = isSpecificReportCategory
            ? allExceptions?.Where(x => x.Category.Value == (int)exceptionCategory)
            : allExceptions;

        var exceptions = filterByDate
            ? categoryFilteredExceptions?.Where(x => x.DateCreated >= startDate && x.DateCreated < endDate)
            : categoryFilteredExceptions;

        return exceptions?.Select(s => s.ToValidationException()).ToList();
    }

    private ServiceResponseModel CreateResponse(bool success, HttpStatusCode statusCode, string message)
    {
        if (!success)
        {
            _logger.LogWarning("Service error occurred: {ErrorMessage}", message);
        }

        return new ServiceResponseModel
        {
            Success = success,
            StatusCode = statusCode,
            Message = message,
        };
    }

    private ServiceResponseModel CreateSuccessResponse(string message) => CreateResponse(true, HttpStatusCode.OK, message);
    private ServiceResponseModel CreateErrorResponse(string message, HttpStatusCode statusCode) => CreateResponse(false, statusCode, message);

    private static string? ValidateServiceNowId(string serviceNowId)
    {
        if (serviceNowId.Contains(' '))
            return "ServiceNowId cannot contain spaces.";
        if (serviceNowId.Length < 9)
            return "ServiceNowId must be at least 9 characters long.";
        if (!serviceNowId.All(char.IsLetterOrDigit))
            return "ServiceNowId must contain only alphanumeric characters.";
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
