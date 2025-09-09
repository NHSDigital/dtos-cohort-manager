import type { Metadata } from "next";
import Header from "@/app/components/header";
import Footer from "@/app/components/footer";
import "./globals.scss";

export const metadata: Metadata = {
  metadataBase: new URL("https://cohort.screening.nhs.uk"),
  title: `${process.env.SERVICE_NAME} - NHS`,
  description:
    "For use with the NHS breast screening programme. View a summary of outstanding exceptions occurring in participant data and access information to raise a service request for the data to be corrected.",
  openGraph: {
    title: `${process.env.SERVICE_NAME} - NHS`,
    description:
      "For use with the NHS breast screening programme. View a summary of outstanding exceptions occurring in participant data and access information to raise a service request for the data to be corrected.",
    url: "/",
    siteName: `${process.env.SERVICE_NAME} - NHS`,
    images: [
      {
        url: "/assets/logos/open-graph.png",
        width: 1200,
        height: 630,
        alt: `${process.env.SERVICE_NAME} - NHS`,
      },
    ],
    locale: "en_GB",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const serviceName = process.env.SERVICE_NAME;
  return (
    <html lang="en">
      <body>
        <Header serviceName={serviceName} />
        <div className="nhsuk-width-container">{children}</div>
        <Footer />
      </body>
    </html>
  );
}
