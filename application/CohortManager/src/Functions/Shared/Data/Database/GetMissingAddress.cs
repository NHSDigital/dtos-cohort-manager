namespace Data.Database;

using System.Data;
using Model;
using System.Data.SqlClient;

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

            // TODO: SQL Params
            string sql = $"SELECT POST_CODE, ADDRESS_LINE_1, ADDRESS_LINE_2, ADDRESS_LINE_3, ADDRESS_LINE_4, ADDRESS_LINE_5 " +
                        $"FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                        $"WHERE PARTICIPANT_ID = '{_participant.ParticipantId}'";

            using (SqlCommand command = new SqlCommand(sql, (SqlConnection) _connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (_participant.Postcode != reader["POST_CODE"] as string)
                        {
                            // will be changed to call exception service
                            throw new ArgumentException();
                        }

                        _participant.AddressLine1 = reader["ADDRESS_LINE_1"] as string ?? null;
                        _participant.AddressLine2 = reader["ADDRESS_LINE_2"] as string ?? null;
                        _participant.AddressLine3 = reader["ADDRESS_LINE_3"] as string ?? null;
                        _participant.AddressLine4 = reader["ADDRESS_LINE_4"] as string ?? null;
                        _participant.AddressLine5 = reader["ADDRESS_LINE_5"] as string ?? null;
                    }
                }
            }
        }
        return _participant;
    }
    
}