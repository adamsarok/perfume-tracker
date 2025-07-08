import { get, post } from "./axios-service";

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

export interface UserConfiguration {
    inviteOnlyRegistration: boolean
}

export interface UserXPResponse {
    xp: number;
    xpLastLevel: number;
    xpNextLevel: number;
    level: number;
}

export async function loginUser(email: string, password: string): Promise<LoginResponse> {
    const response = await post<LoginResponse>(`/identity/account/login`, {
        email,
        password,
    });
    return response.data;
}

export async function loginDemoUser(): Promise<LoginResponse> {
    const response = await post<LoginResponse>(`/identity/account/login/demo`, {});
    return response.data;
}

export async function logoutUser(): Promise<void> {
    await post(`/identity/account/logout`, null);
}

export async function getCurrentUser(): Promise<UserResponse | null> {
    const response = await get<UserResponse>(`/identity/account/me`);
    return response.data;
}

export async function getUserConfiguration(): Promise<UserConfiguration | null> {
    const response = await get<UserConfiguration>(`/identity/configuration`);
    return response.data;
}

export async function registerUser(email: string, password: string, userName: string, inviteCode: string | null): Promise<LoginResponse> {
    const response = await post<LoginResponse>(`/identity/account/register`, {
        email,
        password,
        userName,
        inviteCode
    });
    return response.data;
}

export async function getUserXP(): Promise<UserXPResponse> {
    const response = await get<UserXPResponse>(`/users/xp`);
    return response.data;
} 