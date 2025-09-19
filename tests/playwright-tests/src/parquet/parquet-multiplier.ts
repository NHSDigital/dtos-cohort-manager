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
    const outputFilePath = `${testFilesPath}${recordType}_-_CAAS_BREAST_SCREENING_COHORT.parquet`;
    const schema = reader.getSchema();

    await reader.close();

    const baseRecords: any[] = Array.isArray(inputParticipantRecord)
      ? inputParticipantRecord
      : Object.values(inputParticipantRecord ?? {});

    const prospectiveRows = multiply
      ? baseRecords.length * (nhsNumbers?.length ?? 0)
      : baseRecords.length;

    if (!prospectiveRows || prospectiveRows <= 0) {
      console.warn('Parquet generation skipped: no rows to write');
      return 'NO_ROWS_TO_WRITE';
    }

    const writer = await parquet.ParquetWriter.openFile(schema, outputFilePath);

    for (const baseRecord of baseRecords) {
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
    const msg = (error && typeof error === 'object' && 'message' in error)
      ? (error as any).message
      : String(error);
    return String(msg);
  }
}
