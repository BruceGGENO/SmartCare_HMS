import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { Shield, Search, RefreshCw } from 'lucide-react';

export default function Audit() {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/audit');
      setLogs(res.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Security & Clinical Audit Trail</h1>
          <p className="text-sm text-slate-500">Immutable chronological log of all sensitive user and system events</p>
        </div>
        <button
          onClick={loadData}
          className="flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 rounded-xl text-sm font-semibold shadow-sm transition"
        >
          <RefreshCw className="w-4 h-4 text-slate-500" />
          <span>Refresh Audit</span>
        </button>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-100 font-semibold text-sm text-slate-700">
          Recent Security Audit Events ({logs.length})
        </div>
        {loading ? (
          <div className="p-12 text-center text-slate-400 text-sm">Loading security events...</div>
        ) : logs.length === 0 ? (
          <div className="p-12 text-center text-slate-400 text-sm">No audit logs recorded yet.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-slate-500 text-xs uppercase font-semibold border-b border-slate-100">
                <tr>
                  <th className="py-3 px-4">Timestamp (UTC)</th>
                  <th className="py-3 px-4">Action</th>
                  <th className="py-3 px-4">Entity Type</th>
                  <th className="py-3 px-4">Entity ID</th>
                  <th className="py-3 px-4">Actor ID</th>
                  <th className="py-3 px-4">Outcome</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-mono text-xs">
                {logs.map((log) => (
                  <tr key={log.auditLogId} className="hover:bg-slate-50/50">
                    <td className="py-3 px-4 text-slate-500">
                      {new Date(log.occurredAtUtc).toLocaleString()}
                    </td>
                    <td className="py-3 px-4 font-semibold text-brand-600">{log.action}</td>
                    <td className="py-3 px-4 text-slate-700">{log.entityType}</td>
                    <td className="py-3 px-4 text-slate-400 truncate max-w-[120px]">{log.entityId || '—'}</td>
                    <td className="py-3 px-4 text-slate-500 truncate max-w-[120px]">{log.actorUserId || 'Anonymous'}</td>
                    <td className="py-3 px-4">
                      <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase ${
                        log.outcome === 'Success' ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-800'
                      }`}>
                        {log.outcome}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
