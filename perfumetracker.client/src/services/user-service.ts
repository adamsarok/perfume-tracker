import { AxiosResult, get, post } from "./axios-service";

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
    xpMultiplier: number;
    streakLength: number;
}

export async function loginUser(email: string, password: string): Promise<AxiosResult<LoginResponse>> {
    return post<LoginResponse>(`/identity/account/login`, {
        email,
        password,
    });
}

export async function loginDemoUser(): Promise<AxiosResult<LoginResponse>> {
    return post<LoginResponse>(`/identity/account/login/demo`, {});
}

export async function logoutUser(): Promise<AxiosResult<unknown>> {
    return post(`/identity/account/logout`, null);
}

export async function getCurrentUser(): Promise<AxiosResult<UserResponse>> {
    return get<UserResponse>(`/identity/account/me`);
}

export async function getUserConfiguration(): Promise<AxiosResult<UserConfiguration>> {
    return get<UserConfiguration>(`/identity/configuration`);
}

export async function registerUser(email: string, password: string, userName: string, inviteCode: string | null): Promise<AxiosResult<LoginResponse>> {
    return post<LoginResponse>(`/identity/account/register`, {
        email,
        password,
        userName,
        inviteCode
    });
}

export async function getUserXP(): Promise<AxiosResult<UserXPResponse>> {
    return get<UserXPResponse>(`/users/xp`);
} 