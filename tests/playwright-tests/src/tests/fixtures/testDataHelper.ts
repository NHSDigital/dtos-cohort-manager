export function ensureNhsNumbersStartWith999(nhsNumbers: (string | null)[]) {
  nhsNumbers.forEach((original) => {
    if (!original) return; // allow null or empty string

    let numeric = original.replace(/\D/g, "");

    if (!numeric.startsWith("999")) {
      throw new Error(`NHS number must start with 999: ${original}`);
    }
  });
}
