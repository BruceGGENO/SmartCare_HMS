import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { useAuth } from '../services/AuthContext';
import { CreditCard, CheckCircle, Clock, Smartphone, DollarSign } from 'lucide-react';

export default function Billing() {
  const [invoices, setInvoices] = useState([]);
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [loading, setLoading] = useState(true);
  const [payAmount, setPayAmount] = useState('');
  const [phone, setPhone] = useState('0780000000');
  const [paying, setPaying] = useState(false);
  const { hasPermission } = useAuth();

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/billing/invoices/pending');
      setInvoices(res.data || []);
      if (res.data && res.data.length > 0) {
        loadInvoiceDetails(res.data[0].id);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadInvoiceDetails = async (id) => {
    try {
      const res = await api.get(`/billing/invoices/${id}`);
      setSelectedInvoice(res.data);
      setPayAmount((res.data.totalAmount - res.data.paidAmount).toFixed(2));
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handlePay = async (e) => {
    e.preventDefault();
    if (!selectedInvoice) return;
    setPaying(true);
    try {
      await api.post(`/billing/invoices/${selectedInvoice.id}/pay`, {
        amount: parseFloat(payAmount),
        paymentMethod: 'MobileMoney',
        phoneNumber: phone
      });
      alert('Payment successful!');
      loadData();
    } catch (err) {
      alert(err.response?.data?.message || 'Payment processing failed');
    } finally {
      setPaying(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Billing & Invoicing (MOD-04)</h1>
        <p className="text-sm text-slate-500">Patient invoice balance reconciliation and payment capture</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Invoices List */}
        <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-sm lg:col-span-1">
          <div className="p-4 border-b border-slate-100 font-semibold text-sm text-slate-700 flex justify-between">
            <span>Pending Invoices</span>
            <span className="text-xs bg-brand-50 text-brand-600 px-2 py-0.5 rounded-full font-bold">{invoices.length}</span>
          </div>
          <div className="divide-y divide-slate-100 max-h-[600px] overflow-y-auto">
            {invoices.map(inv => (
              <div
                key={inv.id}
                onClick={() => loadInvoiceDetails(inv.id)}
                className={`p-4 cursor-pointer transition ${
                  selectedInvoice?.id === inv.id ? 'bg-brand-50/70 border-l-4 border-brand-600' : 'hover:bg-slate-50'
                }`}
              >
                <div className="flex items-center justify-between">
                  <h4 className="font-semibold text-slate-900 text-sm">{inv.patientName}</h4>
                  <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase ${
                    inv.status === 'Paid' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'
                  }`}>
                    {inv.status}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">{inv.patientMrn}</p>
                <div className="mt-2 flex items-center justify-between text-xs">
                  <span className="text-slate-500">Balance:</span>
                  <span className="font-bold text-slate-900">
                    ${(inv.totalAmount - inv.paidAmount).toFixed(2)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Selected Invoice Details & Pay */}
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm lg:col-span-2">
          {selectedInvoice ? (
            <div className="space-y-6">
              <div className="flex items-start justify-between border-b border-slate-100 pb-4">
                <div>
                  <span className="text-xs font-mono bg-slate-100 text-slate-600 px-2 py-0.5 rounded">INVOICE #{selectedInvoice.id}</span>
                  <h3 className="text-xl font-bold text-slate-900 mt-1">{selectedInvoice.patientName}</h3>
                  <p className="text-xs text-slate-400">MRN: {selectedInvoice.patientMrn}</p>
                </div>
                <div className="text-right">
                  <p className="text-xs text-slate-400">Total Billed</p>
                  <p className="text-2xl font-black text-slate-900">${selectedInvoice.totalAmount.toFixed(2)}</p>
                </div>
              </div>

              {/* Line items */}
              <div>
                <h4 className="text-xs font-bold text-slate-600 uppercase tracking-wider mb-2">Itemized Services</h4>
                <div className="bg-slate-50 rounded-xl overflow-hidden border border-slate-200">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-100 text-slate-600 uppercase font-semibold">
                      <tr>
                        <th className="py-2.5 px-3">Description</th>
                        <th className="py-2.5 px-3 text-center">Qty</th>
                        <th className="py-2.5 px-3 text-right">Price</th>
                        <th className="py-2.5 px-3 text-right">Total</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-200">
                      {selectedInvoice.lines?.map(l => (
                        <tr key={l.id}>
                          <td className="py-2 px-3 text-slate-800">{l.description}</td>
                          <td className="py-2 px-3 text-center text-slate-600">{l.quantity}</td>
                          <td className="py-2 px-3 text-right text-slate-600">${l.unitPrice.toFixed(2)}</td>
                          <td className="py-2 px-3 text-right font-semibold text-slate-900">${l.lineTotal.toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Payment Summary */}
              <div className="p-4 bg-slate-50 rounded-xl flex items-center justify-between text-sm">
                <div>
                  <span className="text-slate-500">Paid Amount: </span>
                  <span className="font-semibold text-emerald-600">${selectedInvoice.paidAmount.toFixed(2)}</span>
                </div>
                <div>
                  <span className="text-slate-500">Remaining Due: </span>
                  <span className="font-bold text-red-600 text-base">
                    ${(selectedInvoice.totalAmount - selectedInvoice.paidAmount).toFixed(2)}
                  </span>
                </div>
              </div>

              {/* Payment Form */}
              {hasPermission('billing.pay') && selectedInvoice.status !== 'Paid' && (
                <form onSubmit={handlePay} className="p-5 bg-brand-50/50 rounded-2xl border border-brand-100 space-y-4">
                  <div className="flex items-center gap-2 text-brand-900 font-bold text-sm">
                    <Smartphone className="w-4 h-4 text-brand-600" />
                    <span>Process Payment</span>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Pay Amount ($)</label>
                      <input
                        type="number"
                        step="0.01"
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-sm"
                        value={payAmount}
                        onChange={e => setPayAmount(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold text-slate-600 uppercase mb-1">Mobile Money Phone</label>
                      <input
                        type="text"
                        required
                        className="w-full px-3 py-2 bg-white border border-slate-200 rounded-xl text-sm"
                        value={phone}
                        onChange={e => setPhone(e.target.value)}
                      />
                    </div>
                  </div>
                  <button
                    type="submit"
                    disabled={paying}
                    className="w-full py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-xl text-sm font-semibold shadow transition disabled:opacity-50"
                  >
                    {paying ? 'Processing Gateway...' : `Confirm & Pay $${payAmount}`}
                  </button>
                </form>
              )}
            </div>
          ) : (
            <div className="p-16 text-center text-slate-400 text-sm">Select an invoice to view itemized lines and payment actions.</div>
          )}
        </div>
      </div>
    </div>
  );
}
