import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './services/AuthContext';
import Layout from './components/Layout';
import Home from './pages/Home';
import Login from './pages/Login';
import Patients from './pages/Patients';
import Appointments from './pages/Appointments';
import Beds from './pages/Beds';
import WaitingRoom from './pages/WaitingRoom';
import Discharge from './pages/Discharge';
import Pharmacy from './pages/Pharmacy';
import Lab from './pages/Lab';
import Billing from './pages/Billing';
import Revenue from './pages/Revenue';
import Occupancy from './pages/Occupancy';
import Portal from './pages/Portal';
import Documents from './pages/Documents';
import Audit from './pages/Audit';
import Outbox from './pages/Outbox';

const ProtectedRoute = ({ children }) => {
  const { user } = useAuth();
  if (!user) {
    return <Navigate to="/login" replace />;
  }
  return children;
};

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public Hospital Portal (Inspired by Dribbble Design) */}
          <Route path="/" element={<Home />} />
          
          {/* Direct Staff Sign-In */}
          <Route path="/login" element={<Login />} />
          
          {/* Protected Clinical & Administrative Workstation */}
          <Route
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route path="/dashboard" element={<Navigate to="/patients" replace />} />
            <Route path="/patients" element={<Patients />} />
            <Route path="/appointments" element={<Appointments />} />
            <Route path="/beds" element={<Beds />} />
            <Route path="/waiting-room" element={<WaitingRoom />} />
            <Route path="/discharge" element={<Discharge />} />
            <Route path="/pharmacy" element={<Pharmacy />} />
            <Route path="/lab" element={<Lab />} />
            <Route path="/billing" element={<Billing />} />
            <Route path="/revenue" element={<Revenue />} />
            <Route path="/occupancy" element={<Occupancy />} />
            <Route path="/portal" element={<Portal />} />
            <Route path="/documents" element={<Documents />} />
            <Route path="/audit" element={<Audit />} />
            <Route path="/outbox" element={<Outbox />} />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
