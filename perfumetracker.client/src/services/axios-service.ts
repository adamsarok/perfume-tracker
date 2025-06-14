import { getPerfumeTrackerApiAddress } from "./conf-service";
import axios, { AxiosRequestConfig } from "axios";


let cachedApiUrl: string | null = null;
const initializeApiUrl = async () => {
  if (!cachedApiUrl) {
    const apiUrl = await getPerfumeTrackerApiAddress();
    if (!apiUrl) throw new Error('API address not configured');
    cachedApiUrl = apiUrl;
  }
  return cachedApiUrl;
};

export async function get<T>(url: string) {
  const apiUrl = await initializeApiUrl();
  return api.get<T>(apiUrl + url);
}

export async function post<T>(url: string, data: unknown) {
  const apiUrl = await initializeApiUrl();
  return api.post<T>(`${apiUrl}${url}`, data);
}

export async function put<T>(url: string, data: unknown, config: AxiosRequestConfig = {}) {
  const apiUrl = await initializeApiUrl();
  return api.put<T>(`${apiUrl}${url}`, data, config);
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

/*
  TODO: centralized error handling
    } catch (error) {
      if (axios.isAxiosError(error)) {
        console.error('Error checking current user:', error.response?.status, error.response?.data);
      } else {
        console.error('Error checking current user:', error);
      }
      return null;
    }

*/