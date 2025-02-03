"use client";

import { signOut } from "next-auth/react";

interface SignOutProps {
  className?: string;
}

export default function SignOut({ className }: SignOutProps) {
  const handleSignOut = async (event: React.MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    await signOut({
      callbackUrl: "/",
    });
  };

  return (
    <>
      <a href="" className={className} onClick={handleSignOut}>
        Sign out
      </a>
    </>
  );
}
