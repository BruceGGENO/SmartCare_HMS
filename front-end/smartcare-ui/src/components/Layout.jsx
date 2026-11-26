import React from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../services/AuthContext';
import { 
  Calendar, 
  CreditCard, 
  PieChart, 
  LogOut as DischargeIcon, 
  Clock, 
  FileText, 
  Shield, 
  LogOut, 
  Activity, 
  UserCheck,
  BedDouble,
  Pill,
  Microscope,
  TrendingUp,
  Smartphone,
  Send
} from 'lucide-react';

export default function Layout() {
  const { user, logout, hasPermission } = useAuth();
  const navigate = useNavigate();

  const navItems = [
    { name: 'Patients & EHR', path: '/patients', icon: UserCheck, perm: 'patients.read' },
    { name: 'Appointments', path: '/appointments', icon: Calendar, perm: 'appointments.read' },
    { name: 'Bed Orchestrator', path: '/beds', icon: BedDouble, perm: 'beds.read' },
    { name: 'Waiting Room', path: '/waiting-room', icon: Clock, perm: 'waitingroom.read' },
    { name: 'Discharge Tracker', path: '/discharge', icon: DischargeIcon, perm: 'discharge.read' },
    { name: 'Pharmacy & Stock', path: '/pharmacy', icon: Pill, perm: 'pharmacy.read' },
    { name: 'Laboratory & Rad', path: '/lab', icon: Microscope, perm: 'lab.read' },
    { name: 'Billing & Invoices', path: '/billing', icon: CreditCard, perm: 'billing.read' },
    { name: 'Revenue Accounting', path: '/revenue', icon: PieChart, perm: 'revenue.read' },
    { name: 'Occupancy Forecast', path: '/occupancy', icon: TrendingUp, perm: 'occupancy.read' },
    { name: 'Patient Portal', path: '/portal', icon: Smartphone, perm: 'portal.appointments.read' },
    { name: 'Documents', path: '/documents', icon: FileText, perm: 'documents.read' },
    { name: 'Audit Trail', path: '/audit', icon: Shield, perm: 'audit.read' },
    { name: 'Outbox Stubs', path: '/outbox', icon: Send, perm: 'outbox.read' },
  ];

  const isPrivileged = user?.roles?.includes('Super Admin') || user?.roles?.includes('Hospital Admin');
  const visibleNav = navItems.filter((item) => isPrivileged || hasPermission(item.perm) || (item.path === '/portal' && user?.roles?.includes('Patient')));

  return (
    <div className="flex h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className="w-64 bg-slate-900 border-r border-slate-800 flex flex-col justify-between shrink-0">
        <div className="flex flex-col h-full overflow-hidden">
          <div className="h-16 flex items-center gap-3 px-6 border-b border-slate-800 shrink-0">
            <div className="w-8 h-8 rounded-lg bg-brand-600 text-white flex items-center justify-center font-bold shadow-xs">
              <Activity className="w-5 h-5" />
            </div>
            <div>
              <span className="font-bold text-white text-base tracking-tight">SmartCare HMS</span>
              <span className="block text-[10px] text-brand-400 font-medium">Enterprise Health Suite</span>
            </div>
          </div>

          {/* User profile card */}
          <div className="p-3 mx-3 my-2.5 bg-slate-800/60 rounded-xl border border-slate-700/50 shrink-0">
            <div className="flex items-center gap-2.5">
              <div className="w-8 h-8 rounded-full bg-brand-500/20 text-brand-400 font-semibold flex items-center justify-center text-xs border border-brand-500/30">
                {user?.displayName ? user.displayName.charAt(0) : 'U'}
              </div>
              <div className="overflow-hidden">
                <p className="text-xs font-semibold text-white truncate">{user?.displayName}</p>
                <p className="text-[10px] text-slate-400 truncate capitalize">{user?.roles?.join(', ') || 'Staff'}</p>
              </div>
            </div>
            <div className="mt-2 pt-2 border-t border-slate-700/50 flex items-center justify-between">
              <span className="text-[9px] bg-brand-900/60 text-brand-300 px-1.5 py-0.5 rounded border border-brand-700/40 font-mono">
                {user?.permissions?.length || 0} permissions
              </span>
              <span className="text-[9px] text-emerald-400 font-mono">● Active</span>
            </div>
          </div>

          {/* Navigation Links (Scrollable) */}
          <nav className="flex-1 overflow-y-auto px-3 space-y-0.5 py-1">
            {visibleNav.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  className={({ isActive }) =>
                    `flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium transition ${
                      isActive
                        ? 'bg-brand-600 text-white shadow-xs font-semibold'
                        : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
                    }`
                  }
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{item.name}</span>
                </NavLink>
              );
            })}
          </nav>
        </div>

        {/* Logout bottom */}
        <div className="p-3 border-t border-slate-800 shrink-0">
          <button
            onClick={logout}
            className="w-full flex items-center gap-2 px-3 py-2 text-xs text-red-400 hover:text-red-300 hover:bg-red-500/10 rounded-xl transition font-medium"
          >
            <LogOut className="w-4 h-4" />
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden">
        <header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-8 shrink-0">
          <div className="flex items-center gap-3">
            <h2 className="text-base font-bold text-slate-800">Hospital Workstation</h2>
            <span className="text-xs bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full font-mono">
              Role: {user?.roles?.[0] || 'Clinical'}
            </span>
          </div>
          <div className="flex items-center gap-3">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
              Live Connected (Web API :7051)
            </span>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
