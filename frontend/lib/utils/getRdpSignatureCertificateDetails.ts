import { parseRdpSignatureCertificate } from '$utils';
import { t } from 'i18next';

export interface CertificateDetailField {
  label: string;
  value: string;
}

export interface CertificateDetails {
  /** Common Name (CN) for the certificate recipient. */
  issuedTo: string;
  /** Common Name (CN) for the certificate issuer. */
  issuedBy: string;
  validFrom: Date;
  validTo: Date;
  /** Array of human-readable purposes */
  purposes: string[];
  /** Whether the Certificate Policies extension includes the "any policy" OID. */
  hasAllIssuancePolicies: boolean;
  /** Rows of data for the details tab. */
  fields: CertificateDetailField[];
  /** SHA-1 thumbprint (hex, uppercase, no separators). */
  thumbprint: string;
  isExpired: boolean;
  isNotYetValid: boolean;
}

/**
 * Parses a signed RDP file's `signature:s:` value and returns a structured description of the
 * embedded certificate, formatted the way Windows' certificate viewer (certmgr/`.cer` double-
 * click) presents it - a friendly "General" tab summary plus an ordered "Details" tab field list.
 *
 * Throws `RdpSignatureCertificateError` (see extractRdpSignatureCertificate.ts) if the value
 * cannot be parsed or does not embed a certificate.
 */
