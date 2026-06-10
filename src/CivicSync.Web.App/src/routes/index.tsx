import { Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from '../layouts/appLayout';
import AdminPage from '../pages/admin/AdminPage';
import CitizenPage from '../pages/citizen/CitizenPage';
import CitizenLedgerPage from '../pages/citizen/CitizenLedgerPage';
import RequestUpdatePage from '../pages/citizen/RequestUpdatePage';
import DepartmentPage from '../pages/department/DepartmentPage';
import DepartmentLedgerPage from '../pages/department/DepartmentLedgerPage';
import DepartmentRequestsPage from '../pages/department/DepartmentRequestsPage';
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
          <Route path="citizen/request-update" element={<RequestUpdatePage />} />
          <Route path="citizen/ledger" element={<CitizenLedgerPage />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['HomeAffairsOfficer']} />}>
          <Route path="home-affairs" element={<DepartmentPage departmentCode={1} title="Home Affairs" responsibility="Owns identity records, citizen profile checks, and final identity-related approvals." />} />
          <Route path="home-affairs/requests" element={<DepartmentRequestsPage departmentCode={1} title="Home Affairs" />} />
          <Route path="home-affairs/ledger" element={<DepartmentLedgerPage departmentCode={1} title="Home Affairs" />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['SarsOfficer']} />}>
          <Route path="sars" element={<DepartmentPage departmentCode={2} title="SARS" responsibility="Reviews tax-impacting record changes and receives approved citizen updates from peers." />} />
          <Route path="sars/requests" element={<DepartmentRequestsPage departmentCode={2} title="SARS" />} />
          <Route path="sars/ledger" element={<DepartmentLedgerPage departmentCode={2} title="SARS" />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['MunicipalityOfficer']} />}>
          <Route path="municipality" element={<DepartmentPage departmentCode={3} title="Municipality" responsibility="Reviews residence/contact changes and applies approved ledger updates to the municipal node." />} />
          <Route path="municipality/requests" element={<DepartmentRequestsPage departmentCode={3} title="Municipality" />} />
          <Route path="municipality/ledger" element={<DepartmentLedgerPage departmentCode={3} title="Municipality" />} />
        </Route>
        <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
          <Route path="admin" element={<AdminPage />} />
        </Route>
      </Route>
    </Route>
  </Routes>
);

export default AppRoutes;
