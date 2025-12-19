namespace Data.Database;

using System;
using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

public class ValidationExceptionData : IValidationExceptionData
{
    private readonly ILogger<ValidationExceptionData> _logger;
    private readonly IDataServiceClient<ExceptionManagement> _validationExceptionDataServiceClient;

    public ValidationExceptionData(
        ILogger<ValidationExceptionData> logger,
        IDataServiceClient<ExceptionManagement> validationExceptionDataServiceClient
    )
    {
        _logger = logger;
        _validationExceptionDataServiceClient = validationExceptionDataServiceClient;
    }

    public async Task<List<ValidationException>> GetFilteredExceptions(ExceptionStatus? exceptionStatus, SortOrder? sortOrder, ExceptionCategory exceptionCategory)
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

        return GetValidationExceptionWithDetails(exception);
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

            var serviceNowIdChanged = serviceNowId != exception.ServiceNowId;
            var isNullServiceNowId = string.IsNullOrWhiteSpace(serviceNowId);

            exception.ServiceNowId = isNullServiceNowId ? null : serviceNowId;
            exception.ServiceNowCreatedDate = isNullServiceNowId ? null : DateTime.UtcNow;
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

    public async Task<List<ValidationException>> GetReportExceptions(DateTime? reportDate, ExceptionCategory exceptionCategory)
    {
        if (exceptionCategory is not (ExceptionCategory.Confusion or ExceptionCategory.Superseded or ExceptionCategory.NBO))
        {
            return [];
        }

        var filteredExceptions = await GetFilteredReportExceptions(reportDate, exceptionCategory);

        if (filteredExceptions == null || !filteredExceptions.Any())
            return [];

        return MapToValidationExceptions(filteredExceptions);
    }

    public async Task<List<ExceptionManagement>> GetByFilter(Expression<Func<ExceptionManagement, bool>> filter)
    {
        var result = await _validationExceptionDataServiceClient.GetByFilter(filter) ?? Enumerable.Empty<ExceptionManagement>();
        return result.ToList();
    }

    public async Task<ValidationExceptionsByNhsNumberResponse> GetExceptionsWithReportsByNhsNumber(string nhsNumber)
    {
        var validationExceptions = await GetValidationExceptionsByNhsNumber(nhsNumber);

        if (validationExceptions.Count == 0)
        {
            return new ValidationExceptionsByNhsNumberResponse
            {
                Exceptions = [],
                Reports = [],
                NhsNumber = nhsNumber
            };
        }

        var reports = GenerateExceptionReports(validationExceptions);
        return new ValidationExceptionsByNhsNumberResponse
        {
            Exceptions = validationExceptions,
            Reports = reports,
            NhsNumber = nhsNumber
        };
    }

    private List<ValidationException> MapToValidationExceptions(IEnumerable<ExceptionManagement> exceptions)
    {
        return exceptions.Select(GetValidationExceptionWithDetails).Where(x => x != null).ToList()!;
    }

    private ServiceResponseModel CreateSuccessResponse(string message) => CreateResponse(true, HttpStatusCode.OK, message);
    private ServiceResponseModel CreateErrorResponse(string message, HttpStatusCode statusCode) => CreateResponse(false, statusCode, message);

    private ValidationException? GetValidationExceptionWithDetails(ExceptionManagement exception)
    {
        var validationException = exception.ToValidationException();
        var participantDemographic = ExtractParticipantDemographicFromErrorRecord(exception.ErrorRecord);
        return GetExceptionDetails(validationException, participantDemographic);
    }

    private async Task<IEnumerable<ExceptionManagement>?> GetFilteredReportExceptions(DateTime? reportDate, ExceptionCategory exceptionCategory)
    {
        var filteredExceptions = (await _validationExceptionDataServiceClient.GetByFilter(x =>
            x.Category.HasValue && (x.Category.Value == (int)ExceptionCategory.Confusion || x.Category.Value == (int)ExceptionCategory.Superseded)))?.AsEnumerable();

        if (exceptionCategory == ExceptionCategory.Confusion || exceptionCategory == ExceptionCategory.Superseded)
        {
            filteredExceptions = filteredExceptions?.Where(x => x.Category.HasValue && x.Category.Value == (int)exceptionCategory);
        }

        if (reportDate.HasValue)
        {
            var startDate = reportDate.Value.Date;
            var endDate = startDate.AddDays(1);
            filteredExceptions = filteredExceptions?.Where(x => x.DateCreated >= startDate && x.DateCreated < endDate);
        }

        return filteredExceptions;
    }

