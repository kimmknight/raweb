/**
 * Parses the CMS SignedData embedded in a signed RDP file's `signature:s:` value and returns the
 * pkijs `Certificate` it carries. Shared by `extractRdpSignatureCertificate` (which just wants
 * the raw bytes) and `getRdpSignatureCertificateInfo` (which wants the parsed fields).
 *
 * The `asn1js`/`pkijs` parsing libraries are only needed for this one feature, so they're
 * dynamically imported here rather than bundled into the app's main chunk.
 *
 * Throws `RdpSignatureCertificateError` if the value cannot be parsed or does not embed a
 * certificate.
 */
export async function parseRdpSignatureCertificate(signatureValue: string) {
  if (!signatureValue || !signatureValue.trim()) {
    throw new RdpSignatureCertificateError('The RDP file signature is empty.');
  }

  let blob: Uint8Array;
  try {
    blob = decodeBase64(signatureValue);
  } catch {
    throw new RdpSignatureCertificateError('The RDP file signature is not valid base64.');
  }

  if (blob.length < 12) {
    throw new RdpSignatureCertificateError('The RDP file signature is too short to contain a header.');
  }

  const view = new DataView(blob.buffer, blob.byteOffset, blob.byteLength);
  const derLength = view.getUint32(8, true);
  const der = blob.slice(12, 12 + derLength);
  if (der.length !== derLength) {
    throw new RdpSignatureCertificateError('The RDP file signature header does not match its content length.');
  }

  const [asn1js, { Certificate, ContentInfo, SignedData }] = await Promise.all([
    import('asn1js'),
    import('pkijs'),
  ]);

  const asn1 = asn1js.fromBER(der.buffer.slice(der.byteOffset, der.byteOffset + der.byteLength));
  if (asn1.result.error) {
    throw new RdpSignatureCertificateError('The RDP file signature is not valid CMS/PKCS#7 data.');
  }

  let contentInfo: InstanceType<typeof ContentInfo>;
  try {
    contentInfo = new ContentInfo({ schema: asn1.result });
  } catch {
    throw new RdpSignatureCertificateError('The RDP file signature is not a valid CMS ContentInfo structure.');
  }

  if (contentInfo.contentType !== ContentInfo.SIGNED_DATA) {
    throw new RdpSignatureCertificateError('The RDP file signature does not contain CMS SignedData.');
  }

  let signedData: InstanceType<typeof SignedData>;
  try {
    signedData = new SignedData({ schema: contentInfo.content });
  } catch {
    throw new RdpSignatureCertificateError('The RDP file signature could not be parsed as CMS SignedData.');
  }

  const certificate = signedData.certificates?.[0];
  if (!certificate) {
    throw new RdpSignatureCertificateError('This RDP file signature does not embed a certificate.');
  }
  if (!(certificate instanceof Certificate)) {
    throw new RdpSignatureCertificateError('The embedded certificate is not a standard X.509 certificate.');
  }

  return certificate;
}

/**
 * Extracts the raw DER-encoded X.509 certificate bytes from a signed RDP file's `signature:s:`
 * value (the value only - not including the `signature:s:` prefix). The returned bytes are a
 * complete, standalone certificate and can be saved directly as a `.cer` file.
 *
 * Throws `RdpSignatureCertificateError` if the value cannot be parsed or does not embed a
 * certificate.
 */
export async function extractRdpSignatureCertificate(signatureValue: string): Promise<Uint8Array> {
  const certificate = await parseRdpSignatureCertificate(signatureValue);
  return new Uint8Array(certificate.toSchema().toBER(false));
}

function decodeBase64(base64: string): Uint8Array {
  if (typeof Uint8Array.fromBase64 === 'function') {
    return Uint8Array.fromBase64(base64.replace(/\s+/g, ''));
  }

  const binary = atob(base64.replace(/\s+/g, ''));
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

export class RdpSignatureCertificateError extends Error {}
