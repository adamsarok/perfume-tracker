"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import { loginDemoUser, loginUser } from "@/services/user-service";
import { initializeApiUrl } from "@/services/axios-service";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    const result = await loginUser(email.trim(), password);
    if (result.error || !result.data) {
      setError("Login failed: " + (result.error ?? "unknown error"));
    } else {
      router.push("/"); // Redirect to home page after successful login
    }
  };

  const handleLoginDemo = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    const result = await loginDemoUser();
    if (result.error || !result.data) {
      setError("Login failed: " + (result.error ?? "unknown error"));
    } else {
      router.push("/"); // Redirect to home page after successful login
    }
  };

  const handleGithubLogin = async () => {
    const apiUrl = await initializeApiUrl();
    if (!apiUrl) {
      console.error("API URL not configured");
      return;
    }
    window.location.href = `${apiUrl}/auth/github/login`;
  };



  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to your account
          </h2>
        </div>
        <Button variant="outline" onClick={handleGithubLogin}>
          Continue with GitHub
        </Button>

        <form className="mt-8 space-y-6" onSubmit={handleLogin}>
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <Label htmlFor="email-address" className="sr-only">
                Email address
              </Label>
              <Input
                id="email-address"
                name="email"
                type="text"
                autoComplete="username"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Email or UserName"
                value={email}
                onChange={(e) => setEmail(e.target.value.trimStart())}
              />
            </div>
            <div>
              <Label htmlFor="password" className="sr-only">
                Password
              </Label>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                required
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 focus:z-10 sm:text-sm"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
          </div>

          {error && (
            <div className="text-red-500 text-sm text-center">{error}</div>
          )}

          <div className="flex justify-evenly">
            <Button type="submit">Sign in</Button>
            <Link href="/register" passHref>
              <Button variant="outline" type="button">
                Register
              </Button>
            </Link>
            <Button variant="secondary" type="button" onClick={handleLoginDemo}>
              Demo
            </Button>
          </div>
          <div></div>
        </form>
      </div>
    </div>
  );
}