export async function getRdpSignatureCertificateDetails(signatureValue: string): Promise<CertificateDetails> {
  const certificate = await parseRdpSignatureCertificate(signatureValue);

  const issuedTo = getCommonName(certificate.subject);
  const issuedBy = getCommonName(certificate.issuer);
  const validFrom: Date = certificate.notBefore.value;
  const validTo: Date = certificate.notAfter.value;

  const signatureAlgorithmId: string = certificate.signatureAlgorithm.algorithmId;
  const signatureAlgorithmName = SIGNATURE_ALGORITHM_NAMES[signatureAlgorithmId] ?? signatureAlgorithmId;
  const signatureHashAlgorithmName = HASH_ALGORITHM_NAMES[signatureAlgorithmId] ?? signatureAlgorithmId;

  const publicKeyAlgorithmId: string | undefined = certificate.subjectPublicKeyInfo?.algorithm?.algorithmId;
  const publicKeyBits = (() => {
    try {
      const parsedKey = certificate.subjectPublicKeyInfo?.parsedKey;
      if (parsedKey && 'modulus' in parsedKey) {
        return parsedKey.modulus.valueBlock.valueHexView.length * 8;
      }
      return undefined;
    } catch {
      return undefined;
    }
  })();
  const publicKeyAlgorithmName = publicKeyAlgorithmId
    ? (PUBLIC_KEY_ALGORITHM_NAMES[publicKeyAlgorithmId] ?? publicKeyAlgorithmId).replace(
        '%bits%',
        String(publicKeyBits ?? '?')
      )
    : undefined;

  const extensions = certificate.extensions ?? [];
  const purposes: string[] = [];
  let hasCertificatePoliciesExtension = false;
  let hasAllIssuancePolicies = false;
  const fields: CertificateDetailField[] = [
    { label: 'Version', value: `V${(certificate.version ?? 0) + 1}` },
    { label: 'Serial number', value: toHexString(certificate.serialNumber.valueBlock.valueHexView) },
    { label: 'Signature algorithm', value: signatureAlgorithmName },
    { label: 'Signature hash algorithm', value: signatureHashAlgorithmName },
    { label: 'Issuer', value: formatDistinguishedName(certificate.issuer) },
    { label: 'Valid from', value: formatDate(validFrom) },
    { label: 'Valid to', value: formatDate(validTo) },
    { label: 'Subject', value: formatDistinguishedName(certificate.subject) },
  ];
  if (publicKeyAlgorithmName) {
    fields.push({ label: 'Public key', value: publicKeyAlgorithmName });
  }

  // Extension internals vary by library version and by which extensions a given certificate
  // actually carries, so each one is parsed defensively - a shape mismatch on any single
  // extension should not prevent the rest of the certificate's details from being shown.
  for (const extension of extensions) {
    try {
      // get permitted cryptographic operations for the key
      if (extension.extnID === OID_KEY_USAGE && extension.parsedValue) {
        const parsedValue = extension.parsedValue as import('asn1js').BitString;

        // each bit in the BitString corresponds to a Key Usage flag
        const bits = Array.from(parsedValue.valueBlock.valueHexView);
        const flags = KEY_USAGE_BIT_NAMES.filter((_, i) => {
          const byteIndex = Math.floor(i / 8);
          const bitIndex = 7 - (i % 8);
          return ((bits[byteIndex] ?? 0) >> bitIndex) & 1;
        });

        if (flags.length > 0) {
          fields.push({ label: 'Key Usage', value: flags.join(', ') });
        }
      }

      // get the intended purposes for which the certificate may be used
      else if (extension.extnID === OID_EXTENDED_KEY_USAGE && extension.parsedValue?.keyPurposes) {
        const parsedValue = extension.parsedValue as import('pkijs').ExtKeyUsage;

        const displayNames = parsedValue.keyPurposes.map((oid) => {
          const translationKey = EXTENDED_KEY_USAGE_TRANSLATION_KEYS[oid];
          if (!translationKey) {
            return oid;
          }
          return t(`resource.certificateViewer.extendedKeyUsage.${translationKey}`, { defaultValue: oid });
        });

        fields.push({ label: 'Enhanced Key Usage', value: displayNames.join(', ') });
        purposes.push(...new Set(displayNames));
      }

      // get the certificate's issuance policies (if any)
      else if (extension.extnID === OID_CERTIFICATE_POLICIES && extension.parsedValue?.certificatePolicies) {
        hasCertificatePoliciesExtension = true;
        const parsedValue = extension.parsedValue as import('pkijs').CertificatePolicies;

        const policyOids: string[] = parsedValue.certificatePolicies.map((policy) => policy.policyIdentifier);
        if (policyOids.includes(OID_ANY_POLICY)) {
          hasAllIssuancePolicies = true;
        }
        fields.push({ label: 'Certificate Policies', value: policyOids.join(', ') });
      }

      // identify whether this is a certificate authority
      else if (extension.extnID === OID_BASIC_CONSTRAINTS) {
        const parsedValue = extension.parsedValue as import('pkijs').BasicConstraints;

        const isCa = parsedValue.cA ?? false;
        fields.push({
          label: 'Basic Constraints',
          value: isCa ? 'Subject Type=CA' : 'Subject Type=End Entity',
        });
      }

      // get the sha-1 hash of the certificate's public key
      else if (extension.extnID === OID_SUBJECT_KEY_IDENTIFIER) {
        const parsedValue = extension.parsedValue as import('asn1js').OctetString;
        const hash = parsedValue.valueBlock.valueHexView;

        if (hash) {
          fields.push({
            label: 'Subject Key Identifier',
            value: toHexString(hash),
          });
        }
      }

      // get the sha-1 hash of the certificate authority's public key (if this is an end-entity cert)
      else if (extension.extnID === OID_AUTHORITY_KEY_IDENTIFIER) {
        const parsedValue = extension.parsedValue as import('pkijs').AuthorityKeyIdentifier;
        const keyId = parsedValue.keyIdentifier?.valueBlock.valueHexView;
        if (keyId) {
          fields.push({ label: 'Authority Key Identifier', value: `KeyID=${toHexString(keyId)}` });
        }
      }

      // get alternative names for the certificate subject
      else if (extension.extnID === OID_SUBJECT_ALT_NAME && extension.parsedValue?.altNames) {
        const names: string[] = extension.parsedValue.altNames
          .map((altName: { type: number; value: string }) => {
            if (altName.type === 2) return `DNS Name=${altName.value}`;
            if (altName.type === 7) return `IP Address=${altName.value}`;
            return altName.value;
          })
          .filter(Boolean);
        if (names.length > 0) {
          fields.push({ label: 'Subject Alternative Name', value: names.join(', ') });
        }
      }
    } catch {
      // ignore extensions that do not parse correctly
    }
  }

  if (purposes.length === 0) {
    // when no extended key usage extension is present, the certificate is valid for every purpose
    purposes.push('All application policies');
  }
  if (!hasCertificatePoliciesExtension) {
    // when no certificate policies extension is present, the certificate is valid for every issuance policy
    hasAllIssuancePolicies = true;
  }
  if (hasAllIssuancePolicies) {
    purposes.push('All issuance policies');
  }

  const bytes = certificate.toSchema().toBER(false);
  const thumbprint = await getSha1HexString(bytes);
  fields.push({ label: 'Thumbprint algorithm', value: 'sha1' });
  fields.push({ label: 'Thumbprint', value: thumbprint });

  const now = new Date();
  console.log({
    issuedTo,
    issuedBy,
    validFrom,
    validTo,
    purposes,
    hasAllIssuancePolicies,
    fields,
    thumbprint,
    isExpired: validTo < now,
    isNotYetValid: validFrom > now,
  });
  return {
    issuedTo,
    issuedBy,
    validFrom,
    validTo,
    purposes,
    hasAllIssuancePolicies,
    fields,
    thumbprint,
    isExpired: validTo < now,
    isNotYetValid: validFrom > now,
  };
}

const OID_COMMON_NAME = '2.5.4.3';
const OID_ORGANIZATION = '2.5.4.10';
const OID_ORGANIZATIONAL_UNIT = '2.5.4.11';
const OID_COUNTRY = '2.5.4.6';
const OID_STATE = '2.5.4.8';
const OID_LOCALITY = '2.5.4.7';
const OID_EMAIL = '1.2.840.113549.1.9.1';

