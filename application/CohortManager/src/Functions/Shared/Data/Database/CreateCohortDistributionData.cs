namespace Data.Database;

using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Common.Interfaces;
using DataServices.Client;
using Model;
using Model.DTO;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

public class CreateCohortDistributionData : ICreateCohortDistributionData
{
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataServiceClient;
    private readonly IDataServiceClient<BsSelectRequestAudit> _bsSelectRequestAuditDataServiceClient;
    private readonly IExtractCohortDistributionRecordsStrategy? _extractionStrategy;

    public CreateCohortDistributionData(
        IDataServiceClient<CohortDistribution> cohortDistributionDataServiceClient,
        IDataServiceClient<BsSelectRequestAudit> bsSelectRequestAuditDataServiceClient,
        IExtractCohortDistributionRecordsStrategy? extractionStrategy = null)
    {
        _cohortDistributionDataServiceClient = cohortDistributionDataServiceClient;
        _bsSelectRequestAuditDataServiceClient = bsSelectRequestAuditDataServiceClient;
        _extractionStrategy = extractionStrategy;

    }


    public async Task<List<CohortDistributionParticipantDto>> GetUnextractedCohortDistributionParticipants(int rowCount, bool retrieveSupersededRecordsLast)
    {
        List<CohortDistribution> participantsToBeExtracted;
        // Use new extraction logic if environment variable is set and strategy is injected
        if (retrieveSupersededRecordsLast && _extractionStrategy != null)
        {
            participantsToBeExtracted = await _extractionStrategy.GetUnextractedParticipants(rowCount, retrieveSupersededRecordsLast);
        }
        else
        {
            var participantsList = await _cohortDistributionDataServiceClient.GetByFilter(x => x.IsExtracted.Equals(0) && x.RequestId == Guid.Empty);
            participantsToBeExtracted = participantsList.OrderBy(x => x.RecordUpdateDateTime ?? x.RecordInsertDateTime).Take(rowCount).ToList();
        }

        //TODO do this filtering on the data services
        var CohortDistributionParticipantList = participantsToBeExtracted.Select(x => new CohortDistributionParticipant(x)).ToList();


        var requestId = Guid.NewGuid();
        if (await MarkCohortDistributionParticipantsAsExtracted(participantsToBeExtracted, requestId))
        {
            await LogRequestAudit(requestId, (int)HttpStatusCode.OK);

            return CohortDistributionParticipantDto(CohortDistributionParticipantList, requestId);
        }

        var statusCode = CohortDistributionParticipantList.Count == 0 ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.InternalServerError;
        await LogRequestAudit(requestId, statusCode);

        return new List<CohortDistributionParticipantDto>();
    }

    public async Task<List<CohortDistributionParticipantDto>> GetCohortDistributionParticipantsByRequestId(Guid requestId)
    {
        if (requestId == Guid.Empty)
        {
            CohortDistributionParticipantDto(new List<CohortDistributionParticipant>());
        }
        // TODO we should probably tidy this up and make it better.
        var participantsList = await _cohortDistributionDataServiceClient.GetByFilter(x => x.RequestId == requestId);
        return CohortDistributionParticipantDto(participantsList.Select(x => new CohortDistributionParticipant(x)).ToList());
    }

    public async Task<List<CohortRequestAudit>> GetCohortRequestAudit(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var builtPredicate = BuildCohortRequestAuditQuery(requestId, statusCode, dateFrom);

        if (builtPredicate != null)
        {
            var filteredResult = await _bsSelectRequestAuditDataServiceClient.GetByFilter(builtPredicate);
            return BuildCohortRequestAudits(filteredResult);
        }

        var result = await _bsSelectRequestAuditDataServiceClient.GetAll();
        return BuildCohortRequestAudits(result);
    }

