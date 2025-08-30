import { getPerfumeTrackerApiAddress } from "./conf-service";
import axios, { AxiosRequestConfig } from "axios";

let cachedApiUrl: string | null = null;
export const initializeApiUrl = async () => {
  if (!cachedApiUrl) {
    const apiUrl = await getPerfumeTrackerApiAddress();
    if (!apiUrl) throw new Error("API address not configured");
    cachedApiUrl = apiUrl;
  }
  return cachedApiUrl;
};

export async function get<T>(url: string): Promise<AxiosResult<T>> {
  try {
    const apiUrl = await initializeApiUrl();
    const result = await api.get<T>(apiUrl + url);
    return { ok: true, data: result.data };
  } catch (error) {
    return getErrorResult(error);
  }
}

export async function post<T>(
  url: string,
  data: unknown
): Promise<AxiosResult<T>> {
  try {
    const apiUrl = await initializeApiUrl();
    const result = await api.post<T>(`${apiUrl}${url}`, data);
    return { ok: true, data: result.data };
  } catch (error) {
    return getErrorResult(error);
  }
}

export async function put<T>(
  url: string,
  data: unknown,
  config: AxiosRequestConfig = {}
): Promise<AxiosResult<T>> {
  try {
    const apiUrl = await initializeApiUrl();
    const result = await api.put<T>(`${apiUrl}${url}`, data, config);
    return { ok: true, data: result.data };
  } catch (error) {
    return getErrorResult(error);
  }
}

export async function del<T>(url: string): Promise<AxiosResult<T>> {
  try {
    const apiUrl = await initializeApiUrl();
    const result = await api.delete<T>(`${apiUrl}${url}`);
    return { ok: true, data: result.data };
  } catch (error) {
    return getErrorResult(error);
  }
}

export interface AxiosResult<T> {
  error?: string;
  status?: number;
  ok: boolean;
  data?: T;
}

function getErrorResult(error: unknown): AxiosResult<never> {
  if (axios.isAxiosError(error)) {
    return {
      ok: false,
      status: error.response?.status,
      error: error.response?.data || error.message,
    };
  }
  return {
    ok: false,
    error: error instanceof Error ? error.message : String(error),
  };
}

const api = axios.create({
  withCredentials: true,
  xsrfCookieName: "XSRF-TOKEN",
  xsrfHeaderName: "X-XSRF-TOKEN",
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      console.error("Authentication error:", error);
    }
    return Promise.reject(error);
  }
);
