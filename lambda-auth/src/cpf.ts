/**
 * Validação de CPF/CNPJ — portada de OficinaMecanica.Application.Validators.CpfCnpjValidator
 * (app .NET) para manter a MESMA regra de negócio na autenticação serverless.
 *
 * Suporta o novo formato alfanumérico de CNPJ (vigente desde jan/2026).
 */

/** Remove pontuação e uppercaseia, mantendo apenas letras e dígitos. */
export function normalize(value: string): string {
  return (value ?? "")
    .toUpperCase()
    .split("")
    .filter((c) => /[A-Z0-9]/.test(c))
    .join("");
}

export function isValidCpfCnpj(value: string | undefined | null): boolean {
  if (!value || !value.trim()) return false;
  const cleaned = normalize(value);
  if (cleaned.length === 11) return isValidCpf(cleaned);
  if (cleaned.length === 14) return isValidCnpj(cleaned);
  return false;
}

export function isValidCpf(cpf: string): boolean {
  if (!/^\d{11}$/.test(cpf)) return false;
  if (new Set(cpf).size === 1) return false;

  const digits = cpf.split("").map((c) => c.charCodeAt(0) - 48);

  let sum = 0;
  for (let i = 0; i < 9; i++) sum += digits[i]! * (10 - i);
  let rem = sum % 11;
  const d1 = rem < 2 ? 0 : 11 - rem;
  if (digits[9] !== d1) return false;

  sum = 0;
  for (let i = 0; i < 10; i++) sum += digits[i]! * (11 - i);
  rem = sum % 11;
  const d2 = rem < 2 ? 0 : 11 - rem;
  return digits[10] === d2;
}

export function isValidCnpj(cnpj: string): boolean {
  for (let i = 0; i < 12; i++) {
    const c = cnpj[i]!;
    if (!/[0-9]/.test(c) && !(c >= "A" && c <= "Z")) return false;
  }
  if (!/[0-9]/.test(cnpj[12]!) || !/[0-9]/.test(cnpj[13]!)) return false;
  if (new Set(cnpj).size === 1) return false;

  const val = (c: string): number =>
    /[0-9]/.test(c) ? c.charCodeAt(0) - 48 : c.charCodeAt(0) - 65 + 10;

  const w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const w2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

  let sum = 0;
  for (let i = 0; i < 12; i++) sum += val(cnpj[i]!) * w1[i]!;
  let rem = sum % 11;
  const d1 = rem < 2 ? 0 : 11 - rem;
  if (cnpj.charCodeAt(12) - 48 !== d1) return false;

  sum = 0;
  for (let i = 0; i < 13; i++) sum += val(cnpj[i]!) * w2[i]!;
  rem = sum % 11;
  const d2 = rem < 2 ? 0 : 11 - rem;
  return cnpj.charCodeAt(13) - 48 === d2;
}
