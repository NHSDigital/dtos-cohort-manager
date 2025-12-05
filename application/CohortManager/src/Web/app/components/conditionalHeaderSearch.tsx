interface ConditionalHeaderSearchProps {
  readonly children: React.ReactNode;
  readonly pathname: string;
}

export function ConditionalHeaderSearch({
  children,
  pathname,
}: ConditionalHeaderSearchProps) {
  const isNoResultsPage = pathname === "/exceptions/noResults";

  // Don't render the header search if we're on the no results page
  if (isNoResultsPage) {
    return null;
  }

  return <>{children}</>;
}
