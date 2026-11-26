import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { BedDouble, ArrowRightLeft, Sparkles, AlertCircle, Clock, CheckCircle2, User, RefreshCw } from 'lucide-react';

export default function Beds() {
  const [beds, setBeds] = useState([]);
  const [transfers, setTransfers] = useState([]);
  const [turnaroundReports, setTurnaroundReports] = useState([]);
  const [selectedWard, setSelectedWard] = useState('');
  const [loading, setLoading] = useState(true);
  const [showRequestModal, setShowRequestModal] = useState(false);

  const [transferForm, setTransferForm] = useState({
    patientId: '00000000-0000-0000-0000-000000000001',
    patientName: 'Demo Patient',
    patientMrn: 'SC-0001',
    fromUnit: 'Operating Theatre (OT 1)',
    toUnit: 'Intensive Care Unit (ICU)',
    requestedBedType: 'ICU Ventilated',
    priority: 'STAT',
    clinicalNotes: 'Post-op emergency exploratory laparotomy. Ventilated, requires 1:1 ICU monitoring.'
  });

  const loadData = async () => {
    try {
      const [bedsRes, transfersRes, repRes] = await Promise.all([
        api.get('/beds', { params: { ward: selectedWard } }),
        api.get('/beds/transfers'),
        api.get('/beds/turnaround-report')
      ]);
      setBeds(bedsRes.data);
      setTransfers(transfersRes.data);
      setTurnaroundReports(repRes.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 5000); // 5-second polling per Excel sprint decision D8
    return () => clearInterval(interval);
  }, [selectedWard]);

  const handleRequestSubmit = async (e) => {
    e.preventDefault();
    try {
      await api.post('/beds/transfers', transferForm);
      setShowRequestModal(false);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error raising transfer request');
    }
  };

  const handleAssignBed = async (requestId, bedId) => {
    try {
      await api.put(`/beds/transfers/${requestId}/assign`, { bedId });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Cannot assign bed');
    }
  };

  const handleUpdateStage = async (requestId, nextStage) => {
    try {
      await api.put(`/beds/transfers/${requestId}/stage`, { stage: nextStage });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error updating stage');
    }
  };

  const handleCleanBed = async (bedId) => {
    try {
      await api.put(`/beds/${bedId}/status?status=Available`);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error cleaning bed');
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Available':
        return 'bg-emerald-100 text-emerald-800 border-emerald-300';
      case 'Occupied':
        return 'bg-rose-100 text-rose-800 border-rose-300';
      case 'Reserved':
        return 'bg-amber-100 text-amber-800 border-amber-300';
      case 'Cleaning':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      default:
        return 'bg-slate-100 text-slate-800 border-slate-300';
    }
  };

  const availableBeds = beds.filter((b) => b.status === 'Available');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <BedDouble className="w-7 h-7 text-brand-600" />
            Bed Reservation & Transfer Orchestrator
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-07 · Real-Time OT-to-ICU/Ward Handoff, Timestamped Tracking & Concurrency Guards
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowRequestModal(true)}
            className="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-4 py-2.5 rounded-xl font-medium shadow-sm transition"
          >
            <ArrowRightLeft className="w-4 h-4" />
            Raise OT ➔ ICU Transfer
          </button>
        </div>
      </div>

      {/* Turnaround KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {turnaroundReports.map((r, idx) => (
          <div key={idx} className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{r.unit} Turnaround</span>
              <div className="text-2xl font-bold text-slate-900 mt-1">{r.averageTurnaroundMinutes} <span className="text-xs font-normal text-slate-500">mins avg</span></div>
              <p className="text-[11px] text-slate-400 mt-0.5">Median: {r.medianTurnaroundMinutes}m · 90th percentile: {r.p90TurnaroundMinutes}m</p>
            </div>
            <div className="w-12 h-12 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center font-bold">
              <Clock className="w-6 h-6" />
            </div>
          </div>
        ))}
      </div>

      {/* Main Grid: Bed Board (8 cols) & Active Transfers (4 cols) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Bed Inventory Board */}
        <div className="lg:col-span-7 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <div className="flex items-center justify-between mb-5">
            <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <BedDouble className="w-5 h-5 text-brand-600" />
              Live Hospital Bed Inventory
            </h2>
            <div className="flex items-center gap-2">
              <select
                value={selectedWard}
                onChange={(e) => setSelectedWard(e.target.value)}
                className="px-3 py-1.5 border border-slate-200 rounded-xl text-xs bg-white"
              >
                <option value="">All Hospital Wards</option>
                <option value="ICU">Intensive Care Unit (ICU)</option>
                <option value="Surgical">Surgical Ward</option>
                <option value="General">General Medicine</option>
                <option value="Maternity">Maternity Ward</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
            {beds.map((b) => (
              <div
                key={b.id}
                className="p-3.5 rounded-xl border border-slate-200 hover:shadow-md transition bg-slate-50/50 flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-xs text-slate-800">{b.roomNumber} - {b.bedNumber}</span>
                    <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${getStatusBadge(b.status)}`}>
                      {b.status}
                    </span>
                  </div>
                  <div className="text-[11px] text-slate-500 mt-1">{b.wardName}</div>
                  <div className="text-[10px] text-brand-700 font-mono mt-0.5">{b.bedType}</div>

                  {b.currentPatientName && (
                    <div className="mt-2 p-1.5 bg-white rounded-lg border border-slate-200 text-xs">
                      <div className="font-semibold text-slate-800 flex items-center gap-1">
                        <User className="w-3 h-3 text-slate-400" />
                        {b.currentPatientName}
                      </div>
                      <span className="text-[10px] text-slate-400 font-mono">{b.currentPatientMrn}</span>
                    </div>
                  )}
                </div>

                {b.status === 'Cleaning' && (
                  <button
                    onClick={() => handleCleanBed(b.id)}
                    className="mt-3 w-full py-1 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 shadow-sm"
                  >
                    <Sparkles className="w-3.5 h-3.5" />
                    Mark Cleaned & Available
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Transfer Requests Stream */}
        <div className="lg:col-span-5 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col h-[650px]">
          <h2 className="text-base font-bold text-slate-900 flex items-center gap-2 mb-4">
            <ArrowRightLeft className="w-5 h-5 text-indigo-600" />
            Real-Time Transfer Handoff Stream
          </h2>

          <div className="flex-1 overflow-y-auto space-y-4 pr-1">
            {transfers.length === 0 ? (
              <div className="p-8 text-center text-xs text-slate-400">No active transfer requests</div>
            ) : (
              transfers.map((t) => (
                <div key={t.id} className="p-4 rounded-xl border border-slate-200 bg-slate-50 space-y-2 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-slate-900">{t.patientName}</span>
                    <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-100 text-indigo-800">
                      {t.priority}
                    </span>
                  </div>
                  <div className="text-[11px] text-slate-600 flex items-center justify-between font-medium">
                    <span>{t.fromUnit} ➔ {t.toUnit}</span>
                    <span className="font-mono text-slate-500">{t.patientMrn}</span>
                  </div>

                  <p className="text-[11px] text-slate-600 italic bg-white p-2 rounded-lg border border-slate-100">
                    "{t.clinicalNotes}"
                  </p>

                  <div className="pt-2 border-t border-slate-200 flex items-center justify-between">
                    <span className="text-[10px] font-semibold text-slate-500">
                      Stage: <strong className="text-slate-800">{t.status}</strong>
                    </span>

                    {/* Step Actions */}
                    {t.status === 'Requested' && (
                      <div className="flex gap-1.5">
                        <select
                          id={`bed-select-${t.id}`}
                          className="px-2 py-1 border border-slate-200 rounded text-xs bg-white"
                          defaultValue=""
                        >
                          <option value="" disabled>Select Bed</option>
                          {availableBeds.map((b) => (
                            <option key={b.id} value={b.id}>
                              {b.wardName} - {b.bedNumber}
                            </option>
                          ))}
                        </select>
                        <button
                          onClick={() => {
                            const sel = document.getElementById(`bed-select-${t.id}`);
                            if (sel && sel.value) handleAssignBed(t.id, sel.value);
                          }}
                          className="px-2.5 py-1 bg-brand-600 text-white rounded text-xs font-semibold"
                        >
                          Assign
                        </button>
                      </div>
                    )}

                    {t.status === 'BedAssigned' && (
                      <button
                        onClick={() => handleUpdateStage(t.id, 'Dispatched')}
                        className="px-2.5 py-1 bg-amber-600 text-white rounded text-xs font-semibold"
                      >
                        Dispatch Patient ➔
                      </button>
                    )}

                    {t.status === 'Dispatched' && (
                      <button
                        onClick={() => handleUpdateStage(t.id, 'Received')}
                        className="px-2.5 py-1 bg-indigo-600 text-white rounded text-xs font-semibold"
                      >
                        Confirm Receipt ➔
                      </button>
                    )}

                    {t.status === 'Received' && (
                      <button
                        onClick={() => handleUpdateStage(t.id, 'Completed')}
                        className="px-2.5 py-1 bg-emerald-600 text-white rounded text-xs font-semibold flex items-center gap-1"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        Complete Transfer
                      </button>
                    )}

                    {t.status === 'Completed' && (
                      <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200">
                        ✓ In Bed: {t.assignedBedNumber}
                      </span>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      {/* Raise Transfer Modal */}
      {showRequestModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-3 flex items-center gap-2">
              <ArrowRightLeft className="w-5 h-5 text-brand-600" />
              Raise Bed Transfer Handoff (FR-BED-002)
            </h3>
            <form onSubmit={handleRequestSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Patient Name</label>
                <input
                  type="text"
                  required
                  value={transferForm.patientName}
                  onChange={(e) => setTransferForm({ ...transferForm, patientName: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">From Unit</label>
                  <input
                    type="text"
                    required
                    value={transferForm.fromUnit}
                    onChange={(e) => setTransferForm({ ...transferForm, fromUnit: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Destination Unit</label>
                  <select
                    value={transferForm.toUnit}
                    onChange={(e) => setTransferForm({ ...transferForm, toUnit: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="Intensive Care Unit (ICU)">Intensive Care Unit (ICU)</option>
                    <option value="Surgical Ward">Surgical Ward</option>
                    <option value="General Medicine">General Medicine</option>
                    <option value="Maternity Ward">Maternity Ward</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Requested Bed Type</label>
                  <select
                    value={transferForm.requestedBedType}
                    onChange={(e) => setTransferForm({ ...transferForm, requestedBedType: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="ICU Ventilated">ICU Ventilated</option>
                    <option value="ICU Isolation">ICU Isolation</option>
                    <option value="Post-Op Surgical">Post-Op Surgical</option>
                    <option value="General Acute">General Acute</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Clinical Priority</label>
                  <select
                    value={transferForm.priority}
                    onChange={(e) => setTransferForm({ ...transferForm, priority: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="STAT">STAT (Immediate &lt; 15m)</option>
                    <option value="Urgent">Urgent (&lt; 1 hr)</option>
                    <option value="Routine">Routine (&lt; 4 hrs)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Clinical Handoff Notes & Allergies</label>
                <textarea
                  rows={3}
                  value={transferForm.clinicalNotes}
                  onChange={(e) => setTransferForm({ ...transferForm, clinicalNotes: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  placeholder="Patient summary, oxygen requirements, allergy precautions..."
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setShowRequestModal(false)}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-medium"
                >
                  Submit Transfer Request
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
