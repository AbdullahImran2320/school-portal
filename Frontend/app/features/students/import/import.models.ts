export interface StudentImportRowResult {
  rowNumber: number;
  studentName: string;
  outcome: 'Imported' | 'Skipped' | 'Failed';
  detail: string | null;
}

export interface StudentImportResult {
  totalRows: number;
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  results: StudentImportRowResult[];
}
