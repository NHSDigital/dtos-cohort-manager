export const formatDate = (date: Date): string =>
  date.toISOString().slice(0, 10).replace(/-/g, '');

export const generateDynamicDateMap = (): Record<string, string> => {
  const today = new Date();

  return {
    DYNAMIC_DATE_TOMORROW: formatDate(new Date(today.getTime() + 86400000)),
    DYNAMIC_DATE_NEXT_MONTH: formatDate(new Date(today.getFullYear(), today.getMonth() + 1, today.getDate())),
    DYNAMIC_DATE_NEXT_YEAR: formatDate(new Date(today.getFullYear() + 1, today.getMonth(), today.getDate())),
  };
};

export const replaceDynamicDatesInJson = (
  records: Record<string, any>[],
  dateMap: Record<string, string>
): Record<string, any>[] => {
  return records.map((record) => {
    const updatedRecord = { ...record };

    Object.entries(updatedRecord).forEach(([key, value]) => {
      if (typeof value === 'string') {
        const trimmed = value.trim();
        if (trimmed in dateMap) {
          updatedRecord[key] = dateMap[trimmed];
          console.log(`✅ Replaced "${trimmed}" with "${updatedRecord[key]}" in field "${key}" for NHS Number: ${updatedRecord.nhs_number}`);
        }
      }
    });

    return updatedRecord;
  });
};
