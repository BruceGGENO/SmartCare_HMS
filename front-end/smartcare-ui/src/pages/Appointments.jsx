import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { Calendar, User, Clock, CheckCircle, XCircle, Plus } from 'lucide-react';

export default function Appointments() {
  const [appointments, setAppointments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [patients, setPatients] = useState([]);
  const [doctors, setDoctors] = useState([]);
  const [form, setForm] = useState({
    patientId: 1,
    doctorUserId: '',
    department: 'General Outpatient',
    scheduledAtUtc: ''
  });
  const [message, setMessage] = useState('');
  const { hasPermission } = useAuth();

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/appointments');
      setAppointments(res.data || []);
      
      if (hasPermission('appointments.patients.read')) {
        const pRes = await api.get('/appointments/patients');
        setPatients(pRes.data || []);
      }
      if (hasPermission('appointments.doctors.read')) {
        const dRes = await api.get('/appointments/doctors');
        setDoctors(dRes.data || []);
        if (dRes.data && dRes.data.length > 0) {
          setForm(f => ({ ...f, doctorUserId: dRes.data[0].userId }));
        }
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

  const handleBook = async (e) => {
    e.preventDefault();
    try {
      await api.post('/appointments', form);
      setMessage('Appointment booked successfully!');
      setShowModal(false);
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Booking failed');
    }
  };

  const handleCancel = async (id) => {
    const reason = prompt('Reason for cancellation:');
    if (!reason) return;
    try {
      await api.post(`/appointments/${id}/cancel`, { reason });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Cancel failed');
    }
  };

  const handleCheckIn = async (code) => {
    try {
      await api.post('/appointments/check-in', { code });
      alert('Checked in successfully!');
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Check-in failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Appointments Management</h1>
          <p className="text-sm text-slate-500">Scheduled clinical consultations and check-ins</p>
        </div>
        {hasPermission('appointments.book') && (
          <button
            onClick={() => setShowModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Book Appointment</span>
          </button>
        )}
      </div>

      {message && (
        <div className="p-4 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl text-sm flex items-center justify-between">
          <span>{message}</span>
          <button onClick={() => setMessage('')} className="font-bold">×</button>
        </div>
      )}

      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-100 flex items-center justify-between">
          <span className="font-semibold text-sm text-slate-700">Total Consultations ({appointments.length})</span>
          <button onClick={loadData} className="text-xs text-brand-600 hover:text-brand-700 font-medium">Refresh List</button>
        </div>

        {loading ? (
          <div className="p-12 text-center text-slate-400 text-sm">Loading appointments...</div>
        ) : appointments.length === 0 ? (
          <div className="p-12 text-center text-slate-400 text-sm">No scheduled appointments found.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-semibold border-b border-slate-100">
                <tr>
                  <th className="py-3 px-4">Patient</th>
                  <th className="py-3 px-4">Doctor</th>
                  <th className="py-3 px-4">Department</th>
                  <th className="py-3 px-4">Scheduled At</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4">Check-In Code</th>
                  <th className="py-3 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {appointments.map((a) => (
                  <tr key={a.id} className="hover:bg-slate-50/50 transition">
                    <td className="py-3.5 px-4">
                      <p className="font-semibold text-slate-900">{a.patientName}</p>
                      <p className="text-xs text-slate-400">{a.patientMrn}</p>
                    </td>
                    <td className="py-3.5 px-4 font-medium text-slate-700">{a.doctorName}</td>
                    <td className="py-3.5 px-4 text-slate-600">{a.department}</td>
                    <td className="py-3.5 px-4 text-slate-600 font-mono text-xs">
                      {new Date(a.scheduledAtUtc).toLocaleString()}
                    </td>
                    <td className="py-3.5 px-4">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${
                        a.status === 'CheckedIn' ? 'bg-emerald-100 text-emerald-800' :
                        a.status === 'Booked' ? 'bg-blue-100 text-blue-800' :
                        'bg-slate-100 text-slate-700'
                      }`}>
                        {a.status}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 font-mono font-bold text-brand-600">{a.checkInCode}</td>
                    <td className="py-3.5 px-4 text-right space-x-2">
                      {a.status === 'Booked' && hasPermission('appointments.checkin') && (
                        <button
                          onClick={() => handleCheckIn(a.checkInCode)}
                          className="px-2.5 py-1 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg text-xs font-medium"
                        >
                          Check In
                        </button>
                      )}
                      {a.status === 'Booked' && hasPermission('appointments.cancel') && (
                        <button
                          onClick={() => handleCancel(a.id)}
                          className="px-2.5 py-1 bg-red-50 hover:bg-red-100 text-red-700 rounded-lg text-xs font-medium"
                        >
                          Cancel
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Booking Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-4">Book Clinical Consultation</h3>
            <form onSubmit={handleBook} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Select Patient</label>
                <select
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={form.patientId}
                  onChange={(e) => setForm({ ...form, patientId: parseInt(e.target.value) })}
                >
                  {patients.map(p => (
                    <option key={p.patientId} value={p.patientId}>{p.fullName} ({p.mrn})</option>
                  ))}
                  {patients.length === 0 && <option value={1}>Eric Mugabe (MRN-2026-0012)</option>}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Assigned Doctor</label>
                <select
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={form.doctorUserId}
                  onChange={(e) => setForm({ ...form, doctorUserId: e.target.value })}
                  required
                >
                  {doctors.map(d => (
                    <option key={d.userId} value={d.userId}>{d.displayName}</option>
                  ))}
                  {doctors.length === 0 && <option value="">No doctors available</option>}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Department</label>
                <input
                  type="text"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={form.department}
                  onChange={(e) => setForm({ ...form, department: e.target.value })}
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Consultation Date & Time</label>
                <input
                  type="datetime-local"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={form.scheduledAtUtc}
                  onChange={(e) => setForm({ ...form, scheduledAtUtc: e.target.value })}
                  required
                />
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
                  Confirm Booking
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
