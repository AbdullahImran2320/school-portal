export interface LicenseStatus {
  isActivated: boolean;
  isTrial: boolean;
  isExpired: boolean;
  showWarning: boolean;
  daysRemaining: number;
  trialStartDate: string;
  trialEndDate: string;
  licenseEndDate: string | null;
  message: string;
}

export interface LicenseActivationResponse {
  success: boolean;
  message: string;
  licenseEndDate: string | null;
}
