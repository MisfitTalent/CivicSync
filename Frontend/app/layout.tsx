import 'antd/dist/reset.css';
import '../src/styles/global.css';
import type { Metadata } from 'next';
import type { ReactNode } from 'react';

export const metadata: Metadata = {
  title: 'CivicSync Ledger',
  description: 'Decentralized public sector ledger demo',
};

const RootLayout = ({ children }: { children: ReactNode }) => (
  <html lang="en">
    <body>{children}</body>
  </html>
);

export default RootLayout;
