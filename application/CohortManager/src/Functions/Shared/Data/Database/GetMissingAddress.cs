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

                _connection.Open();

                using (IDbCommand command = _connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandType = System.Data.CommandType.Text;

                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = "@ParticipantId";
                    parameter.Value = _participant.ParticipantId;
                    command.Parameters.Add(parameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string storedPostcode = reader["POST_CODE"] as string;

                            if (_participant.Postcode != storedPostcode)
                            {
                                throw new ArgumentException("Participant has an empty address and postcode does not match existing data");
                            }

                            _participant.AddressLine1 = reader["ADDRESS_LINE_1"] as string;
                            _participant.AddressLine2 = reader["ADDRESS_LINE_2"] as string;
                            _participant.AddressLine3 = reader["ADDRESS_LINE_3"] as string;
                            _participant.AddressLine4 = reader["ADDRESS_LINE_4"] as string;
                            _participant.AddressLine5 = reader["ADDRESS_LINE_5"] as string;
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
