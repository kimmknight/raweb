export function generateRdpUri(contents: string, navigate = false) {
  const searchParams = new URLSearchParams();

  const rows = contents.split('\r\n');
  rows
    .map((row) => row.trim())
    .filter((row) => !!row)
    .map((row) => {
      const [key, ...valueParts] = row.split(':');
      searchParams.append(key, valueParts.map((part) => part.trim()).join(':'));
    });

  let uri = `rdp://${searchParams.toString()}`;

  if (navigate) {
    window.location.href = uri;
  }

  return uri;
}
