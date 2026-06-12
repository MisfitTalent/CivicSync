import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthState } from '../providers/authProvider';
import type { UserRole } from '../providers/authProvider/context';

const ProtectedRoute = ({ allowedRoles }: { allowedRoles?: UserRole[] }) => {
  const { currentUser } = useAuthState();
  const location = useLocation();

  if (!currentUser) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (allowedRoles && !allowedRoles.includes(currentUser.role)) {
    return <Navigate to={currentUser.workspacePath} replace />;
  }

  return <Outlet />;
};

export default ProtectedRoute;
