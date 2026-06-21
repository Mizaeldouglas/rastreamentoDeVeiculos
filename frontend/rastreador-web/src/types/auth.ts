export interface AuthResponseDto {
  token: string;
  expiresAt: string;
  email: string;
  companyId: number;
  companyName: string;
}
