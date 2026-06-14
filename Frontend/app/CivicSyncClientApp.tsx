'use client';

import dynamic from 'next/dynamic';

const CivicSyncApp = dynamic(() => import('../src/App'), {
  ssr: false,
});

const CivicSyncClientApp = () => <CivicSyncApp />;

export default CivicSyncClientApp;
