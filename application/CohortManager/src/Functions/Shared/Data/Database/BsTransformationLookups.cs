namespace Data.Database;

using System.Data;
using Model;
using Microsoft.Data.SqlClient;

public class BsTransformationLookups : IBsTransformationLookups
{
    private IDbConnection _connection;
    private string _connectionString;

    public BsTransformationLookups(IDbConnection IdbConnection)
    {
        _connectionString = Environment.GetEnvironmentVariable("DtOsDatabaseConnectionString") ?? string.Empty;
        _connection = IdbConnection; //new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Used in rules 13 & 14 in the "Other" transformations.
    /// Gets the participant's previous family/ given name if an Amend record comes in without one.
    /// </summary>
    /// <param name="participantId">The participant's ID.</param>
    /// <param name="nameType">which name to retrieve, acceptable values are "FAMILY_NAME" and "GIVEN_NAME".</param>
    /// <returns>string, the participant's family/ given name.<returns>
    public string GetGivenName(string participantId)
    {
        return GetName(participantId, "GIVEN_NAME");
    }

    public string GetFamilyName(string participantId)
    {
        return GetName(participantId, "FAMILY_NAME");
    }
    public string GetName(string participantId, string nameType)
    {
        string sql = $"SELECT TOP 1 {nameType} FROM [dbo].[BS_COHORT_DISTRIBUTION] WHERE PARTICIPANT_ID = @participantId AND" +
                     $" {nameType} IS NOT NULL ORDER BY BS_COHORT_DISTRIBUTION_ID DESC";

        using (_connection = new SqlConnection(_connectionString))
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, (SqlConnection)_connection))
            {
                command.Parameters.AddWithValue("@ParticipantId", participantId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        System.Console.WriteLine("first name in database: " + reader.GetString(0));
                        return reader.GetString(0) ?? string.Empty;
                    }
                }
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Used in the transformations, gets the participant's address if a record comes in with a valid postcode but no address.
    /// </summary>
    /// <param name="participant">The participant.</param>
    /// <returns>CohortDistributionParticipant, the transformed participant.<returns>
    public CohortDistributionParticipant GetAddress(CohortDistributionParticipant participant)
    {
        string sql = $"SELECT POST_CODE, ADDRESS_LINE_1, ADDRESS_LINE_2, ADDRESS_LINE_3, ADDRESS_LINE_4, ADDRESS_LINE_5 " +
                    $"FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                    $"WHERE PARTICIPANT_ID = @ParticipantId";
        using (_connection)
        {
            _connection.Open();
            using (SqlCommand command = new SqlCommand(sql, (SqlConnection)_connection))
            {
                command.Parameters.AddWithValue("@ParticipantId", participant.ParticipantId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (participant.Postcode != reader["POST_CODE"] as string)
                        {
                            throw new ArgumentException("Participant has an empty address and postcode does not match existing data");
                        }

                        participant.AddressLine1 = reader["ADDRESS_LINE_1"] as string ?? null;
                        participant.AddressLine2 = reader["ADDRESS_LINE_2"] as string ?? null;
                        participant.AddressLine3 = reader["ADDRESS_LINE_3"] as string ?? null;
                        participant.AddressLine4 = reader["ADDRESS_LINE_4"] as string ?? null;
                        participant.AddressLine5 = reader["ADDRESS_LINE_5"] as string ?? null;
                    }
                }
            }
        }
        return participant;
    }

}