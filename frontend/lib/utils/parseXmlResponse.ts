export async function parseXmlResponse(response: Response) {
  return response.text().then((text) => {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(text, 'application/xml');
    if (xmlDoc.getElementsByTagName('parsererror').length) {
      throw new Error('Error parsing XML response');
    }
    return xmlDoc.documentElement;
  });
}
