/**
 * Converts PascalCase to camelCase.
 * @param str
 * @returns
 */
export const pascalCaseToCamelCase = (str: string) => {
  return str.charAt(0).toLowerCase() + str.slice(1);
};
