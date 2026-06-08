import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from '../layouts/appLayout';
import AdminPage from '../pages/admin/AdminPage';
import CitizenPage from '../pages/citizen/CitizenPage';
import DepartmentPage from '../pages/department/DepartmentPage';
import LoginPage from '../pages/login/LoginPage';
import ProtectedRoute from './ProtectedRoute';

const AppRoutes = () => (
  <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute />}>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<Navigate to="/citizen" replace />} />
        <Route element={<ProtectedRoute allowedRoles={['Citizen']} />}>
          <Route path="citizen" element={<CitizenPage />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['HomeAffairsOfficer']} />}>
          <Route path="home-affairs" element={<DepartmentPage departmentCode={1} title="Home Affairs" responsibility="Owns identity records, citizen profile checks, and final identity-related approvals." />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['SarsOfficer']} />}>
          <Route path="sars" element={<DepartmentPage departmentCode={2} title="SARS" responsibility="Reviews tax-impacting record changes and receives approved citizen updates from peers." />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['MunicipalityOfficer']} />}>
          <Route path="municipality" element={<DepartmentPage departmentCode={3} title="Municipality" responsibility="Reviews residence/contact changes and applies approved ledger updates to the municipal node." />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
          <Route path="admin" element={<AdminPage />} />
        </Route>
      </Route>
    </Route>
  </Routes>
);

export default AppRoutes;
