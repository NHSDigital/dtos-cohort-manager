namespace Data.Database;

using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Model.DTO;
using Model.Enums;
using NHS.CohortManager.Shared.Utilities;

public class CreateCohortDistributionData : ICreateCohortDistributionData
{
    private readonly ILogger<CreateCohortDistributionData> _logger;

    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataServiceClient;

    private readonly IDataServiceClient<BsSelectRequestAudit> _bsSelectRequestAuditDataServiceClient;

    public CreateCohortDistributionData(ILogger<CreateCohortDistributionData> logger, IDataServiceClient<CohortDistribution> cohortDistributionDataServiceClient, IDataServiceClient<BsSelectRequestAudit> bsSelectRequestAuditDataServiceClient)
    {
        _logger = logger;
        _cohortDistributionDataServiceClient = cohortDistributionDataServiceClient;
        _bsSelectRequestAuditDataServiceClient = bsSelectRequestAuditDataServiceClient;
    }


    public async Task<List<CohortDistributionParticipantDto>> GetUnextractedCohortDistributionParticipants(int rowCount)
    {
        var participantsList = await _cohortDistributionDataServiceClient.GetByFilter(x => x.IsExtracted.Equals(0) && x.RequestId == Guid.Empty);
        var CohortDistributionParticipantList = participantsList.Select(x => new CohortDistributionParticipant(x)).Take(rowCount).ToList();


        var requestId = Guid.NewGuid().ToString();
        if (await MarkCohortDistributionParticipantsAsExtracted(CohortDistributionParticipantList, requestId))
        {
            LogRequestAudit(requestId, (int)HttpStatusCode.OK);

            return CohortDistributionParticipantDto(CohortDistributionParticipantList, requestId);
        }

        var statusCode = CohortDistributionParticipantList.Count == 0 ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.InternalServerError;
        LogRequestAudit(requestId, statusCode);

        return new List<CohortDistributionParticipantDto>();
    }

    public async Task<List<CohortDistributionParticipantDto>> GetCohortDistributionParticipantsByRequestId(Guid requestId)
    {
        var requestIdString = requestId.ToString();
        if (requestId == Guid.Empty)
        {
            CohortDistributionParticipantDto(new List<CohortDistributionParticipant>());
        }

        var participantsList = await _cohortDistributionDataServiceClient.GetByFilter(x => x.RequestId == requestId);
        var cohortList = participantsList.Select(x => new CohortDistributionParticipant(x)).ToList();

        return CohortDistributionParticipantDto(cohortList);
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

    private static List<CohortDistributionParticipantDto> CohortDistributionParticipantDto(List<CohortDistributionParticipant> listOfAllParticipants, string newRequestId = "")
    {
        return listOfAllParticipants.Select(s => new CohortDistributionParticipantDto
        {
            RequestId = CreateRequestId(newRequestId),
            NhsNumber = s.NhsNumber ?? string.Empty,
            SupersededByNhsNumber = s.SupersededByNhsNumber ?? string.Empty,
            PrimaryCareProvider = s.PrimaryCareProvider ?? string.Empty,
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

    private static string CreateRequestId(string newRequestId)
    {
        if (!string.IsNullOrEmpty(newRequestId))
        {
            return newRequestId;
        }
        return Guid.Empty.ToString();
    }

    private void LogRequestAudit(string requestId, int statusCode)
    {
        _bsSelectRequestAuditDataServiceClient.Add(new BsSelectRequestAudit()
        {
            RequestId = Guid.Parse(requestId),
            StatusCode = statusCode.ToString(),
            CreatedDateTime = DateTime.Now
        });
    }

    private async Task<bool> MarkCohortDistributionParticipantsAsExtracted(List<CohortDistributionParticipant> cohortParticipants, string requestId)
    {
        var requestIdParsed = Guid.TryParse(requestId, out var parsedRequestId);
        if (cohortParticipants == null || cohortParticipants.Count == 0 || !requestIdParsed)
        {
            return false;
        }

        var allUnextractedParticipants = await _cohortDistributionDataServiceClient.
        GetByFilter(x => x.IsExtracted.Equals(0) && x.RequestId == Guid.Empty);

        var filteredCohortDistribution = allUnextractedParticipants.
            OrderBy(x => x.RecordInsertDateTime)
            .Take(cohortParticipants.Count);


        foreach (var filteredCohortDistributionRecord in filteredCohortDistribution)
        {
            filteredCohortDistributionRecord.IsExtracted = 1;
            filteredCohortDistributionRecord.RequestId = parsedRequestId;

            var updatedRecord = await _cohortDistributionDataServiceClient.Update(filteredCohortDistributionRecord);
            if (!updatedRecord)
            {
                return false;
            }
        }
        return true;
    }

    public async Task<CohortRequestAudit> GetNextCohortRequestAudit(string requestId)
    {
        var parsedRequestId = Guid.Parse(requestId);

        int errorCodeInt = (int)HttpStatusCode.InternalServerError;
        int NoContentInt = (int)HttpStatusCode.NoContent;

        string errorCode = errorCodeInt.ToString();
        string NoContent = NoContentInt.ToString();

        var res = await _bsSelectRequestAuditDataServiceClient.GetByFilter(
            x => x.RequestId > parsedRequestId
            && x.StatusCode != NoContent
            && x.StatusCode != errorCode
        );

        var recordToReturn = res.OrderByDescending(x => x.CreatedDateTime).FirstOrDefault();

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
            Expression<Func<BsSelectRequestAudit, bool>> predicate = (x => x.CreatedDateTime == dateFrom);
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
