import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { TrendingUp, AlertTriangle, ShieldAlert, Play, Calendar, BarChart3, Clock, CheckCircle2 } from 'lucide-react';

export default function Occupancy() {
  const [forecasts, setForecasts] = useState([]);
  const [surgeAlerts, setSurgeAlerts] = useState([]);
  const [snapshots, setSnapshots] = useState([]);
  const [selectedWard, setSelectedWard] = useState('Intensive Care Unit (ICU)');
  const [loading, setLoading] = useState(true);
  const [runningSnapshot, setRunningSnapshot] = useState(false);

  const loadData = async () => {
    try {
      const [fRes, aRes, sRes] = await Promise.all([
        api.get('/occupancy/forecast', { params: { ward: selectedWard, horizonDays: 14 } }),
        api.get('/occupancy/surge-alerts'),
        api.get('/occupancy/snapshots', { params: { ward: selectedWard, days: 14 } })
      ]);
      setForecasts(fRes.data);
      setSurgeAlerts(aRes.data);
      setSnapshots(sRes.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [selectedWard]);

  const handleRunSnapshotNow = async () => {
    setRunningSnapshot(true);
    try {
      await api.post('/occupancy/snapshots/run');
      await loadData();
      alert('Daily occupancy snapshot job executed successfully!');
    } catch (err) {
      alert(err.response?.data?.message || 'Error executing snapshot');
    } finally {
      setRunningSnapshot(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <TrendingUp className="w-7 h-7 text-brand-600" />
            Predictive Bed Occupancy & Surge Analytics
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-11 · 7–14 Day Bed Forecasting, Weekday Moving Average Heuristics & 85% Surge Alerts
          </p>
        </div>
        <button
          onClick={handleRunSnapshotNow}
          disabled={runningSnapshot}
          className="inline-flex items-center gap-2 bg-slate-800 hover:bg-slate-900 disabled:bg-slate-400 text-white px-4 py-2.5 rounded-xl font-medium shadow-sm transition"
        >
          <Play className="w-4 h-4 text-emerald-400" />
          {runningSnapshot ? 'Capturing Snapshot...' : 'Run Daily Snapshot Job'}
        </button>
      </div>

      {/* Surge Alert Banner (FR-OCC-004) */}
      {surgeAlerts.length > 0 && (
        <div className="bg-red-50 border-2 border-red-300 rounded-2xl p-5 shadow-sm animate-in fade-in">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-bold text-red-900 uppercase tracking-wide flex items-center gap-2">
              <ShieldAlert className="w-5 h-5 text-red-600" />
              SURGE ALERT TRIGGERED (&ge; 85% Predicted Capacity)
            </h3>
            <span className="text-xs bg-red-200 text-red-900 font-bold px-2.5 py-0.5 rounded-full">
              {surgeAlerts.length} surge days detected
            </span>
          </div>
          <div className="space-y-2">
            {surgeAlerts.slice(0, 3).map((a, i) => (
              <div key={i} className="p-3 bg-white rounded-xl border border-red-200 text-xs flex flex-col sm:flex-row sm:items-center justify-between gap-2 shadow-xs">
                <div className="flex items-center gap-2">
                  <span className="font-bold text-slate-900">{a.wardName}</span>
                  <span className="text-slate-400">·</span>
                  <span className="font-medium text-slate-600">Date: {new Date(a.forecastDate).toLocaleDateString()}</span>
                  <span className="text-red-600 font-bold font-mono">({a.predictedOccupancyPercent}% occupancy)</span>
                </div>
                <span className="text-red-700 font-semibold">{a.recommendedAction}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Ward Selector & Horizon */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
          <div>
            <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <BarChart3 className="w-5 h-5 text-brand-600" />
              14-Day Forward Bed Capacity Projection
            </h2>
            <span className="text-xs text-slate-400 mt-0.5 block">
              Method: <strong>Weekday Moving Average Heuristic (v1.2-baseline)</strong> per Excel sprint plan decision D7
            </span>
          </div>

          <div className="flex items-center gap-3">
            <label className="text-xs font-semibold text-slate-600">Select Ward:</label>
            <select
              value={selectedWard}
              onChange={(e) => setSelectedWard(e.target.value)}
              className="px-3.5 py-2 border border-slate-200 rounded-xl text-xs bg-white font-medium shadow-xs"
            >
              <option value="Intensive Care Unit (ICU)">Intensive Care Unit (ICU)</option>
              <option value="Surgical Ward">Surgical Ward</option>
              <option value="General Medicine">General Medicine</option>
              <option value="Maternity Ward">Maternity Ward</option>
            </select>
          </div>
        </div>

        {/* Visual Forecast Bars */}
        <div className="space-y-3">
          {forecasts.map((f, idx) => {
            const isAlert = f.isSurgeAlert;
            return (
              <div
                key={idx}
                className={`p-3.5 rounded-xl border transition ${
                  isAlert ? 'bg-red-50/60 border-red-200' : 'bg-slate-50 border-slate-200'
                }`}
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    <span className="font-bold text-xs text-slate-800 w-28">
                      {new Date(f.forecastDate).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })}
                    </span>
                    <span className="text-xs text-slate-500 font-mono">
                      Expected: <strong className={isAlert ? 'text-red-600 font-bold' : 'text-slate-900'}>{f.predictedOccupancyPercent}%</strong>
                    </span>
                    <span className="text-[11px] text-slate-400">
                      (Range: {f.lowerBoundPercent}% - {f.upperBoundPercent}%)
                    </span>
                  </div>
                  {isAlert && (
                    <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-red-100 text-red-800 border border-red-300">
                      ⚠️ Surge &ge; 85%
                    </span>
                  )}
                </div>

                {/* Progress Bar with 85% Threshold Line */}
                <div className="relative w-full h-3.5 bg-slate-200 rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all duration-500 ${
                      isAlert ? 'bg-red-500' : f.predictedOccupancyPercent >= 75 ? 'bg-amber-500' : 'bg-brand-500'
                    }`}
                    style={{ width: `${Math.min(f.predictedOccupancyPercent, 100)}%` }}
                  />
                  {/* 85% Marker */}
                  <div
                    className="absolute top-0 bottom-0 w-0.5 bg-red-700 z-10"
                    style={{ left: '85%' }}
                    title="85% Surge Threshold"
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Historical Daily Snapshots (FR-OCC-001) */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <h2 className="text-base font-bold text-slate-900 flex items-center gap-2 mb-4">
          <Calendar className="w-5 h-5 text-indigo-600" />
          Recent Historical Occupancy Snapshots (Last 14 Days)
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-7 gap-3">
          {snapshots.map((s) => (
            <div key={s.id} className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-center">
              <span className="text-[10px] text-slate-500 block font-semibold">
                {new Date(s.snapshotDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
              </span>
              <span className="text-lg font-bold text-slate-900 font-mono mt-0.5 block">
                {s.occupancyPercentage}%
              </span>
              <span className="text-[10px] text-slate-400">
                {s.occupiedBeds}/{s.totalBeds} beds
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
