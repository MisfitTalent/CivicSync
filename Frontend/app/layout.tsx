import 'antd/dist/reset.css';
import '../src/styles/global.css';
import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import Script from 'next/script';

export const metadata: Metadata = {
  title: 'CivicSync Ledger',
  description: 'Decentralized public sector ledger demo',
};

const RootLayout = ({ children }: { children: ReactNode }) => (
  <html lang="en">
    <body>
      <Script
        id="civicsync-runtime-config"
        strategy="beforeInteractive"
        dangerouslySetInnerHTML={{
          __html: `window.__CIVICSYNC_CONFIG__ = ${JSON.stringify({
            apiKey: process.env.NEXT_PUBLIC_CIVICSYNC_API_KEY || '',
            homeAffairsApiUrl: process.env.NEXT_PUBLIC_CIVICSYNC_HOME_AFFAIRS_API_URL || '',
            sarsApiUrl: process.env.NEXT_PUBLIC_CIVICSYNC_SARS_API_URL || '',
            municipalityApiUrl: process.env.NEXT_PUBLIC_CIVICSYNC_MUNICIPALITY_API_URL || '',
          })};`,
        }}
      />
      {children}
    </body>
  </html>
);

export default RootLayout;
