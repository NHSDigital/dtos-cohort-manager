namespace Data.Database;

using System.Data;
using Model;
using Microsoft.Data.SqlClient;

public class GetMissingAddress
{
    private readonly CohortDistributionParticipant _participant;
    private readonly IDbConnection _connection;
    public GetMissingAddress(CohortDistributionParticipant participant, IDbConnection connection)
    {
        _participant = participant;
        _connection = connection;
    }

    public CohortDistributionParticipant GetAddress()
    {
        try
        {
            string sql = $"SELECT POST_CODE, ADDRESS_LINE_1, ADDRESS_LINE_2, ADDRESS_LINE_3, ADDRESS_LINE_4, ADDRESS_LINE_5 " +
                    $"FROM [dbo].[BS_COHORT_DISTRIBUTION] " +
                    $"WHERE PARTICIPANT_ID = @ParticipantId";
            using (_connection)
            {
                _connection.Open();
                using (SqlCommand command = new SqlCommand(sql, (SqlConnection)_connection))
                {
                    command.Parameters.AddWithValue("@ParticipantId", _participant.ParticipantId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (_participant.Postcode != reader["POST_CODE"] as string)
                            {
                                throw new ArgumentException("Participant has an empty address and postcode does not match existing data");
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
        finally
        {
            if (_connection != null)
            {
                _connection.Close();
            }
        }

    }

}
