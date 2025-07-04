export function ensureNhsNumbersStartWith999(nhsNumbers: string[]) {
  nhsNumbers.forEach((original) => {
    let numeric = original.replace(/\D/g, "");

    if (!numeric.startsWith("999")) {
      throw new Error(`NHS number must start with 999: ${original}`);
    }
  });
}
