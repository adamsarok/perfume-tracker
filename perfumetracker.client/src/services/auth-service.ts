import { getPerfumeTrackerApiAddress } from "./conf-service";

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  token: string;
  email: string;
}

export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const apiUrl = await getPerfumeTrackerApiAddress();
  const response = await fetch(`${apiUrl}/identity/account/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(credentials),
    credentials: 'include', // This is important for handling cookies
  });

  if (!response.ok) {
    throw new Error('Login failed');
  }

  return response.json();
};

export const logout = async (): Promise<void> => {
  const apiUrl = await getPerfumeTrackerApiAddress(); //TODO implement this in the backend
  await fetch(`${apiUrl}/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  });
};

export const getCurrentUser = async (): Promise<{ email: string } | null> => {
  const apiUrl = await getPerfumeTrackerApiAddress();
  try { //TODO implement this in the backend
    const response = await fetch(`${apiUrl}/auth/me`, {
      credentials: 'include',
    });
    
    if (!response.ok) {
      return null;
    }

    return response.json();
  } catch {
    return null;
  }
}; 