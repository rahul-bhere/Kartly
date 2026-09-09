export function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function isRequired(value: string): boolean {
  return value.trim().length > 0;
}

export function minLength(value: string, min: number): boolean {
  return value.trim().length >= min;
}

export interface FieldErrors {
  [field: string]: string;
}

/**
 * Runs a set of field validators and returns an error map.
 * Usage: validate({ email: () => isValidEmail(email) || "Enter a valid email" })
 */
export function validate(rules: Record<string, () => true | string>): FieldErrors {
  const errors: FieldErrors = {};
  for (const [field, rule] of Object.entries(rules)) {
    const result = rule();
    if (result !== true) errors[field] = result;
  }
  return errors;
}
