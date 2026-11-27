import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { Pill, AlertOctagon, PackageCheck, ShieldAlert, CheckCircle2, Search, Plus, DollarSign } from 'lucide-react';

export default function Pharmacy() {
  const [catalog, setCatalog] = useState([]);
  const [alerts, setAlerts] = useState([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [dispenseModal, setDispenseModal] = useState(false);
  const [selectedDrug, setSelectedDrug] = useState(null);
  const [dispenseForm, setDispenseForm] = useState({
    patientName: 'Demo Patient (SC-0001)',
    quantity: 10,
    allergyOverrideReason: ''
  });
  const [lastDispense, setLastDispense] = useState(null);

  const loadData = async () => {
    try {
      const [catRes, altRes] = await Promise.all([
        api.get('/pharmacy/catalog', { params: { query: searchQuery } }),
        api.get('/pharmacy/alerts')
      ]);
      setCatalog(catRes.data);
      setAlerts(altRes.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [searchQuery]);

  const handleOpenDispense = (drug) => {
    setSelectedDrug(drug);
    setDispenseForm({
      patientName: 'Demo Patient (SC-0001)',
      quantity: 10,
      allergyOverrideReason: ''
    });
    setDispenseModal(true);
  };

  const handleDispenseSubmit = async (e) => {
    e.preventDefault();
    if (!selectedDrug) return;

    try {
      const res = await api.post('/pharmacy/dispense', {
        drugId: selectedDrug.id,
        patientName: dispenseForm.patientName,
        quantity: parseInt(dispenseForm.quantity, 10),
        allergyOverrideReason: dispenseForm.allergyOverrideReason
      });

      setLastDispense(res.data);
      setDispenseModal(false);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Dispensing failed');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Pill className="w-7 h-7 text-brand-600" />
            Pharmacy & Inventory Management
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-05 · Batch Expiry Tracking, Real-Time Dispensing, Negative Stock Guards & Reorder Alerts
          </p>
        </div>
      </div>

      {/* Success Notification */}
      {lastDispense && (
        <div className="p-4 bg-emerald-50 border border-emerald-200 rounded-2xl flex items-center justify-between shadow-sm animate-in fade-in">
          <div className="flex items-center gap-3">
            <CheckCircle2 className="w-6 h-6 text-emerald-600 shrink-0" />
            <div>
              <span className="font-bold text-emerald-900 text-sm">Successfully Dispensed & Billed!</span>
              <p className="text-xs text-emerald-700 mt-0.5">
                {lastDispense.dispenseNumber} · Dispensed {lastDispense.quantity} units of {lastDispense.drugName} (Batch: {lastDispense.batchNumber}) for {lastDispense.patientName}. Posted to invoice ({lastDispense.billedAmount.toLocaleString()} RWF).
              </p>
            </div>
          </div>
          <button onClick={() => setLastDispense(null)} className="text-xs text-emerald-700 font-semibold underline">
            Dismiss
          </button>
        </div>
      )}

      {/* Automated Reorder Alert Banner (FR-PHM-005) */}
      {alerts.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-2xl p-5 shadow-sm">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-bold text-amber-900 uppercase tracking-wide flex items-center gap-2">
              <AlertOctagon className="w-5 h-5 text-amber-600" />
              Automated Reorder Alerts ({alerts.length} Low-Stock Breaches)
            </h3>
            <span className="text-xs text-amber-700 font-medium">Automatic threshold detection</span>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {alerts.map((a, i) => (
              <div key={i} className="p-3 bg-white rounded-xl border border-amber-200 text-xs shadow-xs">
                <div className="flex items-center justify-between font-bold text-slate-900">
                  <span>{a.drugName}</span>
                  <span className={`px-2 py-0.5 rounded text-[10px] ${a.urgency === 'Critical' ? 'bg-red-100 text-red-800' : 'bg-amber-100 text-amber-800'}`}>
                    {a.urgency}
                  </span>
                </div>
                <div className="mt-2 flex items-center justify-between text-slate-600">
                  <span>Stock: <strong className="text-red-600">{a.currentStock}</strong> / {a.minimumThreshold} min</span>
                  <span className="text-brand-600 font-medium">Reorder: +{a.suggestedReorderQty} units</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Drug Catalog Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-5 border-b border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="relative max-w-sm w-full">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Search drug name, generic, category..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            />
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 border-b border-slate-200 text-xs uppercase font-semibold text-slate-500 tracking-wider">
              <tr>
                <th className="px-6 py-3.5">Medication Name</th>
                <th className="px-6 py-3.5">Category & Form</th>
                <th className="px-6 py-3.5">Batches & Expiry</th>
                <th className="px-6 py-3.5">Unit Price</th>
                <th className="px-6 py-3.5">Stock Level</th>
                <th className="px-6 py-3.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {catalog.map((d) => (
                <tr key={d.id} className="hover:bg-slate-50 transition">
                  <td className="px-6 py-4">
                    <span className="font-bold text-slate-900 block">{d.brandName}</span>
                    <span className="text-xs text-slate-500">{d.genericName} · {d.strength}</span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="font-medium text-slate-700 block">{d.category}</span>
                    <span className="text-xs text-slate-400">{d.dosageForm}</span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="space-y-1">
                      {d.batches && d.batches.length > 0 ? (
                        d.batches.map((b, i) => (
                          <div key={i} className="flex items-center gap-2 text-xs">
                            <span className="font-mono bg-slate-100 px-1.5 py-0.5 rounded text-[11px] text-slate-700">
                              {b.batchNumber}
                            </span>
                            <span className="text-slate-500">{b.quantity} qty</span>
                            <span className={`text-[10px] font-medium ${b.isNearExpiry ? 'text-amber-600 font-bold' : 'text-slate-400'}`}>
                              exp {new Date(b.expiryDate).toLocaleDateString()}
                            </span>
                          </div>
                        ))
                      ) : (
                        <span className="text-xs text-slate-400 italic">No batches</span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 font-mono font-semibold text-slate-800">
                    {d.unitPrice.toLocaleString()} RWF
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <span className={`text-base font-bold ${d.isLowStock ? 'text-amber-600' : 'text-slate-900'}`}>
                        {d.totalStock}
                      </span>
                      {d.isLowStock && (
                        <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 border border-amber-300">
                          Low Stock
                        </span>
                      )}
                    </div>
                    <span className="text-[11px] text-slate-400">Min threshold: {d.minimumStockThreshold}</span>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <button
                      onClick={() => handleOpenDispense(d)}
                      disabled={d.totalStock === 0}
                      className="px-3 py-1.5 bg-brand-600 hover:bg-brand-700 disabled:bg-slate-200 disabled:text-slate-400 text-white rounded-xl text-xs font-semibold shadow-xs transition"
                    >
                      Dispense Rx ➔
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Dispense Modal with Allergy & Negative Stock Guard */}
      {dispenseModal && selectedDrug && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-1 flex items-center gap-2">
              <Pill className="w-5 h-5 text-brand-600" />
              Dispense Medication (FR-PHM-003)
            </h3>
            <p className="text-xs text-slate-500 mb-4">
              Dispensing for: <strong className="text-slate-800">{selectedDrug.brandName} ({selectedDrug.genericName} {selectedDrug.strength})</strong>
            </p>

            <form onSubmit={handleDispenseSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Patient Name / MRN</label>
                <input
                  type="text"
                  required
                  value={dispenseForm.patientName}
                  onChange={(e) => setDispenseForm({ ...dispenseForm, patientName: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Quantity to Dispense</label>
                <input
                  type="number"
                  min="1"
                  max={selectedDrug.totalStock}
                  required
                  value={dispenseForm.quantity}
                  onChange={(e) => setDispenseForm({ ...dispenseForm, quantity: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm font-mono"
                />
                <span className="text-[11px] text-slate-500 mt-1 block">
                  Available in stock: <strong>{selectedDrug.totalStock}</strong> units. Negative stock blocked.
                </span>
              </div>

              {/* Allergy Warning Notice */}
              <div className="p-3 bg-red-50 border border-red-200 rounded-xl text-xs space-y-1">
                <span className="font-bold text-red-900 flex items-center gap-1">
                  <ShieldAlert className="w-4 h-4 text-red-600" />
                  Allergy Conflict Verification
                </span>
                <p className="text-red-700 text-[11px]">
                  If patient has a documented allergy to this drug class, mandatory override reason is required.
                </p>
                <input
                  type="text"
                  placeholder="Override reason (if clinical allergy warning triggered)"
                  value={dispenseForm.allergyOverrideReason}
                  onChange={(e) => setDispenseForm({ ...dispenseForm, allergyOverrideReason: e.target.value })}
                  className="w-full px-2 py-1 bg-white border border-red-200 rounded text-xs mt-1"
                />
              </div>

              <div className="flex justify-between items-center pt-2">
                <span className="text-xs text-slate-500">
                  Total Bill: <strong className="text-slate-900 font-mono text-sm">{(selectedDrug.unitPrice * (dispenseForm.quantity || 0)).toLocaleString()} RWF</strong>
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setDispenseModal(false)}
                    className="px-3 py-2 border border-slate-200 rounded-xl text-xs font-medium text-slate-600 hover:bg-slate-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-xs font-semibold"
                  >
                    Confirm Dispense & Bill
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
