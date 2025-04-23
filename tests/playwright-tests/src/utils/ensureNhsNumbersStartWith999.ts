export function ensureNhsNumbersStartWith999(nhsNumbers: string[]): string[] {
  return nhsNumbers.map((original) => {
    let numeric = original.replace(/\D/g, "");

    if (numeric.length < 9) {
      numeric = numeric.padEnd(9, "0");
    } else if (numeric.length > 9) {
      numeric = numeric.slice(numeric.length - 9);
    }

    return "999" + numeric.slice(3);
  });
}