const ATTRIBUTE_TYPE_ABBREVIATIONS: Record<string, string> = {
  [OID_COMMON_NAME]: 'CN',
  [OID_ORGANIZATION]: 'O',
  [OID_ORGANIZATIONAL_UNIT]: 'OU',
  [OID_COUNTRY]: 'C',
  [OID_STATE]: 'S',
  [OID_LOCALITY]: 'L',
  [OID_EMAIL]: 'E',
};

const SIGNATURE_ALGORITHM_NAMES: Record<string, string> = {
  '1.2.840.113549.1.1.4': 'md5RSA',
  '1.2.840.113549.1.1.5': 'sha1RSA',
  '1.2.840.113549.1.1.11': 'sha256RSA',
  '1.2.840.113549.1.1.12': 'sha384RSA',
  '1.2.840.113549.1.1.13': 'sha512RSA',
  '1.2.840.10045.4.1': 'sha1ECDSA',
  '1.2.840.10045.4.3.2': 'sha256ECDSA',
  '1.2.840.10045.4.3.3': 'sha384ECDSA',
  '1.2.840.10045.4.3.4': 'sha512ECDSA',
};

const HASH_ALGORITHM_NAMES: Record<string, string> = {
  '1.2.840.113549.1.1.4': 'md5',
  '1.2.840.113549.1.1.5': 'sha1',
  '1.2.840.113549.1.1.11': 'sha256',
  '1.2.840.113549.1.1.12': 'sha384',
  '1.2.840.113549.1.1.13': 'sha512',
  '1.2.840.10045.4.1': 'sha1',
  '1.2.840.10045.4.3.2': 'sha256',
  '1.2.840.10045.4.3.3': 'sha384',
  '1.2.840.10045.4.3.4': 'sha512',
};

const PUBLIC_KEY_ALGORITHM_NAMES: Record<string, string> = {
  '1.2.840.113549.1.1.1': 'RSA (%bits% Bits)',
  '1.2.840.10045.2.1': 'ECC',
};

const EXTENDED_KEY_USAGE_TRANSLATION_KEYS: Record<string, string> = {
  '1.3.6.1.5.5.7.3.1': 'serverAuth',
  '1.3.6.1.5.5.7.3.2': 'clientAuth',
  '1.3.6.1.5.5.7.3.3': 'codeSigning',
  '1.3.6.1.5.5.7.3.4': 'emailProtection',
  '1.3.6.1.5.5.7.3.8': 'timeStamping',
  '1.3.6.1.5.5.7.3.9': 'ocspSigning',
  '1.3.6.1.4.1.311.10.3.4': 'encryptingFileSystem',
};

const KEY_USAGE_BIT_NAMES = [
  'Digital Signature',
  'Non-Repudiation',
  'Key Encipherment',
  'Data Encipherment',
  'Key Agreement',
  'Certificate Signing',
  'Off-line CRL Signing',
  'CRL Signing',
];

const OID_ANY_POLICY = '2.5.29.32.0';
const OID_KEY_USAGE = '2.5.29.15';
const OID_EXTENDED_KEY_USAGE = '2.5.29.37';
const OID_SUBJECT_ALT_NAME = '2.5.29.17';
const OID_BASIC_CONSTRAINTS = '2.5.29.19';
const OID_SUBJECT_KEY_IDENTIFIER = '2.5.29.14';
const OID_AUTHORITY_KEY_IDENTIFIER = '2.5.29.35';
const OID_CERTIFICATE_POLICIES = '2.5.29.32';

function formatDistinguishedName(rdn: {
  typesAndValues: { type: string; value: { valueBlock: { value: string } } }[];
}) {
  return rdn.typesAndValues
    .map((tv) => `${ATTRIBUTE_TYPE_ABBREVIATIONS[tv.type] ?? tv.type}=${tv.value.valueBlock.value}`)
    .join(', ');
}

function getCommonName(rdn: { typesAndValues: { type: string; value: { valueBlock: { value: string } } }[] }) {
  const cn = rdn.typesAndValues.find((tv) => tv.type === OID_COMMON_NAME);
  return cn ? cn.value.valueBlock.value : formatDistinguishedName(rdn);
}

function formatDate(date: Date) {
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

/**
 * Formats an ArrayBuffer or Uint8Array as a hex string with optional separators between bytes.
 * The returned string is uppercase.
 */
function toHexString(bytes: ArrayBuffer | Uint8Array, separator = ' ') {
  const view = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
  return Array.from(view, (b) => b.toString(16).padStart(2, '0'))
    .join(separator)
    .toUpperCase();
}

/**
 * Gets the SHA-1 hash of the given bytes and returns it as a hex string.
 * See `toHexString()` for more info.
 */
async function getSha1HexString(bytes: ArrayBuffer) {
  const digest = await crypto.subtle.digest('SHA-1', bytes);
  return toHexString(digest, '');
}
