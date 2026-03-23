import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: "#185FA5",
        "primary-foreground": "#FFFFFF",
        destructive: "#A32D2D",
        warning: "#854F0B",
        success: "#3B6D11",
        muted: "#888780",
        border: "#D3D1C7",
        background: "#F1EFE8",
        card: "#FFFFFF",
      },
    },
  },
  plugins: [],
};

export default config;
