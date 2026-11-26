import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { LogOut, CheckCircle2, Clock, XCircle, Plus, FileEdit } from 'lucide-react';

export default function Discharge() {
  const [discharges, setDischarges] = useState([]);
  const [selectedDischarge, setSelectedDischarge] = useState(null);
  const [loading, setLoading] = useState(true);
  const [showInitiateModal, setShowInitiateModal] = useState(false);
  const [newDischarge, setNewDischarge] = useState({
    patientName: '',
    patientMrn: '',
    summary: '',
    medications: '',
    followUpInstructions: ''
  });
  const { hasPermission } = useAuth();

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/discharge');
      setDischarges(res.data || []);
      if (res.data && res.data.length > 0) {
        setSelectedDischarge(res.data[0]);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleInitiate = async (e) => {
    e.preventDefault();
    try {
      await api.post('/discharge/initiate', newDischarge);
      setShowInitiateModal(false);
      setNewDischarge({ patientName: '', patientMrn: '', summary: '', medications: '', followUpInstructions: '' });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Initiate failed');
    }
  };

  const handleFinalize = async (id) => {
    if (!confirm('Finalize discharge summary and generate departmental clearance checklist?')) return;
    try {
      await api.post(`/discharge/${id}/finalize`);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Finalize failed');
    }
  };

  const handleClearance = async (clearanceId, status) => {
    const reason = status === 'Blocked' ? prompt('Reason for blocking clearance:') : null;
    if (status === 'Blocked' && !reason) return;

    try {
      await api.post(`/discharge/clearances/${clearanceId}`, { status, blockReason: reason });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Clearance action failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Discharge Turnaround Tracker (MOD-08)</h1>
          <p className="text-sm text-slate-500">Staged clearance handoffs across Nursing, Pharmacy, Billing, and Lab</p>
        </div>
        {hasPermission('discharge.manage') && (
          <button
            onClick={() => setShowInitiateModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Initiate Patient Discharge</span>
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Discharges List */}
        <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm lg:col-span-1">
          <div className="p-4 border-b border-slate-100 font-semibold text-sm text-slate-700 flex justify-between">
            <span>In-Progress Discharges</span>
            <span className="text-xs bg-brand-50 text-brand-600 px-2 py-0.5 rounded-full font-bold">{discharges.length}</span>
          </div>
          <div className="divide-y divide-slate-100 max-h-[600px] overflow-y-auto">
            {discharges.map(d => (
              <div
                key={d.id}
                onClick={() => setSelectedDischarge(d)}
                className={`p-4 cursor-pointer transition ${
                  selectedDischarge?.id === d.id ? 'bg-brand-50/70 border-l-4 border-brand-600' : 'hover:bg-slate-50'
                }`}
              >
                <div className="flex items-center justify-between">
                  <h4 className="font-semibold text-slate-900 text-sm">{d.patientName}</h4>
                  <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase ${
                    d.stage === 'Completed' ? 'bg-emerald-100 text-emerald-800' :
                    d.stage === 'ClearancesPending' ? 'bg-amber-100 text-amber-800' :
                    'bg-slate-100 text-slate-600'
                  }`}>
                    {d.stage}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">{d.patientMrn}</p>
                <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
                  <span>Clearances:</span>
                  <span className="font-semibold text-slate-700">
                    {d.clearances.filter(c => c.status === 'Cleared').length} / {d.clearances.length}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Selected Discharge Details */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm lg:col-span-2">
          {selectedDischarge ? (
            <div className="space-y-6">
              <div className="flex items-start justify-between border-b border-slate-100 pb-4">
                <div>
                  <h3 className="text-xl font-bold text-slate-900">{selectedDischarge.patientName}</h3>
                  <p className="text-xs text-slate-500 font-mono mt-0.5">MRN: {selectedDischarge.patientMrn} | Stage: <span className="font-bold text-slate-700">{selectedDischarge.stage}</span></p>
                </div>
                {selectedDischarge.stage === 'Draft' && hasPermission('discharge.manage') && (
                  <button
                    onClick={() => handleFinalize(selectedDischarge.id)}
                    className="px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-semibold shadow-sm"
                  >
                    Finalize & Lock Checklist
                  </button>
                )}
              </div>

              {/* Clinical Summary */}
              <div className="p-4 bg-slate-50 rounded-xl border border-slate-200/60 text-sm space-y-2">
                <p className="text-xs font-bold text-slate-600 uppercase tracking-wider">Clinical Summary</p>
                <p className="text-slate-800 whitespace-pre-line">{selectedDischarge.summary}</p>
                {selectedDischarge.medications && (
                  <div className="pt-2 border-t border-slate-200">
                    <p className="text-xs font-bold text-slate-600 uppercase">Medications to Continue</p>
                    <p className="text-slate-700 font-mono text-xs mt-0.5">{selectedDischarge.medications}</p>
                  </div>
                )}
              </div>

              {/* Department Clearances Checklist */}
              <div>
                <h4 className="text-sm font-bold text-slate-900 mb-3 uppercase tracking-wider">Department Clearance Gates</h4>
                <div className="space-y-2.5">
                  {selectedDischarge.clearances.map(c => (
                    <div
                      key={c.id}
                      className="p-3.5 bg-white border border-slate-200 rounded-xl flex items-center justify-between"
                    >
                      <div className="flex items-center gap-3">
                        {c.status === 'Cleared' ? (
                          <CheckCircle2 className="w-5 h-5 text-emerald-500" />
                        ) : c.status === 'Blocked' ? (
                          <XCircle className="w-5 h-5 text-red-500" />
                        ) : (
                          <Clock className="w-5 h-5 text-amber-500" />
                        )}
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-slate-900 text-sm">{c.departmentName}</span>
                            {c.isMandatory && (
                              <span className="text-[10px] bg-red-100 text-red-700 px-1.5 py-0.2 rounded font-bold uppercase">Mandatory</span>
                            )}
                          </div>
                          {c.blockReason && (
                            <p className="text-xs text-red-600 mt-0.5 font-medium">Blocked reason: {c.blockReason}</p>
                          )}
                          {c.clearedAtUtc && (
                            <p className="text-[11px] text-slate-400">Cleared at: {new Date(c.clearedAtUtc).toLocaleTimeString()}</p>
                          )}
                        </div>
                      </div>

                      {hasPermission('discharge.manage') && c.status === 'Pending' && (
                        <div className="flex gap-1.5">
                          <button
                            onClick={() => handleClearance(c.id, 'Cleared')}
                            className="px-2.5 py-1 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg text-xs font-semibold"
                          >
                            ✓ Clear
                          </button>
                          <button
                            onClick={() => handleClearance(c.id, 'Blocked')}
                            className="px-2.5 py-1 bg-red-50 hover:bg-red-100 text-red-700 rounded-lg text-xs font-semibold"
                          >
                            ✗ Block
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                  {selectedDischarge.clearances.length === 0 && (
                    <p className="text-sm text-slate-400 italic">No clearance gates yet. Finalize the draft above to initialize.</p>
                  )}
                </div>
              </div>
            </div>
          ) : (
            <div className="p-16 text-center text-slate-400 text-sm">Select a discharge from the list to view clearances.</div>
          )}
        </div>
      </div>

      {/* Modal */}
      {showInitiateModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-4">Doctor Discharge Order</h3>
            <form onSubmit={handleInitiate} className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient Name</label>
                  <input
                    type="text"
                    required
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                    value={newDischarge.patientName}
                    onChange={e => setNewDischarge({ ...newDischarge, patientName: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient MRN</label>
                  <input
                    type="text"
                    required
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                    value={newDischarge.patientMrn}
                    onChange={e => setNewDischarge({ ...newDischarge, patientMrn: e.target.value })}
                  />
                </div>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Clinical Discharge Summary</label>
                <textarea
                  required
                  rows={3}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newDischarge.summary}
                  onChange={e => setNewDischarge({ ...newDischarge, summary: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Home Medications</label>
                <input
                  type="text"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newDischarge.medications}
                  onChange={e => setNewDischarge({ ...newDischarge, medications: e.target.value })}
                />
              </div>

              <div className="flex gap-2 pt-4">
                <button
                  type="button"
                  onClick={() => setShowInitiateModal(false)}
                  className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-sm font-semibold"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold"
                >
                  Initiate Draft
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
