export function ensureNhsNumbersStartWith999(nhsNumbers: (string | null | undefined)[]) {
  nhsNumbers.forEach((original) => {
    if (!original || original === '' || original === 'undefined') return; // allow null, undefined, empty string, or string 'undefined' for negative tests where nhs number is not updated

    let numeric = original.replace(/\D/g, "");

    if (!numeric.startsWith("999")) {
      throw new Error(`NHS number must start with 999: ${original}`);
    }
  });
}
