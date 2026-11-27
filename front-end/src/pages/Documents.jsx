import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { FileText, Upload, Download, FileCheck } from 'lucide-react';

export default function Documents() {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(false);
  const [file, setFile] = useState(null);
  const [entityType, setEntityType] = useState('Patient');
  const [entityId, setEntityId] = useState('1');
  const [uploading, setUploading] = useState(false);
  const { hasPermission } = useAuth();

  const handleUpload = async (e) => {
    e.preventDefault();
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);
    formData.append('entityType', entityType);
    formData.append('entityId', entityId);

    try {
      const res = await api.post('/documents/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      alert('Document securely stored!');
      setDocuments(prev => [res.data, ...prev]);
      setFile(null);
    } catch (err) {
      alert(err.response?.data?.message || 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (docId, fileName) => {
    try {
      const res = await api.get(`/documents/${docId}`, { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert('Download failed');
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Secure Document Storage (MOD-06)</h1>
        <p className="text-sm text-slate-500">Mukunzi secure local file storage with SQL metadata and tamper checks</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {hasPermission('documents.upload') && (
          <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm lg:col-span-1">
            <h3 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
              <Upload className="w-5 h-5 text-brand-600" />
              <span>Upload Document</span>
            </h3>
            <form onSubmit={handleUpload} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Entity Type</label>
                <select
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={entityType}
                  onChange={e => setEntityType(e.target.value)}
                >
                  <option value="Patient">Patient File</option>
                  <option value="Lab">Lab Diagnostic</option>
                  <option value="Invoice">Billing Receipt</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Entity ID (e.g. Patient ID)</label>
                <input
                  type="text"
                  required
                  className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  value={entityId}
                  onChange={e => setEntityId(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Choose File</label>
                <input
                  type="file"
                  required
                  onChange={e => setFile(e.target.files[0])}
                  className="w-full text-xs text-slate-500 file:mr-3 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-xs file:font-semibold file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100"
                />
              </div>

              <button
                type="submit"
                disabled={uploading || !file}
                className="w-full py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow transition disabled:opacity-50"
              >
                {uploading ? 'Storing Document...' : 'Upload & Encrypt'}
              </button>
            </form>
          </div>
        )}

        <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm lg:col-span-2">
          <div className="p-4 border-b border-slate-100 font-semibold text-sm text-slate-700">
            Uploaded Documents in Current Session
          </div>
          {documents.length === 0 ? (
            <div className="p-16 text-center text-slate-400 text-sm">
              No documents uploaded in this browser session. Upload one using the form on the left.
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {documents.map((d) => (
                <div key={d.documentId} className="p-4 flex items-center justify-between hover:bg-slate-50">
                  <div className="flex items-center gap-3">
                    <FileCheck className="w-6 h-6 text-emerald-600" />
                    <div>
                      <p className="font-semibold text-slate-900 text-sm">{d.originalFileName}</p>
                      <p className="text-xs text-slate-400 font-mono">
                        {d.contentType} • {(d.sizeBytes / 1024).toFixed(1)} KB • {d.entityType} #{d.entityId}
                      </p>
                    </div>
                  </div>
                  <button
                    onClick={() => handleDownload(d.documentId, d.originalFileName)}
                    className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-semibold"
                  >
                    <Download className="w-3.5 h-3.5" />
                    <span>Download</span>
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
