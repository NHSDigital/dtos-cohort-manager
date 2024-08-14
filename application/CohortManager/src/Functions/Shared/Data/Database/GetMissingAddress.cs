using System.Data;
using Model;
using System.Data.SqlClient;

namespace Data.Database;

public class GetMissingAddress() 
{
    private CohortDistributionParticipant _participant;
    private IDbConnection _connection;
    public GetMissingAddress(CohortDistributionParticipant participant, IDbConnection connection)
    {
        _participant = participant;
        _connection = connection;
    }

    public CohortDistributionParticipant GetAddress()
    {
        using (_connection)
        {
            _connection.Open();

            string sql = $"SELECT POST_CODE, ADDRESS_LINE_1, ADDRESS_LINE_2, ADDRESS_LINE_3, ADDRESS_LINE_4, ADDRESS_LINE_5 " +
                        $"FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                        $"WHERE PARTICIPANT_ID = '{_participant.ParticipantId}'";

            using (SqlCommand command = new SqlCommand(sql, (SqlConnection) _connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (participant.Postcode != reader["POST_CODE"] as string)
                        {
                            // will be changed to call exception service
                            throw new ArgumentException();
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