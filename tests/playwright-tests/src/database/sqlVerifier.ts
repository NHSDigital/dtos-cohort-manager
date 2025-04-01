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

export async function checkSqlServerConnection(): Promise<boolean> {
  let connection;
  try {
    connection = await sql.connect(sqlConfig);
    console.log('SQL Server connection successful');
    return true;
  } catch (err) {
    console.error('SQL Server connection failed:', err);
    return false;
  } finally {
    if (connection) {
      await connection.close();
    }
  }
}

export async function validateSqlData(validations: any): Promise<boolean> {
  const pool = await sql.connect(sqlConfig);
  const results = [];

  try {
    for (const validationObj of validations) {
      const { tableName, ...columns } = validationObj.validations;
      let query = `SELECT COUNT(*) AS count FROM ${tableName} WHERE `;
      const conditions = [];
      const inputs = [];

      for (const [key, value] of Object.entries(columns)) {
        if (key.startsWith('columnName')) {
          const columnIndex = key.replace('columnName', '');
          const columnValueKey = `columnValue${columnIndex}`;
          conditions.push(`${value} = @value${columnIndex}`);
          inputs.push({ name: `value${columnIndex}`, value: columns[columnValueKey] });
        }
      }

      query += conditions.join(' AND ');

      let result;
      let retries = Number(config.sqlRetry);
      let waitTime = Number(config.sqlWaitTime);

      for (let attempt = 1; attempt <= retries; attempt++) {
        const request = pool.request();
        inputs.forEach(input => request.input(input.name, sql.VarChar, input.value));
        result = await request.query(query);

        if (result.recordset[0].count > 0) {
          console.info(`Validation passed for ${JSON.stringify(columns)} in table ${tableName}`);
          results.push({ columns, tableName, status: 'pass' });
          break;
        } else {
          console.warn(`Validation failed for ${JSON.stringify(columns)} in table ${tableName}, attempt ${attempt} after ${waitTime} seconds`);
          if (attempt < retries) {
            await new Promise(resolve => setTimeout(resolve, waitTime));
            waitTime += 5000;
          } else {
            results.push({ columns, tableName, status: 'fail' });
          }
        }
      }
    }

    const hasFailures = results.some(result => result.status === 'fail');
    return !hasFailures;
  } catch (error) {
    console.error('Error checking validations:', error); //  surface test failures in logs for easy debugging
    throw error;
  } finally {
    pool.close();
  }
}

export async function cleanupDatabase(nhsNumbers: string[]) {

  const pool = await sql.connect(sqlConfig);
  try {
    const conditions = nhsNumbers.map(item => `'${item}'`).join(',');
    const tables = ['PARTICIPANT_MANAGEMENT', 'PARTICIPANT_DEMOGRAPHIC', 'BS_COHORT_DISTRIBUTION', 'EXCEPTION_MANAGEMENT']
    for (const table of tables) {
      await pool.request().query(`DELETE FROM ${table} WHERE NHS_Number IN (${conditions})`);
      console.info(`Contents deleted from ${table} WHERE NHS_Number IN (${conditions})`);
    }
  } catch (err) {
    console.error('Error deleting contents:', err);
  } finally {
    await pool.close();
  }
}
