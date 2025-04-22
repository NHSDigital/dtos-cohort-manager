import * as parquet from 'parquetjs';
import * as path from 'path';

export async function createParquetFromJson(
  nhsNumbers: string[],
  inputParticipantRecord: Record<string, any>,
  testFilesPath: string,
  recordType: string = 'ADD',
  multiply: boolean = true
): Promise<string> {
  try {
    const reader = await parquet.ParquetReader.openFile(path.join(__dirname, `schema.parquet`));
    const outputFilePath = `${testFilesPath}${recordType}${nhsNumbers.length}_-_CAAS_BREAST_SCREENING_COHORT.parquet`;
    const schema = reader.getSchema();

    await reader.close();
    const writer = await parquet.ParquetWriter.openFile(schema, outputFilePath);
    const baseRecords: Record<string, any> = inputParticipantRecord;

    for (const baseRecord of Object.values(baseRecords)){


    if (multiply) {
      let updatedNameForAmended = baseRecord.given_name;
      if (recordType === 'AMENDED') {
        updatedNameForAmended = `${baseRecord.given_name}Updated`;
      }

      for (const nhsNumber of nhsNumbers) {
        const updatedRecord = {
          ...baseRecord,
          nhs_number: nhsNumber,
          serial_change_number: nhsNumbers.indexOf(nhsNumber) + 1,
          record_type: recordType,
          given_name: `${updatedNameForAmended}`,
        };
        await writer.appendRow(updatedRecord);
      }


    } else {
      await writer.appendRow(baseRecord);
    }

  }

    await writer.close();
    console.info(`New Parquet file created with updated records: ${outputFilePath}`);
    return outputFilePath;
  } catch (error: any) { //TODO: fix this error type
    console.error('Error processing Parquet file:', error);
    return error.message.toString();
  }
}