    private ParticipantDemographic? ExtractParticipantDemographicFromErrorRecord(string? errorRecord)
    {
        if (string.IsNullOrWhiteSpace(errorRecord))
        {
            return null;
        }

        try
        {
            var errorRecordData = JsonSerializer.Deserialize<ErrorRecordDto>(errorRecord);
            if (errorRecordData == null)
            {
                return null;
            }

            return new ParticipantDemographic
            {
                NhsNumber = long.TryParse(errorRecordData.NhsNumber, out long nhsNumber) ? nhsNumber : 0,
                GivenName = errorRecordData.FirstName,
                FamilyName = errorRecordData.FamilyName,
                DateOfBirth = MappingUtilities.FormatDateTime(MappingUtilities.ParseDates(errorRecordData.DateOfBirth ?? string.Empty)),
                SupersededByNhsNumber = long.TryParse(errorRecordData.SupersededByNhsNumber, out long superseded) ? superseded : null,
                Gender = errorRecordData.Gender,
                AddressLine1 = errorRecordData.AddressLine1,
                AddressLine2 = errorRecordData.AddressLine2,
                AddressLine3 = errorRecordData.AddressLine3,
                AddressLine4 = errorRecordData.AddressLine4,
                AddressLine5 = errorRecordData.AddressLine5,
                PostCode = errorRecordData.Postcode,
                TelephoneNumberHome = errorRecordData.TelephoneNumber,
                EmailAddressHome = errorRecordData.EmailAddress,
                PrimaryCareProvider = errorRecordData.PrimaryCareProvider,
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ErrorRecord JSON: {ErrorRecord}. Error: {Error}", errorRecord, ex.Message);
            return null;
        }
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


    private static string? ValidateServiceNowId(string serviceNowId)
    {
        if (string.IsNullOrWhiteSpace(serviceNowId))
        {
            return null;
        }
        if (serviceNowId.Contains(' '))
        {
            return "ServiceNowId cannot contain spaces.";
        }
        if (serviceNowId.Length < 9)
        {
            return "ServiceNowId must be at least 9 characters long.";
        }
        if (!serviceNowId.All(char.IsLetterOrDigit))
        {
            return "ServiceNowId must contain only alphanumeric characters.";
        }
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
            _logger.LogWarning("Missing participant demographic data for exception");
        }

        return exception;
    }

    private async Task<List<ExceptionManagement>?> GetExceptionRecords(string nhsNumber, string screeningName)
    {
        var exceptions = await _validationExceptionDataServiceClient.GetByFilter(x => x.NhsNumber == nhsNumber && x.ScreeningName == screeningName);
        return exceptions?.ToList();
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



    private async Task<List<ValidationException>> GetValidationExceptionsByNhsNumber(string nhsNumber)
    {
        var exceptions = await _validationExceptionDataServiceClient.GetByFilter(x => x.NhsNumber == nhsNumber && x.Category.HasValue && x.Category.Value == (int)ExceptionCategory.NBO);
        if (exceptions == null || !exceptions.Any())
        {
            return [];
        }

        return [.. exceptions
            .Select(GetValidationExceptionWithDetails)
            .Where(x => x != null)
            .Cast<ValidationException>()
            .OrderByDescending(x => x.DateCreated)];
    }

    private static List<ValidationExceptionReport> GenerateExceptionReports(List<ValidationException> validationExceptions)
    {
        return [.. validationExceptions
            .Where(x => x.Category.HasValue && (x.Category.Value == 12 || x.Category.Value == 13))
            .GroupBy(x => new
            {
                Date = x.DateCreated?.Date ?? DateTime.Now.Date,
                Category = x.Category
            })
            .Select(g => new ValidationExceptionReport
            {
                ReportDate = g.Key.Date,
                Category = g.Key.Category,
                ExceptionCount = g.Count()
            })
            .OrderByDescending(r => r.ReportDate)];
    }
}
