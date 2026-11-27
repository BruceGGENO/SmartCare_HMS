import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { Clock, AlertTriangle, UserCheck, Flame, Plus } from 'lucide-react';

export default function WaitingRoom() {
  const [queue, setQueue] = useState([]);
  const [analytics, setAnalytics] = useState([]);
  const [heatmap, setHeatmap] = useState([]);
  const [loading, setLoading] = useState(true);
  const [stationFilter, setStationFilter] = useState('');
  const [showEventModal, setShowEventModal] = useState(false);
  const [newEvent, setNewEvent] = useState({
    patientName: '',
    patientMrn: '',
    station: 'Registration',
    eventType: 'Enter'
  });
  const { hasPermission } = useAuth();

  const loadData = async () => {
    setLoading(true);
    try {
      const qRes = await api.get(`/waitingroom/live-queue${stationFilter ? `?station=${stationFilter}` : ''}`);
      setQueue(qRes.data || []);

      const aRes = await api.get('/waitingroom/analytics');
      setAnalytics(aRes.data || []);

      const hRes = await api.get('/waitingroom/heatmap');
      setHeatmap(hRes.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 20000);
    return () => clearInterval(interval);
  }, [stationFilter]);

  const handleRecordEvent = async (e) => {
    e.preventDefault();
    try {
      await api.post('/waitingroom/events', newEvent);
      setShowEventModal(false);
      setNewEvent({ patientName: '', patientMrn: '', station: 'Registration', eventType: 'Enter' });
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Failed to record event');
    }
  };

  const stations = ['Registration', 'Triage', 'Doctor', 'Laboratory', 'Pharmacy', 'Billing'];

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Waiting Room & Bottlenecks (MOD-09)</h1>
          <p className="text-sm text-slate-500">Real-time patient journey tracking and queue analytics</p>
        </div>
        {hasPermission('waitingroom.manage') && (
          <button
            onClick={() => setShowEventModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow-sm transition"
          >
            <Plus className="w-4 h-4" />
            <span>Record Station Event</span>
          </button>
        )}
      </div>

      {/* Bottleneck Warning Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {analytics.filter(a => a.isBottleneck).map(b => (
          <div key={b.station} className="p-4 bg-amber-50 rounded-2xl border border-amber-200 flex items-start gap-3">
            <div className="p-2 bg-amber-100 text-amber-700 rounded-xl">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <p className="text-xs uppercase font-bold text-amber-800 tracking-wider">Bottleneck Detected</p>
              <p className="text-base font-bold text-slate-900">{b.station} Station</p>
              <p className="text-xs text-amber-700 mt-1">
                Average wait: <span className="font-bold">{b.avgMinutes} min</span> (Target: &lt; 30 min)
              </p>
            </div>
          </div>
        ))}
        {analytics.filter(a => a.isBottleneck).length === 0 && (
          <div className="col-span-3 p-4 bg-emerald-50 rounded-2xl border border-emerald-200 flex items-center gap-3">
            <UserCheck className="w-5 h-5 text-emerald-600" />
            <span className="text-sm font-medium text-emerald-800">All hospital service stations are operating within standard wait targets (&lt;30 min).</span>
          </div>
        )}
      </div>

      {/* Live Waiting Patients Queue */}
      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Clock className="w-5 h-5 text-brand-600" />
            <span className="font-bold text-slate-800">Active Queue</span>
            <span className="text-xs bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full font-semibold">{queue.length} patients waiting</span>
          </div>
          <div className="flex gap-1 bg-slate-100 p-1 rounded-xl">
            <button
              onClick={() => setStationFilter('')}
              className={`px-3 py-1 rounded-lg text-xs font-semibold ${!stationFilter ? 'bg-white shadow text-slate-900' : 'text-slate-600'}`}
            >
              All
            </button>
            {stations.map(s => (
              <button
                key={s}
                onClick={() => setStationFilter(s)}
                className={`px-2.5 py-1 rounded-lg text-xs font-semibold ${stationFilter === s ? 'bg-white shadow text-slate-900' : 'text-slate-600'}`}
              >
                {s}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="p-10 text-center text-slate-400 text-sm">Loading queue...</div>
        ) : queue.length === 0 ? (
          <div className="p-10 text-center text-slate-400 text-sm">No patients currently queued at this station.</div>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-semibold border-b border-slate-100">
              <tr>
                <th className="py-3 px-4">Patient</th>
                <th className="py-3 px-4">Current Station</th>
                <th className="py-3 px-4">Wait Time</th>
                <th className="py-3 px-4">Arrival Timestamp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {queue.map(q => (
                <tr key={q.id} className="hover:bg-slate-50/50">
                  <td className="py-3 px-4 font-semibold text-slate-900">
                    {q.patientName} <span className="text-xs font-normal text-slate-400">({q.patientMrn})</span>
                  </td>
                  <td className="py-3 px-4">
                    <span className="px-2 py-0.5 rounded-md text-xs font-semibold bg-brand-50 text-brand-700 border border-brand-200">
                      {q.station}
                    </span>
                  </td>
                  <td className="py-3 px-4">
                    <span className={`font-mono font-bold text-xs ${q.waitMinutes > 30 ? 'text-red-600' : 'text-slate-700'}`}>
                      {q.waitMinutes} mins
                    </span>
                  </td>
                  <td className="py-3 px-4 font-mono text-xs text-slate-500">
                    {new Date(q.occurredAtUtc).toLocaleTimeString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Heatmap Section */}
      <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
        <div className="flex items-center gap-2 mb-4">
          <Flame className="w-5 h-5 text-orange-500" />
          <h2 className="font-bold text-slate-900">Station Waiting Heatmap (Hours 08:00 - 18:00)</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-center text-xs">
            <thead>
              <tr className="border-b border-slate-200 text-slate-500">
                <th className="py-2 text-left w-28">Station</th>
                {Array.from({ length: 11 }, (_, i) => i + 8).map(h => (
                  <th key={h} className="py-2">{h}:00</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {stations.map(station => (
                <tr key={station}>
                  <td className="py-2.5 text-left font-semibold text-slate-700">{station}</td>
                  {Array.from({ length: 11 }, (_, i) => i + 8).map(h => {
                    const cell = heatmap.find(c => c.station === station && c.hourOfDay === h);
                    const wait = cell ? cell.avgWaitMinutes : 15;
                    const bgClass =
                      wait > 35 ? 'bg-red-500 text-white font-bold' :
                      wait > 25 ? 'bg-amber-400 text-slate-900 font-semibold' :
                      wait > 15 ? 'bg-blue-200 text-slate-800' :
                      'bg-emerald-100 text-emerald-800';
                    return (
                      <td key={h} className="p-1">
                        <div className={`py-1 rounded text-[11px] ${bgClass}`}>
                          {wait}m
                        </div>
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal */}
      {showEventModal && (
        <div className="fixed inset-0 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100">
            <h3 className="text-lg font-bold text-slate-900 mb-4">Record Patient Station Movement</h3>
            <form onSubmit={handleRecordEvent} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Eric Mugabe"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newEvent.patientName}
                  onChange={e => setNewEvent({ ...newEvent, patientName: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Patient MRN</label>
                <input
                  type="text"
                  required
                  placeholder="MRN-2026-0012"
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newEvent.patientMrn}
                  onChange={e => setNewEvent({ ...newEvent, patientMrn: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Station</label>
                <select
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newEvent.station}
                  onChange={e => setNewEvent({ ...newEvent, station: e.target.value })}
                >
                  {stations.map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Event Type</label>
                <select
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={newEvent.eventType}
                  onChange={e => setNewEvent({ ...newEvent, eventType: e.target.value })}
                >
                  <option value="Enter">Enter Station (Start Waiting / In Service)</option>
                  <option value="Leave">Leave Station (Completed Service)</option>
                </select>
              </div>

              <div className="flex gap-2 pt-4">
                <button
                  type="button"
                  onClick={() => setShowEventModal(false)}
                  className="flex-1 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-sm font-semibold"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold"
                >
                  Save Event
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
