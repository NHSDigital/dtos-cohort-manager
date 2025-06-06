import fs from 'fs';
import path from 'path';

export const createTempDirAndWriteJson = (jsonData: any[], fileName = 'temp-data-json.json'): string => {
  const tempDirPath = path.join(process.cwd(), 'temp');
  const tempFilePath = path.join(tempDirPath, fileName);

  fs.mkdirSync(tempDirPath, { recursive: true });
  fs.writeFileSync(tempFilePath, JSON.stringify([jsonData], null, 2), 'utf-8');

  console.log(fs.existsSync(tempFilePath)
    ? `✅ File saved at: ${tempFilePath}`
    : '❌ File was not created.'
  );

  return tempFilePath;
};

export const deleteTempDir = (): void => {
  const tempDirPath = path.join(process.cwd(), 'temp');

  try {
    fs.rmSync(tempDirPath, { recursive: true, force: true });
    console.log(`🧹 Deleted temp directory: ${tempDirPath}`);
  } catch (error: any) {
    console.error('❌ Error deleting temp directory:', error.message);
  }
};
