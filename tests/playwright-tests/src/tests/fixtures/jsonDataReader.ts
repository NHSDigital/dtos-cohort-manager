import fs from 'fs';
import path from 'path';
import { config } from '../../config/env';
import { ParticipantRecord } from '../../interface/InputData';


export function loadParticipantPayloads(folderName: string, fileName: string): Record<string, ParticipantRecord> {
  const fullPath = path.join(process.cwd(), config.participantPayloadPath, folderName, fileName);
  if (!fs.existsSync(fullPath)) {
    throw new Error(`❌ File not found: ${fullPath}`);
  }
  const rawData = fs.readFileSync(fullPath, 'utf-8');
  return JSON.parse(rawData);
}

export function omitField<T extends object>(obj: T, fieldPath: string): Partial<T> {
  const result = JSON.parse(JSON.stringify(obj));
  const parts = fieldPath.split('.');
  let current: any = result;

  for (let i = 0; i < parts.length - 1; i++) {
    if (!current[parts[i]]) return result;
    current = current[parts[i]];
  }

  delete current[parts[parts.length - 1]];
  return result;
}
