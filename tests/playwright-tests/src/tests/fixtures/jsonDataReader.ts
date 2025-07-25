import fs from 'fs';
import path from 'path';
import { config } from '../../config/env';
import { ParticipantRecord } from '../../interface/InputData';

export function getAllParticipantPayloads(): Record<string, ParticipantRecord> {
  if (!config.participantPayloadPath) {
    throw new Error('❌ PARTICIPANT_PAYLOAD_PATH not set in .env');
  }

  const filePath = path.join(__dirname, '..', config.participantPayloadPath);
  const rawData = fs.readFileSync(filePath, 'utf-8');
  return JSON.parse(rawData);
}

export function omitField<T extends object>(obj: T, fieldPath: string): T {
  const cloned = structuredClone(obj);
  const parts = fieldPath.split('.');
  let current: any = cloned;

  for (let i = 0; i < parts.length - 1; i++) {
    if (!(parts[i] in current)) return cloned;
    current = current[parts[i]];
  }

  delete current[parts[parts.length - 1]];
  return cloned;
}
