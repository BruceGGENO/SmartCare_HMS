import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { UserCheck, Search, Plus, AlertTriangle, ShieldAlert, HeartPulse, FileText, CheckCircle2, X } from 'lucide-react';

export default function Patients() {
  const [patients, setPatients] = useState([]);
  const [selectedPatient, setSelectedPatient] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [showRegisterModal, setShowRegisterModal] = useState(false);
  const [duplicateWarning, setDuplicateWarning] = useState(null);
  const [registerForm, setRegisterForm] = useState({
    firstName: '',
    lastName: '',
    dateOfBirth: '1995-05-15',
    gender: 'Female',
    phoneNumber: '',
    email: '',
    address: 'Kigali, Rwanda',
    bloodGroup: 'O+'
  });

  // Allergy and diagnosis sub-forms
  const [newAllergy, setNewAllergy] = useState({ substance: '', severity: 'Moderate', reaction: '' });
  const [newDiagnosis, setNewDiagnosis] = useState({ icdCode: '', description: '' });
  const [newPlan, setNewPlan] = useState({ goals: '', medications: '', followUpDate: '' });

  const loadPatients = async () => {
    setLoading(true);
    try {
      const res = await api.get('/patients', { params: { query: searchQuery } });
      setPatients(res.data);
      if (res.data.length > 0 && !selectedPatient) {
        loadPatientDetail(res.data[0].id);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadPatientDetail = async (id) => {
    try {
      const res = await api.get(`/patients/${id}`);
      setSelectedPatient(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    loadPatients();
  }, [searchQuery]);

  const handleCheckDuplicate = async () => {
    try {
      const res = await api.post('/patients/check-duplicate', registerForm);
      if (res.data.isDuplicate) {
        setDuplicateWarning(res.data.message);
      } else {
        setDuplicateWarning(null);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleRegisterSubmit = async (e) => {
    e.preventDefault();
    try {
      const res = await api.post('/patients', registerForm);
      setShowRegisterModal(false);
      setDuplicateWarning(null);
      setRegisterForm({
        firstName: '',
        lastName: '',
        dateOfBirth: '1995-05-15',
        gender: 'Female',
        phoneNumber: '',
        email: '',
        address: 'Kigali, Rwanda',
        bloodGroup: 'O+'
      });
      await loadPatients();
      loadPatientDetail(res.data.id);
    } catch (err) {
      alert(err.response?.data?.message || 'Error registering patient');
    }
  };

  const handleAddAllergy = async (e) => {
    e.preventDefault();
    if (!selectedPatient || !newAllergy.substance) return;
    try {
      await api.post(`/patients/${selectedPatient.id}/allergies`, newAllergy);
      setNewAllergy({ substance: '', severity: 'Moderate', reaction: '' });
      loadPatientDetail(selectedPatient.id);
    } catch (err) {
      alert(err.response?.data?.message || 'Error adding allergy');
    }
  };

  const handleAddDiagnosis = async (e) => {
    e.preventDefault();
    if (!selectedPatient || !newDiagnosis.icdCode) return;
    try {
      await api.post(`/patients/${selectedPatient.id}/diagnoses`, newDiagnosis);
      setNewDiagnosis({ icdCode: '', description: '' });
      loadPatientDetail(selectedPatient.id);
    } catch (err) {
      alert(err.response?.data?.message || 'Error recording diagnosis');
    }
  };

  const handleSavePlan = async (e) => {
    e.preventDefault();
    if (!selectedPatient) return;
    try {
      await api.post(`/patients/${selectedPatient.id}/treatment-plans`, newPlan);
      setNewPlan({ goals: '', medications: '', followUpDate: '' });
      loadPatientDetail(selectedPatient.id);
    } catch (err) {
      alert(err.response?.data?.message || 'Error saving treatment plan');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <UserCheck className="w-7 h-7 text-brand-600" />
            Electronic Health Records (EHR)
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            MOD-01 · Longitudinal Patient Demographics, Allergy Banners, ICD Diagnoses & Treatment Plans
          </p>
        </div>
        <button
          onClick={() => setShowRegisterModal(true)}
          className="inline-flex items-center gap-2 bg-brand-600 hover:bg-brand-700 text-white px-4 py-2.5 rounded-xl font-medium shadow-sm transition"
        >
          <Plus className="w-4 h-4" />
          Register Patient
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Patient List (Left 4 cols) */}
        <div className="lg:col-span-4 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden flex flex-col h-[750px]">
          <div className="p-4 border-b border-slate-200">
            <div className="relative">
              <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Search patient, MRN, phone..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-9 pr-4 py-2 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
            {loading ? (
              <div className="p-8 text-center text-sm text-slate-400">Loading patients...</div>
            ) : patients.length === 0 ? (
              <div className="p-8 text-center text-sm text-slate-400">No patients found</div>
            ) : (
              patients.map((p) => {
                const isSelected = selectedPatient?.id === p.id;
                return (
                  <div
                    key={p.id}
                    onClick={() => loadPatientDetail(p.id)}
                    className={`p-4 cursor-pointer transition ${
                      isSelected ? 'bg-brand-50/70 border-l-4 border-brand-600' : 'hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-sm text-slate-900">{p.fullName}</span>
                      <span className="text-xs font-mono font-medium px-2 py-0.5 rounded bg-slate-100 text-slate-700">
                        {p.mrn}
                      </span>
                    </div>
                    <div className="text-xs text-slate-500 mt-1 flex items-center justify-between">
                      <span>Phone: {p.phoneNumber}</span>
                      <span>DOB: {new Date(p.dateOfBirth).toLocaleDateString()}</span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Patient EHR Detail (Right 8 cols) */}
        <div className="lg:col-span-8 space-y-6">
          {selectedPatient ? (
            <>
              {/* Allergy Banner (FR-EHR-003) */}
              {selectedPatient.allergies && selectedPatient.allergies.length > 0 ? (
                <div className="p-4 bg-red-50 border border-red-200 rounded-2xl flex items-start gap-3 shadow-sm">
                  <ShieldAlert className="w-6 h-6 text-red-600 shrink-0 mt-0.5" />
                  <div className="flex-1">
                    <div className="flex items-center justify-between">
                      <h3 className="text-sm font-bold text-red-800 uppercase tracking-wide">
                        ⚠️ High-Risk Clinical Allergy Alert
                      </h3>
                      <span className="text-xs text-red-600 font-medium">Prescribing contraindicated items blocked</span>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {selectedPatient.allergies.map((a) => (
                        <span
                          key={a.id}
                          className="px-2.5 py-1 rounded-lg text-xs font-semibold bg-red-100 text-red-900 border border-red-300"
                        >
                          {a.substance} ({a.severity}) — {a.reaction}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-xs text-emerald-800 flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                  <span>No documented adverse drug allergies recorded for this patient.</span>
                </div>
              )}

              {/* Patient Profile Header Card */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-3">
                      <h2 className="text-xl font-bold text-slate-900">{selectedPatient.fullName}</h2>
                      <span className="text-xs font-mono font-bold bg-brand-100 text-brand-800 px-2.5 py-0.5 rounded-full">
                        {selectedPatient.mrn}
                      </span>
                    </div>
                    <p className="text-xs text-slate-500 mt-1">
                      DOB: {new Date(selectedPatient.dateOfBirth).toLocaleDateString()} · Blood Group: {selectedPatient.bloodGroup || 'O+'} · Gender: {selectedPatient.gender}
                    </p>
                  </div>
                  <div className="text-right text-xs text-slate-500">
                    <div>Contact: {selectedPatient.phoneNumber}</div>
                    <div>Address: {selectedPatient.address}</div>
                  </div>
                </div>
              </div>

              {/* Diagnoses & Treatment Plan */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Diagnoses with ICD Lookup (FR-EHR-004) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex flex-col justify-between">
                  <div>
                    <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wide flex items-center gap-2 mb-3">
                      <HeartPulse className="w-4 h-4 text-rose-500" />
                      Diagnoses (ICD-10 Encounters)
                    </h3>
                    <div className="space-y-2 mb-4 max-h-48 overflow-y-auto">
                      {selectedPatient.diagnoses && selectedPatient.diagnoses.length > 0 ? (
                        selectedPatient.diagnoses.map((d) => (
                          <div key={d.id} className="p-2.5 rounded-xl bg-slate-50 border border-slate-100 text-xs">
                            <div className="flex items-center justify-between">
                              <span className="font-mono font-bold text-slate-900 bg-slate-200 px-1.5 py-0.5 rounded text-[11px]">
                                {d.icdCode}
                              </span>
                              <span className="text-slate-400 text-[10px]">{new Date(d.diagnosedAtUtc).toLocaleDateString()}</span>
                            </div>
                            <p className="text-slate-700 mt-1 font-medium">{d.description}</p>
                            <span className="text-[10px] text-slate-400">By {d.diagnosedBy}</span>
                          </div>
                        ))
                      ) : (
                        <p className="text-xs text-slate-400 italic">No diagnoses recorded</p>
                      )}
                    </div>
                  </div>

                  {/* Add Diagnosis Quick Form */}
                  <form onSubmit={handleAddDiagnosis} className="pt-3 border-t border-slate-100 space-y-2">
                    <div className="flex gap-2">
                      <input
                        type="text"
                        placeholder="ICD Code (e.g. J06.9, I10)"
                        value={newDiagnosis.icdCode}
                        onChange={(e) => setNewDiagnosis({ ...newDiagnosis, icdCode: e.target.value })}
                        className="w-1/3 px-2 py-1.5 border border-slate-200 rounded-lg text-xs font-mono"
                        required
                      />
                      <input
                        type="text"
                        placeholder="Description (e.g. Hypertension)"
                        value={newDiagnosis.description}
                        onChange={(e) => setNewDiagnosis({ ...newDiagnosis, description: e.target.value })}
                        className="flex-1 px-2 py-1.5 border border-slate-200 rounded-lg text-xs"
                        required
                      />
                    </div>
                    <button type="submit" className="w-full py-1.5 bg-slate-800 hover:bg-slate-900 text-white rounded-lg text-xs font-medium">
                      + Add ICD Diagnosis
                    </button>
                  </form>
                </div>

                {/* Treatment Plans (FR-EHR-005) */}
                <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex flex-col justify-between">
                  <div>
                    <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wide flex items-center gap-2 mb-3">
                      <FileText className="w-4 h-4 text-brand-600" />
                      Active Treatment Plan
                    </h3>
                    <div className="space-y-3 mb-4 max-h-48 overflow-y-auto">
                      {selectedPatient.treatmentPlans && selectedPatient.treatmentPlans.length > 0 ? (
                        selectedPatient.treatmentPlans.map((tp) => (
                          <div key={tp.id} className="p-3 bg-brand-50/50 border border-brand-100 rounded-xl text-xs space-y-1">
                            <div className="flex justify-between items-center text-[10px] text-brand-700">
                              <span className="font-semibold">{tp.doctorName}</span>
                              <span>{new Date(tp.createdAtUtc).toLocaleDateString()}</span>
                            </div>
                            <p className="text-slate-800 font-medium">🎯 Goals: {tp.goals}</p>
                            <p className="text-slate-600">💊 Rx: {tp.medications}</p>
                            {tp.followUpDate && (
                              <p className="text-emerald-700 font-semibold text-[11px]">
                                📅 Next Follow-up: {new Date(tp.followUpDate).toLocaleDateString()}
                              </p>
                            )}
                          </div>
                        ))
                      ) : (
                        <p className="text-xs text-slate-400 italic">No treatment plan documented</p>
                      )}
                    </div>
                  </div>

                  {/* Save Plan Quick Form */}
                  <form onSubmit={handleSavePlan} className="pt-3 border-t border-slate-100 space-y-2">
                    <input
                      type="text"
                      placeholder="Clinical goals (e.g. Reduce BP < 130/80)"
                      value={newPlan.goals}
                      onChange={(e) => setNewPlan({ ...newPlan, goals: e.target.value })}
                      className="w-full px-2 py-1.5 border border-slate-200 rounded-lg text-xs"
                      required
                    />
                    <div className="flex gap-2">
                      <input
                        type="text"
                        placeholder="Prescribed medications"
                        value={newPlan.medications}
                        onChange={(e) => setNewPlan({ ...newPlan, medications: e.target.value })}
                        className="flex-1 px-2 py-1.5 border border-slate-200 rounded-lg text-xs"
                        required
                      />
                      <input
                        type="date"
                        value={newPlan.followUpDate}
                        onChange={(e) => setNewPlan({ ...newPlan, followUpDate: e.target.value })}
                        className="w-32 px-2 py-1.5 border border-slate-200 rounded-lg text-xs text-slate-600"
                      />
                    </div>
                    <button type="submit" className="w-full py-1.5 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-xs font-medium">
                      Save Treatment Plan
                    </button>
                  </form>
                </div>
              </div>

              {/* Add Allergy Section */}
              <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm">
                <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wide mb-3 flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 text-amber-500" />
                  Document Allergen / Adverse Reaction
                </h3>
                <form onSubmit={handleAddAllergy} className="grid grid-cols-1 sm:grid-cols-4 gap-3 items-end">
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-600 mb-1">Substance</label>
                    <input
                      type="text"
                      placeholder="e.g. Aspirin, Latex, Sulfa"
                      value={newAllergy.substance}
                      onChange={(e) => setNewAllergy({ ...newAllergy, substance: e.target.value })}
                      className="w-full px-3 py-1.5 border border-slate-200 rounded-lg text-xs"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-600 mb-1">Severity</label>
                    <select
                      value={newAllergy.severity}
                      onChange={(e) => setNewAllergy({ ...newAllergy, severity: e.target.value })}
                      className="w-full px-3 py-1.5 border border-slate-200 rounded-lg text-xs bg-white"
                    >
                      <option value="Mild">Mild</option>
                      <option value="Moderate">Moderate</option>
                      <option value="Severe">Severe (Anaphylaxis)</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-600 mb-1">Observed Reaction</label>
                    <input
                      type="text"
                      placeholder="e.g. Urticaria, Bronchospasm"
                      value={newAllergy.reaction}
                      onChange={(e) => setNewAllergy({ ...newAllergy, reaction: e.target.value })}
                      className="w-full px-3 py-1.5 border border-slate-200 rounded-lg text-xs"
                      required
                    />
                  </div>
                  <button type="submit" className="py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-xs font-semibold">
                    + Record Allergy
                  </button>
                </form>
              </div>
            </>
          ) : (
            <div className="bg-white p-12 text-center rounded-2xl border border-slate-200 text-slate-400">
              Select a patient from the list to view their longitudinal EHR record.
            </div>
          )}
        </div>
      </div>

      {/* Register Patient Modal with Duplicate Detection (FR-EHR-001) */}
      {showRegisterModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100 animate-in fade-in zoom-in-95">
            <div className="flex items-center justify-between mb-4 border-b border-slate-100 pb-3">
              <h3 className="text-lg font-bold text-slate-900 flex items-center gap-2">
                <UserCheck className="w-5 h-5 text-brand-600" />
                Register New Patient (FR-EHR-001)
              </h3>
              <button onClick={() => setShowRegisterModal(false)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            {duplicateWarning && (
              <div className="mb-4 p-3 bg-amber-50 border border-amber-300 rounded-xl text-amber-900 text-xs flex items-start gap-2">
                <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0 mt-0.5" />
                <div>
                  <span className="font-bold">Duplicate Warning:</span> {duplicateWarning}
                </div>
              </div>
            )}

            <form onSubmit={handleRegisterSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">First Name</label>
                  <input
                    type="text"
                    required
                    value={registerForm.firstName}
                    onBlur={handleCheckDuplicate}
                    onChange={(e) => setRegisterForm({ ...registerForm, firstName: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Last Name</label>
                  <input
                    type="text"
                    required
                    value={registerForm.lastName}
                    onBlur={handleCheckDuplicate}
                    onChange={(e) => setRegisterForm({ ...registerForm, lastName: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Date of Birth</label>
                  <input
                    type="date"
                    required
                    value={registerForm.dateOfBirth}
                    onBlur={handleCheckDuplicate}
                    onChange={(e) => setRegisterForm({ ...registerForm, dateOfBirth: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Phone Number</label>
                  <input
                    type="tel"
                    required
                    value={registerForm.phoneNumber}
                    onBlur={handleCheckDuplicate}
                    onChange={(e) => setRegisterForm({ ...registerForm, phoneNumber: e.target.value })}
                    placeholder="078xxxxxxx"
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Gender</label>
                  <select
                    value={registerForm.gender}
                    onChange={(e) => setRegisterForm({ ...registerForm, gender: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="Female">Female</option>
                    <option value="Male">Male</option>
                    <option value="Other">Other</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Blood Group</label>
                  <select
                    value={registerForm.bloodGroup}
                    onChange={(e) => setRegisterForm({ ...registerForm, bloodGroup: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm bg-white"
                  >
                    <option value="O+">O+</option>
                    <option value="O-">O-</option>
                    <option value="A+">A+</option>
                    <option value="A-">A-</option>
                    <option value="B+">B+</option>
                    <option value="B-">B-</option>
                    <option value="AB+">AB+</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Address</label>
                  <input
                    type="text"
                    value={registerForm.address}
                    onChange={(e) => setRegisterForm({ ...registerForm, address: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-200 rounded-xl text-sm"
                  />
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setShowRegisterModal(false)}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-sm font-medium text-slate-600 hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-medium"
                >
                  Confirm & Generate MRN
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
