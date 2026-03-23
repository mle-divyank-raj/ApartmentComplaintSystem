/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  transpilePackages: ["@acls/shared-types", "@acls/api-contracts", "@acls/sdk"],
};

export default nextConfig;
