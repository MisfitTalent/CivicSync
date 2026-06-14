'use client';

import { ConfigProvider } from 'antd';
import { BrowserRouter } from 'react-router-dom';
import { AppProviders } from './providers';
import AppRoutes from './routes';

const App = () => (
  <ConfigProvider
    theme={{
      token: {
        colorPrimary: '#ff8a1d',
        colorSuccess: '#6fbf16',
        colorWarning: '#ffd84d',
        borderRadius: 12,
        fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
      },
    }}
  >
    <BrowserRouter>
      <AppProviders>
        <AppRoutes />
      </AppProviders>
    </BrowserRouter>
  </ConfigProvider>
);

export default App;

