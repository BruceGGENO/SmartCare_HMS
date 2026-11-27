import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { Microscope, TestTube2, CheckCircle2, AlertTriangle, Search, Plus, FileCheck, Clock, User } from 'lucide-react';

export default function Lab() {
  const [catalog, setCatalog] = useState([]);
  const [orders, setOrders] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [loading, setLoading] = useState(true);
  const [orderModal, setOrderModal] = useState(false);
  const [resultModal, setResultModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);

  const [orderForm, setOrderForm] = useState({
    patientId: '00000000-0000-0000-0000-000000000001',
    patientName: 'Demo Patient',
    testCatalogItemId: '',
    priority: 'Routine',
    clinicalNotes: 'Suspected bacterial infection. Check leukocytosis.'
  });

  const [resultForm, setResultForm] = useState({
    resultValue: '',
    unit: '',
    isAbnormal: false,
    isCritical: false,
    technicianNotes: ''
  });

  const loadData = async () => {
    try {
      const [catRes, ordRes] = await Promise.all([
        api.get('/lab/catalog', { params: { category: selectedCategory } }),
        api.get('/lab/orders')
      ]);
      setCatalog(catRes.data);
      setOrders(ordRes.data);
      if (catRes.data.length > 0 && !orderForm.testCatalogItemId) {
        setOrderForm((prev) => ({ ...prev, testCatalogItemId: catRes.data[0].id }));
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [selectedCategory]);

  const handleCreateOrder = async (e) => {
    e.preventDefault();
    try {
      await api.post('/lab/orders', orderForm);
      setOrderModal(false);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error ordering test');
    }
  };

  const handleUpdateStatus = async (orderId, newStatus) => {
    try {
      await api.put(`/lab/orders/${orderId}/sample-status`, { status: newStatus });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error updating sample status');
    }
  };

  const handleOpenResult = (order) => {
    setSelectedOrder(order);
    setResultForm({
      resultValue: order.resultValue || '',
      unit: order.unit || '',
      isAbnormal: order.isAbnormal || false,
      isCritical: order.isCritical || false,
      technicianNotes: order.technicianNotes || ''
    });
    setResultModal(true);
  };

  const handleSubmitResult = async (e) => {
    e.preventDefault();
    if (!selectedOrder) return;

    try {
      await api.post(`/lab/orders/${selectedOrder.id}/result`, resultForm);
      setResultModal(false);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error submitting result');
    }
  };

  const handleVerify = async (orderId) => {
    try {
      await api.post(`/lab/orders/${orderId}/verify`);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Error verifying result');
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Collected':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'Received':
        return 'bg-indigo-100 text-indigo-800 border-indigo-200';
      case 'In-process':
        return 'bg-amber-100 text-amber-800 border-amber-200';
      case 'Completed':
        return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      default:
        return 'bg-slate-100 text-slate-800 border-slate-200';
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Microscope className="w-7 h-7 text-brand-600" />
            Laboratory & Radiology Orders
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-06 · Sample Tracking, Structured Reference Ranges, Verification & Billing Auto-Posting
          </p>
        </div>
        <button
          onClick={() => setOrderModal(true)}
          className="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-4 py-2.5 rounded-xl font-medium shadow-sm transition"
        >
          <Plus className="w-4 h-4" />
          Order Lab Test / Imaging
        </button>
      </div>

      {/* Orders Worklist */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-5 border-b border-slate-200 flex items-center justify-between">
          <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
            <TestTube2 className="w-5 h-5 text-brand-600" />
            Clinical Laboratory Worklist
          </h2>
          <span className="text-xs font-semibold px-2.5 py-1 bg-brand-50 text-brand-700 rounded-full border border-brand-200">
            {orders.length} active orders
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 border-b border-slate-200 text-xs uppercase font-semibold text-slate-500 tracking-wider">
              <tr>
                <th className="px-6 py-3.5">Order / Patient</th>
                <th className="px-6 py-3.5">Test Details</th>
                <th className="px-6 py-3.5">Sample Status</th>
                <th className="px-6 py-3.5">Results & Flag</th>
                <th className="px-6 py-3.5">Verification</th>
                <th className="px-6 py-3.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {orders.map((o) => (
                <tr key={o.id} className="hover:bg-slate-50 transition">
                  <td className="px-6 py-4">
                    <span className="font-bold text-slate-900 block">{o.patientName}</span>
                    <span className="text-xs text-slate-400 font-mono">{o.orderNumber} · {o.patientMrn}</span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="font-semibold text-slate-800 block">{o.testName}</span>
                    <span className="text-xs text-slate-500">{o.category} · Priority: <strong className="text-brand-600">{o.priority}</strong></span>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${getStatusBadge(o.sampleStatus)}`}>
                      {o.sampleStatus}
                    </span>
                    <span className="block text-[10px] text-slate-400 font-mono mt-1">Barcode: {o.sampleId}</span>
                  </td>
                  <td className="px-6 py-4">
                    {o.resultValue ? (
                      <div>
                        <div className="flex items-center gap-1.5">
                          <span className={`font-mono text-xs font-bold ${o.isAbnormal ? 'text-rose-600' : 'text-slate-900'}`}>
                            {o.resultValue}
                          </span>
                          {o.isAbnormal && (
                            <span className="text-[10px] bg-red-100 text-red-800 px-1.5 py-0.2 rounded font-bold">
                              Abnormal
                            </span>
                          )}
                        </div>
                        <span className="text-[10px] text-slate-400 block mt-0.5">Ref: {o.referenceRange}</span>
                      </div>
                    ) : (
                      <span className="text-xs text-slate-400 italic">Pending analysis</span>
                    )}
                  </td>
                  <td className="px-6 py-4">
                    {o.isVerified ? (
                      <span className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200">
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        Verified by {o.verifiedBy}
                      </span>
                    ) : (
                      <span className="text-xs text-amber-700 bg-amber-50 px-2 py-0.5 rounded border border-amber-200">
                        Awaiting Verification
                      </span>
                    )}
                  </td>
                  <td className="px-6 py-4 text-right space-x-2">
                    {o.sampleStatus === 'Collected' && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, 'Received')}
                        className="px-2.5 py-1 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-xs font-semibold shadow-xs"
                      >
                        Receive Sample
                      </button>
                    )}

                    {o.sampleStatus === 'Received' && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, 'In-process')}
                        className="px-2.5 py-1 bg-amber-600 hover:bg-amber-700 text-white rounded-lg text-xs font-semibold shadow-xs"
                      >
                        Start Analysis
                      </button>
                    )}

                    {(o.sampleStatus === 'In-process' || o.sampleStatus === 'Completed') && (
                      <button
                        onClick={() => handleOpenResult(o)}
                        className="px-2.5 py-1 bg-slate-800 hover:bg-slate-900 text-white rounded-lg text-xs font-semibold shadow-xs"
                      >
                        {o.resultValue ? 'Edit Result' : 'Enter Result'}
                      </button>
                    )}

                    {o.resultValue && !o.isVerified && (
                      <button
                        onClick={() => handleVerify(o.id)}
                        className="px-2.5 py-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-semibold shadow-xs"
                      >
                        Verify & Release
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Order Test Modal */}
      {orderModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-3 flex items-center gap-2">
              <Microscope className="w-5 h-5 text-brand-600" />
              Order Lab / Radiology Test (FR-LAB-001)
            </h3>
            <form onSubmit={handleCreateOrder} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Patient Name</label>
                <input
                  type="text"
                  required
                  value={orderForm.patientName}
                  onChange={(e) => setOrderForm({ ...orderForm, patientName: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Test Catalog Item</label>
                <select
                  value={orderForm.testCatalogItemId}
                  onChange={(e) => setOrderForm({ ...orderForm, testCatalogItemId: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                >
                  {catalog.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name} ({c.category}) — {c.unitPrice.toLocaleString()} RWF
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Priority</label>
                  <select
                    value={orderForm.priority}
                    onChange={(e) => setOrderForm({ ...orderForm, priority: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="Routine">Routine</option>
                    <option value="Urgent">Urgent</option>
                    <option value="STAT">STAT (Immediate)</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Bill Auto-Post</label>
                  <input
                    type="text"
                    disabled
                    value="Auto-post to patient invoice"
                    className="w-full px-3 py-2 bg-slate-100 border border-slate-200 rounded-xl text-xs text-slate-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Doctor Clinical Notes</label>
                <textarea
                  rows={2}
                  value={orderForm.clinicalNotes}
                  onChange={(e) => setOrderForm({ ...orderForm, clinicalNotes: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setOrderModal(false)}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-medium"
                >
                  Submit Order
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Enter Result Modal */}
      {resultModal && selectedOrder && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-2 flex items-center gap-2">
              <FileCheck className="w-5 h-5 text-brand-600" />
              Enter Structured Result (FR-LAB-003)
            </h3>
            <p className="text-xs text-slate-500 mb-4">
              Patient: <strong className="text-slate-800">{selectedOrder.patientName}</strong> · Test: <strong className="text-slate-800">{selectedOrder.testName}</strong>
            </p>

            <form onSubmit={handleSubmitResult} className="space-y-4">
              <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-xs text-slate-600">
                Reference Range: <strong className="text-slate-800">{selectedOrder.referenceRange}</strong>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Result Value</label>
                  <input
                    type="text"
                    required
                    value={resultForm.resultValue}
                    onChange={(e) => setResultForm({ ...resultForm, resultValue: e.target.value })}
                    placeholder="e.g. 14.8, Negative, Clear"
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm font-mono"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Unit of Measurement</label>
                  <input
                    type="text"
                    value={resultForm.unit}
                    onChange={(e) => setResultForm({ ...resultForm, unit: e.target.value })}
                    placeholder="e.g. 10^3/uL, mmol/L, mg/dL"
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
              </div>

              <div className="flex gap-4 p-3 bg-slate-50 rounded-xl border border-slate-200">
                <label className="flex items-center gap-2 text-xs font-semibold text-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={resultForm.isAbnormal}
                    onChange={(e) => setResultForm({ ...resultForm, isAbnormal: e.target.checked })}
                    className="rounded text-brand-600"
                  />
                  <span>Flag as Abnormal</span>
                </label>
                <label className="flex items-center gap-2 text-xs font-semibold text-red-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={resultForm.isCritical}
                    onChange={(e) => setResultForm({ ...resultForm, isCritical: e.target.checked })}
                    className="rounded text-red-600"
                  />
                  <span>Critical Panic Value</span>
                </label>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Technician Notes & Observations</label>
                <textarea
                  rows={2}
                  value={resultForm.technicianNotes}
                  onChange={(e) => setResultForm({ ...resultForm, technicianNotes: e.target.value })}
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  placeholder="Clinical smear notes, differential count, equipment flags..."
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setResultModal(false)}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-medium"
                >
                  Save Result
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