    private static List<CohortDistributionParticipantDto> CohortDistributionParticipantDto(List<CohortDistributionParticipant> listOfAllParticipants, Guid? newRequestId = null)
    {
        return listOfAllParticipants.Select(s => new CohortDistributionParticipantDto
        {
            RequestId = CreateRequestId(newRequestId, s.RequestId),
            NhsNumber = s.NhsNumber ?? string.Empty,
            SupersededByNhsNumber = s.SupersededByNhsNumber ?? null,
            PrimaryCareProvider = s.PrimaryCareProvider ?? null,
            PrimaryCareProviderEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.PrimaryCareProviderEffectiveFromDate),
            NamePrefix = s.NamePrefix ?? string.Empty,
            FirstName = s.FirstName ?? string.Empty,
            OtherGivenNames = s.OtherGivenNames ?? string.Empty,
            FamilyName = s.FamilyName ?? string.Empty,
            PreviousFamilyName = s.PreviousFamilyName ?? string.Empty,
            DateOfBirth = DatabaseHelper.FormatDateAPI(s.DateOfBirth),
            Gender = s.Gender ?? Gender.NotKnown,
            AddressLine1 = s.AddressLine1 ?? string.Empty,
            AddressLine2 = s.AddressLine2 ?? string.Empty,
            AddressLine3 = s.AddressLine3 ?? string.Empty,
            AddressLine4 = s.AddressLine4 ?? string.Empty,
            AddressLine5 = s.AddressLine5 ?? string.Empty,
            Postcode = s.Postcode ?? string.Empty,
            UsualAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.UsualAddressEffectiveFromDate),
            DateOfDeath = DatabaseHelper.FormatDateAPI(s.DateOfDeath),
            TelephoneNumber = s.TelephoneNumber ?? string.Empty,
            TelephoneNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.TelephoneNumberEffectiveFromDate),
            MobileNumber = s.MobileNumber ?? string.Empty,
            MobileNumberEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.MobileNumberEffectiveFromDate) ?? string.Empty,
            EmailAddress = s.EmailAddress ?? string.Empty,
            EmailAddressEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.EmailAddressEffectiveFromDate) ?? string.Empty,
            PreferredLanguage = s.PreferredLanguage ?? string.Empty,
            IsInterpreterRequired = int.TryParse(s.IsInterpreterRequired, out var isInterpreterRequired) ? isInterpreterRequired : 0,
            ReasonForRemoval = s.ReasonForRemoval ?? string.Empty,
            ReasonForRemovalEffectiveFromDate = DatabaseHelper.FormatDateAPI(s.ReasonForRemovalEffectiveFromDate),
            ParticipantId = s.ParticipantId ?? string.Empty,
            IsExtracted = s.Extracted ?? string.Empty,
        }).ToList();
    }

    private static string CreateRequestId(Guid? newRequestId, string currentRequestId)
    {
        if (newRequestId != null)
        {
            return newRequestId.Value.ToString();
        }
        return currentRequestId;
    }

    private async Task LogRequestAudit(Guid requestId, int statusCode)
    {
        await _bsSelectRequestAuditDataServiceClient.Add(new BsSelectRequestAudit()
        {
            RequestId = requestId,
            StatusCode = statusCode.ToString(),
            CreatedDateTime = DateTime.UtcNow
        });
    }

    private async Task<bool> MarkCohortDistributionParticipantsAsExtracted(List<CohortDistribution> cohortParticipants, Guid requestId)
    {

        if (cohortParticipants == null || cohortParticipants.Count == 0)
        {
            return false;
        }

        var extractedParticipants = cohortParticipants.Select(x => x.CohortDistributionId);

        foreach (var participantId in extractedParticipants)
        {
            var participant = await _cohortDistributionDataServiceClient.GetSingle(participantId.ToString());
            participant.IsExtracted = 1;
            participant.RequestId = requestId;

            var updatedRecord = await _cohortDistributionDataServiceClient.Update(participant);

            if (!updatedRecord)
            {
                return false;
            }
        }
        return true;
    }

    public async Task<CohortRequestAudit> GetNextCohortRequestAudit(Guid requestId)
    {
        int errorCodeInt = (int)HttpStatusCode.InternalServerError;
        int NoContentInt = (int)HttpStatusCode.NoContent;

        string errorCode = errorCodeInt.ToString();
        string NoContent = NoContentInt.ToString();

        var previousRequest = await _bsSelectRequestAuditDataServiceClient.GetSingleByFilter(x => x.RequestId == requestId);

        if (previousRequest == null)
        {
            throw new KeyNotFoundException("No RequestId Found");
        }

        var res = await _bsSelectRequestAuditDataServiceClient.GetByFilter(
            x => x.CreatedDateTime > previousRequest.CreatedDateTime
            && x.StatusCode != NoContent
            && x.StatusCode != errorCode
        );

        var recordToReturn = res.OrderBy(x => x.CreatedDateTime).FirstOrDefault();

        if (recordToReturn == null)
        {
            return null;
        }

        return new CohortRequestAudit()
        {
            RequestId = recordToReturn.RequestId,
            StatusCode = recordToReturn.StatusCode,
            CreatedDateTime = recordToReturn.CreatedDateTime.ToString()
        };
    }

    private List<CohortRequestAudit> BuildCohortRequestAudits(IEnumerable<BsSelectRequestAudit> bsSelectRequestAudits)
    {
        return bsSelectRequestAudits.Select(x => new CohortRequestAudit
        {
            RequestId = x.RequestId,
            StatusCode = x.StatusCode,
            CreatedDateTime = MappingUtilities.FormatDateTime(x.CreatedDateTime)
        }).OrderBy(x => x.CreatedDateTime).ToList();
    }

    private static Expression<Func<BsSelectRequestAudit, bool>> BuildCohortRequestAuditQuery(string? requestId, string? statusCode, DateTime? dateFrom)
    {
        var conditions = new List<Expression<Func<BsSelectRequestAudit, bool>>>();

        if (dateFrom.HasValue)
        {
            DateTime dateTimeValue = dateFrom.Value.Date;
            Expression<Func<BsSelectRequestAudit, bool>> predicate = (x => x.CreatedDateTime >= dateTimeValue);
            conditions.Add(predicate);
        }

        if (!string.IsNullOrEmpty(statusCode))
        {
            Expression<Func<BsSelectRequestAudit, bool>> predicate = (x => x.StatusCode == statusCode);
            conditions.Add(predicate);
        }

        if (!string.IsNullOrEmpty(requestId))
        {
            var parsedRequestId = Guid.Parse(requestId);
            Expression<Func<BsSelectRequestAudit, bool>> predicate = (x => x.RequestId == parsedRequestId);
            conditions.Add(predicate);
        }

        Expression<Func<BsSelectRequestAudit, bool>> finalPredicate;
        if (conditions.Count > 0)
        {

            finalPredicate = CombineWithAnd<BsSelectRequestAudit>(conditions);
            return finalPredicate;
        }
        return x => true;
    }

    private static Expression<Func<T, bool>> CombineWithAnd<T>(List<Expression<Func<T, bool>>> expressions)
    {

        var firstExpression = expressions.First();
        var parameter = firstExpression.Parameters[0];

        if (expressions.Count() == 1)
        {
            return firstExpression;
        }

        var body = expressions
            .Skip(1)
            .Aggregate(
                firstExpression.Body,
                (current, expression) =>
                {
                    var visitor = new ParameterReplacer(expression.Parameters[0], parameter);
                    var expressionBody = visitor.Visit(expression.Body);
                    return Expression.AndAlso(current, expressionBody);
                });

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
