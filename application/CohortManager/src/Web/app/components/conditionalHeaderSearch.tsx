"use client";
import { usePathname } from "next/navigation";

interface ConditionalHeaderSearchProps {
  readonly children: React.ReactNode;
}

export function ConditionalHeaderSearch({
  children,
}: ConditionalHeaderSearchProps) {
  const pathname = usePathname();
  const isNoResultsPage = pathname === "/exceptions/noResults";

  // Don't render the header search if we're on the error page
  if (isNoResultsPage) {
    return null;
  }

  return <>{children}</>;
}
