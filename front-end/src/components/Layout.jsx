import React from 'react';
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
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
  Send,
  Home,
  ChevronRight,
  ExternalLink,
  HeartPulse
} from 'lucide-react';

export default function Layout() {
  const { user, logout, hasPermission } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const navCategories = [
    {
      label: 'Clinical & Patient Care',
      items: [
        { name: 'Patients & EHR', path: '/patients', icon: UserCheck, perm: 'patients.read', color: 'text-blue-600 bg-blue-50' },
        { name: 'Appointments', path: '/appointments', icon: Calendar, perm: 'appointments.read', color: 'text-indigo-600 bg-indigo-50' },
        { name: 'Waiting Room Queue', path: '/waiting-room', icon: Clock, perm: 'waitingroom.read', color: 'text-amber-600 bg-amber-50' },
        { name: 'Bed Orchestrator', path: '/beds', icon: BedDouble, perm: 'beds.read', color: 'text-teal-600 bg-teal-50' },
        { name: 'Discharge Tracker', path: '/discharge', icon: DischargeIcon, perm: 'discharge.read', color: 'text-rose-600 bg-rose-50' },
      ]
    },
    {
      label: 'Diagnostic & Pharmacy',
      items: [
        { name: 'Pharmacy & Stock', path: '/pharmacy', icon: Pill, perm: 'pharmacy.read', color: 'text-emerald-600 bg-emerald-50' },
        { name: 'Laboratory & Radiology', path: '/lab', icon: Microscope, perm: 'lab.read', color: 'text-purple-600 bg-purple-50' },
      ]
    },
    {
      label: 'Financial & Management',
      items: [
        { name: 'Billing & Invoices', path: '/billing', icon: CreditCard, perm: 'billing.read', color: 'text-emerald-600 bg-emerald-50' },
        { name: 'Prepaid Revenue', path: '/revenue', icon: PieChart, perm: 'revenue.read', color: 'text-sky-600 bg-sky-50' },
        { name: 'Occupancy Forecast', path: '/occupancy', icon: TrendingUp, perm: 'occupancy.read', color: 'text-violet-600 bg-violet-50' },
        { name: 'Patient Micro-Portal', path: '/portal', icon: Smartphone, perm: 'portal.appointments.read', color: 'text-brand-600 bg-brand-50' },
        { name: 'Document Vault', path: '/documents', icon: FileText, perm: 'documents.read', color: 'text-slate-600 bg-slate-100' },
        { name: 'Security Audit Trail', path: '/audit', icon: Shield, perm: 'audit.read', color: 'text-orange-600 bg-orange-50' },
        { name: 'SMS & Outbox Stubs', path: '/outbox', icon: Send, perm: 'outbox.read', color: 'text-cyan-600 bg-cyan-50' },
      ]
    }
  ];

  const isPrivileged = user?.roles?.includes('Super Admin') || user?.roles?.includes('Hospital Admin');

  const currentPageTitle = navCategories
    .flatMap((c) => c.items)
    .find((i) => i.path === location.pathname)?.name || 'Clinical Workstation';

  return (
    <div className="flex h-screen bg-gradient-to-br from-slate-50 via-brand-50/30 to-slate-50 overflow-hidden font-sans">
      {/* Light, Airy Dribbble-Inspired Medical Sidebar */}
      <aside className="w-72 bg-white border-r border-slate-200/80 flex flex-col justify-between shrink-0 shadow-soft">
        <div className="flex flex-col h-full overflow-hidden">
          {/* Brand Header */}
          <div className="h-20 flex items-center justify-between px-6 border-b border-slate-100 shrink-0">
            <div className="flex items-center gap-3 cursor-pointer" onClick={() => navigate('/')}>
              <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-brand-700 to-brand-500 text-white flex items-center justify-center font-bold shadow-md shadow-brand-500/25">
                <Activity className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center gap-1">
                  <span className="font-extrabold text-slate-900 text-base tracking-tight">SmartCare</span>
                  <span className="font-extrabold text-brand-600 text-base">HMS</span>
                </div>
                <span className="block text-[10px] text-slate-400 font-semibold tracking-wider uppercase">
                  Care. Compassion. Excellence.
                </span>
              </div>
            </div>
          </div>

          {/* User Profile Card */}
          <div className="p-4 mx-4 my-4 bg-gradient-to-br from-brand-50/80 to-slate-50 rounded-3xl border border-brand-100/80 shrink-0 shadow-xs">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-brand-600 to-brand-400 text-white font-extrabold flex items-center justify-center text-sm shadow-sm">
                {user?.displayName ? user.displayName.charAt(0) : 'U'}
              </div>
              <div className="overflow-hidden">
                <p className="text-sm font-bold text-slate-900 truncate">{user?.displayName || 'Authorized User'}</p>
                <p className="text-[11px] text-brand-700 font-semibold truncate capitalize">
                  {user?.roles?.join(', ') || 'Staff Member'}
                </p>
              </div>
            </div>
            <div className="mt-3 pt-2.5 border-t border-brand-100 flex items-center justify-between text-[10px]">
              <span className="bg-white text-brand-700 px-2.5 py-0.5 rounded-full border border-brand-200 font-bold">
                {user?.permissions?.length || 0} Permissions
              </span>
              <span className="text-emerald-700 font-bold flex items-center gap-1.5 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
                Online
              </span>
            </div>
          </div>

          {/* Navigation Links */}
          <nav className="flex-1 overflow-y-auto px-4 space-y-5 py-1">
            {navCategories.map((cat, idx) => {
              const visibleItems = cat.items.filter(
                (item) => isPrivileged || hasPermission(item.perm) || (item.path === '/portal' && user?.roles?.includes('Patient'))
              );

              if (visibleItems.length === 0) return null;

              return (
                <div key={idx} className="space-y-1.5">
                  <span className="text-[10px] font-extrabold uppercase tracking-widest text-slate-400 px-3">
                    {cat.label}
                  </span>
                  <div className="space-y-1">
                    {visibleItems.map((item) => {
                      const Icon = item.icon;
                      return (
                        <NavLink
                          key={item.path}
                          to={item.path}
                          className={({ isActive }) =>
                            `flex items-center justify-between px-3 py-2.5 rounded-2xl text-xs font-bold transition group ${
                              isActive
                                ? 'bg-brand-600 text-white shadow-md shadow-brand-600/25'
                                : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100/80'
                            }`
                          }
                        >
                          {({ isActive }) => (
                            <>
                              <div className="flex items-center gap-3 truncate">
                                <div className={`w-7 h-7 rounded-xl flex items-center justify-center shrink-0 transition ${
                                  isActive ? 'bg-white/20 text-white' : item.color
                                }`}>
                                  <Icon className="w-4 h-4" />
                                </div>
                                <span className="truncate">{item.name}</span>
                              </div>
                              <ChevronRight className={`w-3.5 h-3.5 shrink-0 transition ${
                                isActive ? 'text-white/80' : 'text-slate-300 group-hover:text-slate-500'
                              }`} />
                            </>
                          )}
                        </NavLink>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </nav>
        </div>

        {/* Bottom Actions */}
        <div className="p-4 border-t border-slate-100 shrink-0 space-y-1.5 bg-slate-50/50">
          <button
            onClick={() => navigate('/')}
            className="w-full flex items-center gap-2.5 px-3.5 py-2.5 text-xs text-slate-700 hover:text-brand-700 hover:bg-white rounded-2xl border border-transparent hover:border-slate-200 transition font-bold"
          >
            <Home className="w-4 h-4 text-brand-600" />
            <span>Public Hospital Site</span>
          </button>
          <button
            onClick={logout}
            className="w-full flex items-center gap-2.5 px-3.5 py-2.5 text-xs text-rose-600 hover:text-rose-700 hover:bg-rose-50 rounded-2xl transition font-bold"
          >
            <LogOut className="w-4 h-4" />
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Clean White Top Header */}
        <header className="h-20 bg-white/90 backdrop-blur-md border-b border-slate-200/80 flex items-center justify-between px-8 shrink-0">
          <div className="flex items-center gap-3.5">
            <div className="w-9 h-9 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center">
              <HeartPulse className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base font-extrabold text-slate-900 tracking-tight">{currentPageTitle}</h2>
              <p className="text-[11px] text-slate-500 font-medium">
                Department: <span className="text-brand-700 font-bold">{user?.roles?.[0] || 'Clinical Staff'}</span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/')}
              className="text-xs font-bold text-brand-700 hover:text-brand-800 flex items-center gap-1.5 bg-brand-50 hover:bg-brand-100 px-4 py-2 rounded-xl border border-brand-200/80 transition"
            >
              <span>Public Portal</span>
              <ExternalLink className="w-3.5 h-3.5" />
            </button>
            <span className="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
              API Connected (:7051)
            </span>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto p-6 sm:p-8">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
