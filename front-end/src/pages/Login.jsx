import React, { useState } from 'react';
import { useAuth } from '../services/AuthContext';
import { useNavigate } from 'react-router-dom';
import { Activity, Lock, User, ArrowLeft, ShieldCheck, ChevronRight } from 'lucide-react';

export default function Login() {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { login, loading } = useAuth();
  const navigate = useNavigate();

  const getHomeRoute = (userProfile) => {
    const roles = userProfile?.roles || [];
    const uname = userProfile?.userName?.toLowerCase() || '';

    if (roles.includes('Lab Technician') || uname === 'lab') return '/lab';
    if (roles.includes('Pharmacist') || uname === 'pharmacist') return '/pharmacy';
    if (roles.includes('Patient') || uname === 'patient') return '/portal';
    if (roles.includes('Billing Staff') || uname === 'billing') return '/billing';
    if (roles.includes('Nurse') || uname === 'nurse') return '/waiting-room';
    if (roles.includes('Receptionist') || uname === 'receptionist') return '/appointments';
    return '/patients';
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    const res = await login(userName, password);
    if (res.success) {
      navigate(getHomeRoute(res.user));
    } else {
      setError(res.message);
    }
  };

  const handleQuickDemo = (demoUser) => {
    setUserName(demoUser);
    setPassword('Demo@12345');
    setError('');
  };

  const demoRoles = [
    { id: 'admin', label: 'Hospital Admin' },
    { id: 'doctor', label: 'Doctor' },
    { id: 'nurse', label: 'Nurse' },
    { id: 'receptionist', label: 'Receptionist' },
    { id: 'lab', label: 'Lab Tech' },
    { id: 'pharmacist', label: 'Pharmacist' },
    { id: 'billing', label: 'Billing Staff' },
    { id: 'patient', label: 'Patient' },
    { id: 'superadmin', label: 'Super Admin' }
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-brand-50 via-slate-50 to-brand-100/40 flex flex-col items-center justify-center p-4 selection:bg-brand-600 selection:text-white">
      {/* Return to Public Portal link */}
      <div className="max-w-md w-full mb-4">
        <button
          onClick={() => navigate('/')}
          className="inline-flex items-center gap-2 text-xs font-bold text-slate-600 hover:text-brand-600 bg-white/80 hover:bg-white px-3.5 py-2 rounded-xl border border-slate-200 transition shadow-xs"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Back to Hospital Website</span>
        </button>
      </div>

      <div className="max-w-md w-full bg-white rounded-3xl shadow-card p-7 sm:p-9 border border-slate-200/80">
        <div className="text-center mb-7">
          <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-tr from-brand-700 to-brand-500 text-white mb-3 shadow-md shadow-brand-500/25">
            <Activity className="w-7 h-7" />
          </div>
          <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight">SmartCare Hospital</h1>
          <p className="text-xs text-slate-500 font-medium mt-1">Authorized Clinical & Administrative Workstation</p>
        </div>

        {error && (
          <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-2xl flex items-center gap-2 font-medium">
            <span>⚠️</span>
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1.5">Username</label>
            <div className="relative">
              <User className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                required
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                placeholder="e.g. superadmin, doctor, nurse"
                className="w-full pl-10 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent text-sm transition"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1.5">Password</label>
            <div className="relative">
              <Lock className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full pl-10 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent text-sm transition"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-3.5 bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white font-bold rounded-2xl shadow-md shadow-brand-600/30 transition disabled:opacity-50 text-sm flex items-center justify-center gap-2"
          >
            <span>{loading ? 'Authenticating...' : 'Sign In to Workstation'}</span>
            {!loading && <ChevronRight className="w-4 h-4" />}
          </button>
        </form>

        <div className="mt-8 pt-6 border-t border-slate-100">
          <div className="flex items-center justify-between mb-3">
            <span className="text-[11px] text-slate-400 uppercase tracking-wider font-bold">
              Quick Role Switcher
            </span>
            <span className="text-[10px] text-brand-600 font-mono bg-brand-50 px-2 py-0.5 rounded-full font-semibold">
              Demo@12345
            </span>
          </div>

          <div className="grid grid-cols-3 gap-2 text-xs">
            {demoRoles.map((r) => (
              <button
                key={r.id}
                type="button"
                onClick={() => handleQuickDemo(r.id)}
                className={`py-2 px-2 rounded-xl font-bold transition text-center text-[11px] truncate ${
                  userName.toLowerCase() === r.id
                    ? 'bg-brand-600 text-white shadow-xs'
                    : 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                }`}
              >
                {r.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
