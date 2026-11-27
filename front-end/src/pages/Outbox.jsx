import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { Mail, MessageSquare, Send, CheckCircle2, Search, Clock } from 'lucide-react';

export default function Outbox() {
  const [messages, setMessages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filterChannel, setFilterChannel] = useState('');

  const loadOutbox = async () => {
    try {
      const res = await api.get('/outbox');
      setMessages(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOutbox();
  }, []);

  const filtered = filterChannel
    ? messages.filter((m) => m.channel.toLowerCase() === filterChannel.toLowerCase())
    : messages;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Send className="w-7 h-7 text-brand-600" />
            Outbox Stub & Delivery Audit
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Pluggable SMS & Email Notifications Gateway · Auditable Delivery Outbox Proof
          </p>
        </div>
        <div className="flex items-center gap-2">
          <select
            value={filterChannel}
            onChange={(e) => setFilterChannel(e.target.value)}
            className="px-3 py-1.5 border border-slate-200 rounded-xl text-xs bg-white font-medium"
          >
            <option value="">All Channels (SMS & Email)</option>
            <option value="SMS">SMS Only</option>
            <option value="Email">Email Only</option>
          </select>
        </div>
      </div>

      {/* Outbox Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 border-b border-slate-200 text-xs uppercase font-semibold text-slate-500 tracking-wider">
              <tr>
                <th className="px-6 py-3.5">ID / Channel</th>
                <th className="px-6 py-3.5">Recipient</th>
                <th className="px-6 py-3.5">Subject & Content</th>
                <th className="px-6 py-3.5">Delivery Status</th>
                <th className="px-6 py-3.5">Timestamp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan="5" className="p-8 text-center text-xs text-slate-400">Loading outbox messages...</td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan="5" className="p-8 text-center text-xs text-slate-400">No dispatched outbox records found.</td>
                </tr>
              ) : (
                filtered.map((m) => (
                  <tr key={m.outboxId} className="hover:bg-slate-50 transition">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        {m.channel === 'SMS' ? (
                          <MessageSquare className="w-4 h-4 text-emerald-600" />
                        ) : (
                          <Mail className="w-4 h-4 text-indigo-600" />
                        )}
                        <span className="font-bold text-xs text-slate-900 font-mono">
                          #{m.outboxId} ({m.channel})
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 font-mono text-xs font-semibold text-slate-800">
                      {m.recipient}
                    </td>
                    <td className="px-6 py-4">
                      <span className="font-semibold text-slate-900 block text-xs">{m.subject}</span>
                      <p className="text-xs text-slate-600 mt-0.5 line-clamp-2">{m.body}</p>
                    </td>
                    <td className="px-6 py-4">
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                        <CheckCircle2 className="w-3 h-3" />
                        {m.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-xs text-slate-400 font-mono">
                      {new Date(m.createdAtUtc).toLocaleString()}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
