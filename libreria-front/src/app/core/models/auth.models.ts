export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  nombreCompleto: string;
  role?: number;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  nombreCompleto: string;
  email: string;
  role: string;
}

export type UserRole = 'Admin' | 'Vendedor' | 'Cliente';

export interface AuthState {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  nombreCompleto: string;
  email: string;
  role: UserRole;
}
