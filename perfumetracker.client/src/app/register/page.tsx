"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { getUserConfiguration, registerUser } from "@/services/user-service";

export default function RegisterPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [passwordAgain, setPasswordAgain] = useState("");
  const [userName, setUserName] = useState("");
  const [inviteCode, setInviteCode] = useState("");
  const [error, setError] = useState("");
  const [passwordsMatch, setPasswordsMatch] = useState(true);
  const [inviteOnlyRegistration, setInviteOnlyRegistration] = useState<boolean | undefined>(true);
  const router = useRouter();

  useEffect(() => {
    const fetchConfig = async () => {
      const config = await getUserConfiguration();
      setInviteOnlyRegistration(config?.inviteOnlyRegistration);
    };
    fetchConfig();
  }, []);

  useEffect(() => {
    if (password && passwordAgain) {
      setPasswordsMatch(password === passwordAgain);
    }
  }, [password, passwordAgain]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!passwordsMatch) {
      setError("Passwords do not match");
      return;
    }

    try {
      await registerUser(email.trim(), password, userName.trim(), inviteCode.trim());
      router.push("/"); // Redirect to home page after successful registration
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Registration failed.");
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Register new account
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit} autoComplete="on">
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <Label htmlFor="email-address" className="sr-only">
                Email address
              </Label>
              <Input
                id="email-address"
                name="email"
                type="email"
                autoComplete="email"
                required
                placeholder="Email address"
                value={email}
                className="mt-2"
                onChange={(e) => setEmail(e.target.value.trimStart())}
              />
            </div>
            <div>
              <Label htmlFor="userName" className="sr-only">
                User Name
              </Label>
              <Input
                id="userName"
                name="userName"
                type="text"
                autoComplete="username"
                required
                placeholder="User name"
                value={userName}
                className="mt-2"
                onChange={(e) => setUserName(e.target.value.trimStart())}
              />
            </div>
            {inviteOnlyRegistration && <div>
              <Label htmlFor="inviteCode" className="sr-only">
                Invite Code
              </Label>
              <Input
                id="inviteCode"
                name="inviteCode"
                type="text"
                autoComplete="off"
                required
                placeholder="Invite Code"
                value={inviteCode}
                className="mt-2"
                onChange={(e) => setInviteCode(e.target.value.trimStart())}
              />
            </div>}
            <div>
              <Label htmlFor="password" className="sr-only">
                Password
              </Label>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                required
                placeholder="Password"
                value={password}
                className="mt-2"
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="passwordAgain" className="sr-only">
                Confirm Password
              </Label>
              <Input
                id="passwordAgain"
                name="passwordAgain"
                type="password"
                autoComplete="new-password"
                required
                placeholder="Confirm Password"
                value={passwordAgain}
                onChange={(e) => setPasswordAgain(e.target.value)}
                className={cn(
                  "mt-2",
                  !passwordsMatch && password && passwordAgain && "border-red-500 focus:ring-red-500 focus:border-red-500"
                )}
              />
            </div>
          </div>

          {error && (
            <div className="text-red-500 text-sm text-center">{error}</div>
          )}

          <div>
            <Button
              type="submit"
              disabled={!passwordsMatch || !password || !passwordAgain}
              className="w-full"
            >
              Register
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
} 