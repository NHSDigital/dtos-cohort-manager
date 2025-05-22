/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  sassOptions: {
    quietDeps: true,
    includePaths: ["./node_modules/nhsuk-frontend"],
  },
};

export default nextConfig;
