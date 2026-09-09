// Triggers a browser download for a blob fetched via HttpClient. Needed
// (rather than a plain <a href>) because these export endpoints require an
// auth token that only HttpClient's interceptor attaches — a raw link
// navigation would just hit an unauthorized response.
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  window.URL.revokeObjectURL(url);
}
