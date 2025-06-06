import { getPerfumeTrackerApiAddress } from "./conf-service";
import axios from "axios";

export interface LoginResponse {
  email: string;
  token: string;
}

export interface UserResponse {
  userName: string;
  email: string;
  id: string;
  roles: string[];
}

export const api = axios.create({
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
  xsrfCookieName: 'XSRF-TOKEN',
  xsrfHeaderName: 'X-XSRF-TOKEN',
});

// Add response interceptor to handle auth errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized error
      console.error('Authentication error:', error);
    }
    return Promise.reject(error);
  }
);

export async function login(email: string, password: string): Promise<LoginResponse> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  console.log('Attempting login to:', apiUrl);
  
  const response = await api.post<LoginResponse>(`${apiUrl}/identity/account/login`, {
    email,
    password,
  });

  console.log('Login successful for:', response.data.email);
  return response.data;
}

export async function logout(): Promise<void> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  console.log('Attempting logout from:', apiUrl);
  
  await api.post(`${apiUrl}/identity/account/logout`);
  console.log('Logout successful');
}

export async function getCurrentUser(): Promise<UserResponse | null> {
  const apiUrl = await getPerfumeTrackerApiAddress();
  console.log('Checking current user from:', apiUrl);
  
  try {
    const response = await api.get<UserResponse>(`${apiUrl}/identity/account/me`);
    console.log('Current user found:', response.data.email);
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      console.error('Error checking current user:', error.response?.status, error.response?.data);
    } else {
      console.error('Error checking current user:', error);
    }
    return null;
  }
} 