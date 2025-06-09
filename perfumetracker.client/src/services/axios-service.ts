import { getPerfumeTrackerApiAddress, getR2ApiAddress } from "./conf-service";
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

let cachedApiUrl: string | null = null;
const initializeApiUrl = async () => {
  if (!cachedApiUrl) {
    const apiUrl = await getPerfumeTrackerApiAddress();
    if (!apiUrl) throw new Error('API address not configured');
    cachedApiUrl = apiUrl;
  }
  return cachedApiUrl;
};

initializeApiUrl().catch(error => {
  console.error('Failed to initialize API URL:', error);
});


let cachedR2ApiUrl: string | null = null;
const initializeR2ApiUrl = async () => {
  if (!cachedR2ApiUrl) {
    const r2apiUrl = await getR2ApiAddress();
    if (!r2apiUrl) throw new Error('API address not configured');
    cachedR2ApiUrl = r2apiUrl;
  }
  return cachedApiUrl;
};

initializeR2ApiUrl().catch(error => {
  console.error('Failed to initialize R2 API URL:', error);
});

export async function getR2ApiUrl(): Promise<string> {
  const apiUrl = await initializeR2ApiUrl();
  if (!apiUrl) throw new Error('R2 API address not configured');
  return apiUrl;
}

export async function get<T>(url: string) {
  const apiUrl = await initializeApiUrl();
  return api.get<T>(apiUrl + url);
}

export async function post<T>(url: string, data: unknown) {
  const apiUrl = await initializeApiUrl();
  return api.post<T>(`${apiUrl}${url}`, data);
}

export async function put<T>(url: string, data: unknown) {
  const apiUrl = await initializeApiUrl();
  return api.put<T>(`${apiUrl}${url}`, data);
}

export async function del<T>(url: string) {
  const apiUrl = await initializeApiUrl();
  return api.delete<T>(`${apiUrl}${url}`);
}

const api = axios.create({
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
  const apiUrl = await initializeApiUrl();
  const response = await api.post<LoginResponse>(`${apiUrl}/identity/account/login`, {
    email,
    password,
  });
  return response.data;
}

export async function loginDemo(): Promise<LoginResponse> {
  const apiUrl = await initializeApiUrl();
  const response = await api.post<LoginResponse>(`${apiUrl}/identity/account/login/demo`, {});
  return response.data;
}

export async function logout(): Promise<void> {
  try {
    const apiUrl = await initializeApiUrl();
    await api.post(`${apiUrl}/identity/account/logout`);
  } catch (error) {
    console.error('Logout error:', error);
  }
}

export async function getCurrentUser(): Promise<UserResponse | null> {
  try {
    const apiUrl = await initializeApiUrl();
    const response = await api.get<UserResponse>(`${apiUrl}/identity/account/me`);
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

export async function register(email: string, password: string, userName: string): Promise<LoginResponse> {
  const apiUrl = await initializeApiUrl();
  const response = await api.post<LoginResponse>(`${apiUrl}/identity/account/register`, {
    email,
    password,
    userName
  });
  return response.data;
} 