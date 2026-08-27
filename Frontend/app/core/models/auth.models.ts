export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  fullName: string;
}

export interface LoginResult {
  token: string;
  username: string;
  role: string;
  fullName: string;
}

export interface RegisterResult {
  success: boolean;
  message: string;
  errorCode?: string;
}