import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { PieChart, Download, Plus, CheckCircle, TrendingUp, Layers } from 'lucide-react';

export default function Revenue() {
  const [summary, setSummary] = useState(null);
  const [packages, setPackages] = useState([]);
  const [usages, setUsages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [newPkg, setNewPkg] = useState({
    patientId: 1,
    patientName: '',
    patientMrn: '',
    packageName: '',
    totalSessions: 5,
    totalAmount: 250
  });
  const { hasPermission } = useAuth();

  const loadData = async () => {
    setLoading(true);
    try {
      const sRes = await api.get('/revenue/summary');
      setSummary(sRes.data);

      const pRes = await api.get('/revenue/packages');
      setPackages(pRes.data || []);

      const uRes = await api.get('/revenue/usages');
      setUsages(uRes.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handlePurchase = async (e) => {
    e.preventDefault();
    try {
      await api.post('/revenue/packages/purchase', newPkg);
      setShowModal(false);
      setNewPkg({ patientId: 1, patientName: '', patientMrn: '', packageName: '', totalSessions: 5, totalAmount: 250 });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Purchase failed');
    }
  };

  const handleConsume = async (packageId) => {
    const notes = prompt('Enter session attendance notes:');
    if (notes === null) return;
    try {
      await api.post(`/revenue/packages/${packageId}/consume`, { notes });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to consume session');
    }
  };

  const handleExportCsv = async () => {
    try {
      const res = await api.get('/revenue/export/csv', { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'Revenue_Reconciliation.csv');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert('Export failed');
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Revenue vs. Usage Accounting (MOD-12)</h1>
          <p className="text-sm text-slate-500">Unearned / deferred prepaid packages vs. recognized consumption ledger</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handleExportCsv}
            className="flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 rounded-xl text-sm font-semibold shadow-sm transition"
          >
            <Download className="w-4 h-4 text-slate-500" />
            <span>Reconciliation CSV</span>
          </button>
          {hasPermission('revenue.manage') && (
            <button
              onClick={() => setShowModal(true)}
              className="flex items-center gap-2 px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow-sm transition"
            >
              <Plus className="w-4 h-4" />
              <span>Purchase Treatment Package</span>
            </button>
          )}
        </div>
      </div>

      {/* KPI Financial Cards */}
      {summary && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="p-5 bg-white rounded-2xl border border-slate-200 shadow-sm">
            <span className="text-xs uppercase font-bold text-slate-400">Total Cash Collected</span>
            <p className="text-2xl font-black text-slate-900 mt-1">${summary.totalCashCollected.toFixed(2)}</p>
            <p className="text-[11px] text-slate-500 mt-1">{summary.totalSessionsSold} prepaid sessions sold</p>
          </div>
          <div className="p-5 bg-emerald-50 rounded-2xl border border-emerald-200 shadow-sm">
            <span className="text-xs uppercase font-bold text-emerald-700">Earned (Recognized)</span>
            <p className="text-2xl font-black text-emerald-800 mt-1">${summary.totalEarnedRevenue.toFixed(2)}</p>
            <p className="text-[11px] text-emerald-600 mt-1">{summary.totalSessionsConsumed} attended sessions</p>
          </div>
          <div className="p-5 bg-amber-50 rounded-2xl border border-amber-200 shadow-sm">
            <span className="text-xs uppercase font-bold text-amber-700">Deferred (Unearned)</span>
            <p className="text-2xl font-black text-amber-800 mt-1">${summary.totalDeferredRevenue.toFixed(2)}</p>
            <p className="text-[11px] text-amber-600 mt-1">{summary.remainingSessions} unconsumed liability</p>
          </div>
          <div className="p-5 bg-white rounded-2xl border border-slate-200 shadow-sm">
            <span className="text-xs uppercase font-bold text-slate-400">Revenue Earned Ratio</span>
            <p className="text-2xl font-black text-brand-600 mt-1">{summary.earnedPercentage}%</p>
            <div className="w-full bg-slate-100 rounded-full h-1.5 mt-2">
              <div className="bg-brand-600 h-1.5 rounded-full" style={{ width: `${summary.earnedPercentage}%` }}></div>
            </div>
          </div>
        </div>
      )}

      {/* Packages Table */}
      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-100 font-semibold text-sm text-slate-800">
          Prepaid Treatment Packages Ledger
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-semibold border-b border-slate-100">
              <tr>
                <th className="py-3 px-4">Patient</th>
                <th className="py-3 px-4">Treatment Package</th>
                <th className="py-3 px-4 text-center">Sessions (Used / Total)</th>
                <th className="py-3 px-4 text-right">Collected</th>
                <th className="py-3 px-4 text-right">Earned</th>
                <th className="py-3 px-4 text-right">Deferred</th>
                <th className="py-3 px-4 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {packages.map(p => (
                <tr key={p.id} className="hover:bg-slate-50/50">
                  <td className="py-3.5 px-4">
                    <p className="font-semibold text-slate-900">{p.patientName}</p>
                    <p className="text-xs text-slate-400">{p.patientMrn}</p>
                  </td>
                  <td className="py-3.5 px-4 font-medium text-slate-800">{p.packageName}</td>
                  <td className="py-3.5 px-4 text-center">
                    <span className="font-mono text-xs font-bold text-slate-700">{p.usedSessions} / {p.totalSessions}</span>
                  </td>
                  <td className="py-3.5 px-4 text-right font-mono font-medium text-slate-700">${p.totalAmount.toFixed(2)}</td>
                  <td className="py-3.5 px-4 text-right font-mono font-semibold text-emerald-600">${p.earnedRevenue.toFixed(2)}</td>
                  <td className="py-3.5 px-4 text-right font-mono font-semibold text-amber-600">${p.deferredRevenue.toFixed(2)}</td>
                  <td className="py-3.5 px-4 text-right">
                    {hasPermission('revenue.manage') && p.remainingSessions > 0 ? (
                      <button
                        onClick={() => handleConsume(p.id)}
                        className="px-2.5 py-1 bg-brand-50 hover:bg-brand-100 text-brand-700 rounded-lg text-xs font-semibold"
                      >
                        + Consume Session
                      </button>
                    ) : (
                      <span className="text-xs text-slate-400 font-medium">Exhausted</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-4">Sell Treatment Package</h3>
            <form onSubmit={handlePurchase} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Eric Mugabe"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newPkg.patientName}
                  onChange={e => setNewPkg({ ...newPkg, patientName: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient MRN</label>
                <input
                  type="text"
                  required
                  placeholder="MRN-2026-0012"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newPkg.patientMrn}
                  onChange={e => setNewPkg({ ...newPkg, patientMrn: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Package Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Physiotherapy 5-Session Bundle"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newPkg.packageName}
                  onChange={e => setNewPkg({ ...newPkg, packageName: e.target.value })}
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Total Sessions</label>
                  <input
                    type="number"
                    min="1"
                    required
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                    value={newPkg.totalSessions}
                    onChange={e => setNewPkg({ ...newPkg, totalSessions: parseInt(e.target.value) })}
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Total Paid ($)</label>
                  <input
                    type="number"
                    step="0.01"
                    min="1"
                    required
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                    value={newPkg.totalAmount}
                    onChange={e => setNewPkg({ ...newPkg, totalAmount: parseFloat(e.target.value) })}
                  />
                </div>
              </div>

              <div className="flex gap-2 pt-4">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-sm font-semibold"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold"
                >
                  Purchase & Issue
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
