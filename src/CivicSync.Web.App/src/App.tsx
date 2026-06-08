import { ConfigProvider } from 'antd';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './providers/authProvider';
import { CivicSyncProvider } from './providers/civicSyncProvider';
import AppRoutes from './routes';
import './styles/global.css';

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
      <AuthProvider>
        <CivicSyncProvider>
          <AppRoutes />
        </CivicSyncProvider>
      </AuthProvider>
    </BrowserRouter>
  </ConfigProvider>
);

export default App;
