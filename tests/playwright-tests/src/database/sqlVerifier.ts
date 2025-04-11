import * as sql from 'mssql';
import { config } from '../config/env'

const sqlConfig = {
  user: config.sqlConfig.user,
  password: config.sqlConfig.password,
  server: config.sqlConfig.host,
  database: config.sqlConfig.database,
  options: {
    trustServerCertificate: true,
  },
};

export async function cleanupDatabase(nhsNumbers: string[]) {
  const validNHSNumbers = nhsNumbers.filter(number => /^\d{10}$/.test(number));

  const pool = await sql.connect(sqlConfig);
  try {
    const tables = ['PARTICIPANT_MANAGEMENT', 'PARTICIPANT_DEMOGRAPHIC', 'BS_COHORT_DISTRIBUTION', 'EXCEPTION_MANAGEMENT'];
    for (const table of tables) {
      const request = pool.request();
      validNHSNumbers.forEach((nhsNumber, index) => {
        request.input(`nhsNumber${index}`, sql.VarChar, nhsNumber);
      });

      const conditions = validNHSNumbers.map((_, index) => `@nhsNumber${index}`).join(',');
      await request.query(`DELETE FROM ${table} WHERE NHS_Number IN (${conditions})`);
      console.info(`Contents deleted from ${table} WHERE NHS_Number IN (${conditions})`);
    }
  } catch (err) {
    console.error('Error deleting contents:', err);
  } finally {
    await pool.close();
  }
}
