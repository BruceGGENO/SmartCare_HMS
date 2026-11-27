import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { Smartphone, QrCode, FileText, Package, Calendar, Clock, CheckCircle2, ShieldCheck, User } from 'lucide-react';

export default function Portal() {
  const [profile, setProfile] = useState(null);
  const [appointments, setAppointments] = useState([]);
  const [packages, setPackages] = useState([]);
  const [notes, setNotes] = useState([]);
  const [loading, setLoading] = useState(true);

  const loadPortalData = async () => {
    try {
      const [appRes, pkgRes, noteRes] = await Promise.all([
        api.get('/portal/appointments'),
        api.get('/portal/packages'),
        api.get('/portal/released-notes')
      ]);
      setAppointments(appRes.data);
      setPackages(pkgRes.data);
      setNotes(noteRes.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPortalData();
  }, []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Smartphone className="w-7 h-7 text-brand-600" />
            Patient Self-Service Micro-Portal
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-10 · Fast QR Check-In, Treatment History, Prepaid Package Sessions & Released Notes
          </p>
        </div>
        <span className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
          <ShieldCheck className="w-4 h-4 text-emerald-600" />
          Verified Patient Session
        </span>
      </div>

      {/* Top Section: QR Check-In Card & Packages */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* QR Check-In Card (FR-POR-002) */}
        <div className="md:col-span-5 bg-linear-to-br from-brand-900 via-slate-900 to-slate-900 text-white p-6 rounded-2xl shadow-xl flex flex-col justify-between border border-brand-800/40">
          <div>
            <div className="flex items-center justify-between mb-4">
              <span className="text-xs uppercase font-bold tracking-widest text-brand-300">
                Self-Service Digital Pass
              </span>
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-brand-500/20 text-brand-300 border border-brand-500/30">
                Live Pass
              </span>
            </div>

            <div className="flex items-center gap-4 mb-5">
              {/* Simulated QR Matrix (Excel Decision D1) */}
              <div className="w-24 h-24 bg-white p-2 rounded-xl flex items-center justify-center shrink-0 shadow-md">
                <QrCode className="w-20 h-20 text-slate-900" />
              </div>
              <div>
                <span className="text-xs text-slate-400 block">Check-in Scan Code</span>
                <span className="text-2xl font-mono font-bold text-white tracking-wider">
                  {appointments.length > 0 ? (appointments[0].checkInCode || 'CHK-8921') : 'CHK-8921'}
                </span>
                <p className="text-[11px] text-brand-300 mt-1">
                  Present at front-desk kiosk or scan directly to join the live waiting queue.
                </p>
              </div>
            </div>
          </div>

          <div className="pt-3 border-t border-slate-800 flex items-center justify-between text-xs text-slate-400">
            <span>Patient: <strong>Demo Patient (SC-0001)</strong></span>
            <span>Valid today</span>
          </div>
        </div>

        {/* Prepaid Treatment Packages (FR-POR-004) */}
        <div className="md:col-span-7 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col justify-between">
          <div>
            <h2 className="text-base font-bold text-slate-900 flex items-center gap-2 mb-3">
              <Package className="w-5 h-5 text-brand-600" />
              My Purchased Treatment Packages
            </h2>
            <div className="space-y-3">
              {packages.length === 0 ? (
                <div className="p-6 text-center text-xs text-slate-400">No active treatment packages found.</div>
              ) : (
                packages.map((pkg) => (
                  <div key={pkg.id} className="p-3.5 rounded-xl border border-slate-200 bg-slate-50 flex items-center justify-between">
                    <div>
                      <span className="font-bold text-sm text-slate-900 block">{pkg.packageName}</span>
                      <span className="text-xs text-slate-500">
                        Purchased: {pkg.totalSessions} sessions · Consumed: {pkg.consumedSessions}
                      </span>
                    </div>
                    <div className="text-right">
                      <span className="text-xl font-bold text-brand-600 font-mono">
                        {pkg.remainingSessions}
                      </span>
                      <span className="block text-[10px] text-slate-400 font-medium">sessions left</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
          <div className="text-[11px] text-slate-400 mt-3 pt-2 border-t border-slate-100">
            Sessions decrease automatically as clinical consultations or therapy sessions are consumed.
          </div>
        </div>
      </div>

      {/* Bottom Section: Appointments & Released Doctor Notes */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Appointments List */}
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <h2 className="text-base font-bold text-slate-900 flex items-center gap-2 mb-4">
            <Calendar className="w-5 h-5 text-indigo-600" />
            My Hospital Appointments
          </h2>
          <div className="space-y-3">
            {appointments.length === 0 ? (
              <p className="text-xs text-slate-400 italic">No appointments booked.</p>
            ) : (
              appointments.map((a) => (
                <div key={a.id} className="p-3.5 rounded-xl border border-slate-200 bg-slate-50/50 flex items-center justify-between">
                  <div>
                    <span className="font-bold text-sm text-slate-900 block">{a.doctorName}</span>
                    <span className="text-xs text-slate-500">{a.department} · {a.slotTime}</span>
                  </div>
                  <div className="text-right">
                    <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-brand-100 text-brand-800">
                      {a.status}
                    </span>
                    <span className="block text-[10px] font-mono text-slate-400 mt-1">{a.checkInCode}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Released Doctor Notes (FR-POR-005) */}
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
          <h2 className="text-base font-bold text-slate-900 flex items-center gap-2 mb-4">
            <FileText className="w-5 h-5 text-emerald-600" />
            Released Physician Clinical Notes
          </h2>
          <div className="space-y-3">
            {notes.length === 0 ? (
              <p className="text-xs text-slate-400 italic">No notes released by your physician yet.</p>
            ) : (
              notes.map((n) => (
                <div key={n.id} className="p-3.5 rounded-xl border border-emerald-100 bg-emerald-50/40 space-y-1">
                  <div className="flex items-center justify-between text-xs text-emerald-900 font-semibold">
                    <span>{n.doctorName}</span>
                    <span className="text-[10px] text-slate-400">{new Date(n.releasedAtUtc).toLocaleDateString()}</span>
                  </div>
                  <p className="text-xs text-slate-700 font-medium">
                    "{n.noteText}"
                  </p>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
