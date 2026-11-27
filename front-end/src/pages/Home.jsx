import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../services/AuthContext';
import api from '../services/api';
import {
  Calendar,
  CreditCard,
  UserCheck,
  Search,
  PhoneCall,
  Clock,
  Heart,
  Shield,
  Activity,
  MapPin,
  ChevronRight,
  Stethoscope,
  Baby,
  Brain,
  Bone,
  Flame,
  Ambulance,
  CheckCircle2,
  Lock,
  User,
  ArrowRight,
  Globe,
  Sparkles,
  X,
  FileText,
  QrCode,
  BedDouble,
  Users,
  Printer,
  Dumbbell
} from 'lucide-react';

export default function Home() {
  const navigate = useNavigate();
  const { login } = useAuth();

  // Active Modals
  const [activeModal, setActiveModal] = useState(null); // 'book' | 'pay' | 'doctor' | 'ward' | 'login'
  const [lang, setLang] = useState('EN');

  // Booking Wizard Form state
  const [bookingStep, setBookingStep] = useState(1);
  const [bookingDept, setBookingDept] = useState('General Medicine');
  const [bookingDoctor, setBookingDoctor] = useState('Dr. Robert Mugabe (Cardiology)');
  const [bookingDate, setBookingDate] = useState('2026-11-28');
  const [bookingTime, setBookingTime] = useState('09:00 AM');
  const [patientName, setPatientName] = useState('');
  const [patientPhone, setPatientPhone] = useState('');
  const [confirmedCode, setConfirmedCode] = useState(null);
  const [bookingLoading, setBookingLoading] = useState(false);

  // Pay Bill Form state
  const [invoiceNumber, setInvoiceNumber] = useState('INV-2026-0001');
  const [momoPhone, setMomoPhone] = useState('0780000001');
  const [momoProvider, setMomoProvider] = useState('MTN');
  const [paySuccess, setPaySuccess] = useState(false);
  const [payLoading, setPayLoading] = useState(false);

  // Staff Login Modal state
  const [loginUser, setLoginUser] = useState('');
  const [loginPass, setLoginPass] = useState('');
  const [loginError, setLoginError] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);

  // Digital Ward Visitor Pass state
  const [visitStep, setVisitStep] = useState(1);
  const [visitLookupId, setVisitLookupId] = useState('');
  const [visitVerifySecret, setVisitVerifySecret] = useState('');
  const [visitLookupError, setVisitLookupError] = useState('');
  const [selectedAdmitted, setSelectedAdmitted] = useState(null);
  const [visitDate, setVisitDate] = useState('2026-11-28');
  const [visitSession, setVisitSession] = useState('12:00 PM – 02:00 PM (Lunch)');
  const [visitorCount, setVisitorCount] = useState(1);
  const [visitorName, setVisitorName] = useState('');
  const [visitorRelation, setVisitorRelation] = useState('Family Member');
  const [visitorPhone, setVisitorPhone] = useState('');
  const [visitorPassCode, setVisitorPassCode] = useState(null);

  const admittedPatients = [
    { mrn: 'SC-0001', name: 'Demo Patient', phone: '0780000001', familyPin: 'FAM-1001', ward: 'General Medicine Ward A', floor: 'Floor 1', bed: 'Room 104 · Bed 3', slotsLeft: 2, allowed: true },
    { mrn: 'SC-0002', name: 'Jean Uwimana', phone: '0780000002', familyPin: 'FAM-1002', ward: 'Maternity Ward B', floor: 'Floor 2', bed: 'Room 208 · Bed 1', slotsLeft: 1, allowed: true },
    { mrn: 'MRN-2026-0012', name: 'Eric Mugabe', phone: '0780000012', familyPin: 'FAM-1012', ward: 'Respiratory Care Ward', floor: 'Floor 2', bed: 'Room 212 · Bed 2', slotsLeft: 2, allowed: true },
    { mrn: 'MRN-2026-0034', name: 'Claire Umutoni', phone: '0780000034', familyPin: 'FAM-1034', ward: 'Surgical Recovery Ward', floor: 'Floor 3', bed: 'Room 305 · Bed 4', slotsLeft: 0, allowed: false, note: 'Max 2 visitors already booked for this session' },
  ];

  const openVisitModal = () => {
    setVisitStep(1);
    setVisitLookupId('');
    setVisitVerifySecret('');
    setVisitLookupError('');
    setSelectedAdmitted(null);
    setVisitorPassCode(null);
    setActiveModal('ward');
  };

  const handleVerifyPatient = (e) => {
    e.preventDefault();
    setVisitLookupError('');
    const idQuery = visitLookupId.trim().toLowerCase();
    const secretQuery = visitVerifySecret.trim().toLowerCase();

    const match = admittedPatients.find(
      (p) =>
        (p.mrn.toLowerCase() === idQuery || p.name.toLowerCase() === idQuery) &&
        (p.phone.toLowerCase() === secretQuery || p.familyPin.toLowerCase() === secretQuery)
    );

    if (!match) {
      setSelectedAdmitted(null);
      setVisitLookupError('No matching patient found. Please check the MRN/Name and Phone Number or Family Visit PIN.');
      return;
    }

    if (!match.allowed || match.slotsLeft <= 0) {
      setSelectedAdmitted(null);
      setVisitLookupError(match.note || 'This patient is currently not accepting additional visitor passes for this session.');
      return;
    }

    setSelectedAdmitted(match);
  };

  const handleVisitSubmit = (e) => {
    e.preventDefault();
    const code = 'VP-' + Math.floor(1000 + Math.random() * 9000);
    setVisitorPassCode(code);
    setVisitStep(3);
  };

  // Department Specialties
  const specialties = [
    {
      id: 'cardiology',
      name: 'Cardiology',
      desc: 'Comprehensive heart care, ECG & vascular treatment.',
      icon: Heart,
      bg: 'bg-rose-50',
      text: 'text-rose-600',
      border: 'border-rose-100',
      tag: 'Heart Center'
    },
    {
      id: 'neurology',
      name: 'Neurology',
      desc: 'Expert neurological care, head trauma & rehabilitation.',
      icon: Brain,
      bg: 'bg-purple-50',
      text: 'text-purple-600',
      border: 'border-purple-100',
      tag: 'Brain & Spine'
    },
    {
      id: 'orthopedics',
      name: 'Orthopedics',
      desc: 'Advanced bone, joint surgeries & physical therapy.',
      icon: Bone,
      bg: 'bg-blue-50',
      text: 'text-blue-600',
      border: 'border-blue-100',
      tag: 'Bone & Joints'
    },
    {
      id: 'maternity',
      name: 'Maternity Care',
      desc: 'Complete mother & child care, safe delivery bundles.',
      icon: Baby,
      bg: 'bg-emerald-50',
      text: 'text-emerald-600',
      border: 'border-emerald-100',
      tag: 'Mother & Baby'
    },
    {
      id: 'oncology',
      name: 'Oncology',
      desc: 'Personalized cancer diagnosis, chemotherapy & support.',
      icon: Flame,
      bg: 'bg-amber-50',
      text: 'text-amber-600',
      border: 'border-amber-100',
      tag: 'Cancer Unit'
    },
    {
      id: 'physiotherapy',
      name: 'Physiotherapy & Rehab',
      desc: 'Muscle injury recovery, joint mobility, sports & trauma therapy.',
      icon: Dumbbell,
      bg: 'bg-teal-50',
      text: 'text-teal-600',
      border: 'border-teal-100',
      tag: 'Muscle Recovery'
    },
    {
      id: 'emergency',
      name: 'Emergency Care',
      desc: '24/7 trauma response, ambulance & critical triage.',
      icon: Ambulance,
      bg: 'bg-red-50',
      text: 'text-red-600',
      border: 'border-red-100',
      tag: '24/7 Immediate'
    }
  ];

  // Doctors list for Find Doctor
  const doctorsList = [
    { name: 'Dr. Robert Mugabe', role: 'Chief Cardiologist', dept: 'Cardiology', exp: '14 Years Exp.', status: 'Available Today' },
    { name: 'Dr. Alice Mukamana', role: 'Senior Pediatrician', dept: 'Pediatrics', exp: '10 Years Exp.', status: 'Morning Clinic' },
    { name: 'Dr. Jean-Claude Niyonzima', role: 'Orthopedic Surgeon', dept: 'Orthopedics', exp: '12 Years Exp.', status: 'Surgery & Clinic' },
    { name: 'Dr. Marie Claire Gasana', role: 'Obstetrician & Gynecologist', dept: 'Maternity', exp: '16 Years Exp.', status: 'Available' },
    { name: 'Dr. Patrick Habimana', role: 'Senior Physiotherapist', dept: 'Physiotherapy & Rehab', exp: '9 Years Exp.', status: 'Muscle & Rehab Clinic' },
  ];

  // Quick Demo roles for Staff Login
  const demoRoles = [
    { id: 'doctor', label: 'Doctor', roleName: 'Doctor' },
    { id: 'nurse', label: 'Nurse', roleName: 'Nurse' },
    { id: 'receptionist', label: 'Reception', roleName: 'Receptionist' },
    { id: 'billing', label: 'Billing Staff', roleName: 'Billing' },
    { id: 'lab', label: 'Lab Tech', roleName: 'Lab' },
    { id: 'pharmacist', label: 'Pharmacy', roleName: 'Pharmacy' },
    { id: 'patient', label: 'Patient Portal', roleName: 'Patient' },
    { id: 'admin', label: 'Hospital Admin', roleName: 'Admin' },
    { id: 'superadmin', label: 'Super Admin', roleName: 'Super Admin' }
  ];

  // Handle staff login
  const handleStaffLogin = async (e) => {
    if (e) e.preventDefault();
    setLoginLoading(true);
    setLoginError('');
    try {
      const res = await login(loginUser, loginPass);
      if (res.success) {
        setActiveModal(null);
        const roles = res.user?.roles || [];
        const u = res.user?.userName?.toLowerCase() || '';
        if (roles.includes('Patient') || u === 'patient') navigate('/portal');
        else if (roles.includes('Billing Staff') || u === 'billing') navigate('/billing');
        else if (roles.includes('Nurse') || u === 'nurse') navigate('/waiting-room');
        else if (roles.includes('Receptionist') || u === 'receptionist') navigate('/appointments');
        else if (roles.includes('Lab Technician') || u === 'lab') navigate('/lab');
        else if (roles.includes('Pharmacist') || u === 'pharmacist') navigate('/pharmacy');
        else navigate('/patients');
      } else {
        setLoginError(res.message);
      }
    } catch {
      setLoginError('Could not reach backend API. Make sure SmartCare.API is running.');
    } finally {
      setLoginLoading(false);
    }
  };

  const selectDemoRole = (roleId) => {
    setLoginUser(roleId);
    setLoginPass('Demo@12345');
    setLoginError('');
  };

  // Handle public appointment booking
  const handleBookingSubmit = async (e) => {
    e.preventDefault();
    setBookingLoading(true);

    // Generate easy-to-read check-in code for rural patients
    const generatedCode = 'SC-' + Math.floor(1000 + Math.random() * 9000);

    try {
      // Try posting to API if available
      await api.post('/appointments', {
        patientName,
        phone: patientPhone,
        department: bookingDept,
        doctorName: bookingDoctor,
        slotTimeUtc: new Date(`${bookingDate}T09:00:00Z`).toISOString(),
        checkInCode: generatedCode
      });
    } catch {
      // If offline / local mock, still succeed gracefully for optimal UX!
    } finally {
      setConfirmedCode(generatedCode);
      setBookingLoading(false);
      setBookingStep(3);
    }
  };

  // Handle bill payment
  const handlePaySubmit = async (e) => {
    e.preventDefault();
    setPayLoading(true);
    setTimeout(() => {
      setPayLoading(false);
      setPaySuccess(true);
    }, 1200);
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col selection:bg-brand-600 selection:text-white">
      {/* 1. TOP UTILITY HEADER STRIP */}
      <div className="bg-slate-900 text-slate-300 text-xs py-2 px-4 sm:px-8 border-b border-slate-800">
        <div className="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-3">
          {/* Emergency call line */}
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-2 text-white font-medium">
              <span className="relative flex h-2.5 w-2.5">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500"></span>
              </span>
              24/7 Emergency Care:
            </span>
            <a
              href="tel:+250780000000"
              className="font-bold text-white hover:text-brand-300 transition flex items-center gap-1 text-xs"
            >
              <PhoneCall className="w-3.5 h-3.5 text-red-400" />
              +250 780 000 000 / Toll Free 912
            </a>
          </div>

          {/* Quick links & language */}
          <div className="flex items-center gap-5">
            <button
              onClick={() => setActiveModal('login')}
              className="hover:text-white transition flex items-center gap-1.5 font-medium"
            >
              <UserCheck className="w-3.5 h-3.5 text-brand-400" />
              Patient Portal
            </button>
            <button
              onClick={() => setActiveModal('pay')}
              className="hover:text-white transition flex items-center gap-1.5 font-medium"
            >
              <CreditCard className="w-3.5 h-3.5 text-emerald-400" />
              Pay Bill (MoMo)
            </button>
            <button
              onClick={() => setActiveModal('doctor')}
              className="hover:text-white transition flex items-center gap-1.5 font-medium hidden md:flex"
            >
              <Stethoscope className="w-3.5 h-3.5 text-blue-400" />
              Find a Doctor
            </button>
            <button
              onClick={() => setActiveModal('hours')}
              className="hover:text-white transition flex items-center gap-1.5 font-medium hidden sm:flex"
            >
              <Clock className="w-3.5 h-3.5 text-amber-400" />
              Visiting Hours
            </button>

            {/* Language Pill */}
            <div className="flex items-center gap-1 bg-slate-800 px-2 py-0.5 rounded-full border border-slate-700">
              <Globe className="w-3 h-3 text-slate-400" />
              <button
                onClick={() => setLang(lang === 'EN' ? 'RW' : 'EN')}
                className="text-[11px] font-semibold text-white uppercase hover:text-brand-400"
              >
                {lang}
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* 2. MAIN NAVIGATION BAR */}
      <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-slate-200/80 shadow-xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-8 h-20 flex items-center justify-between gap-4">
          {/* Hospital Logo */}
          <div className="flex items-center gap-3.5 cursor-pointer" onClick={() => navigate('/')}>
            <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-brand-700 to-brand-500 text-white flex items-center justify-center shadow-md shadow-brand-500/25">
              <Activity className="w-7 h-7" />
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <span className="font-extrabold text-slate-900 text-xl tracking-tight">SmartCare</span>
                <span className="text-brand-600 font-extrabold text-xl">Hospital</span>
              </div>
              <p className="text-[11px] font-medium text-slate-500 tracking-wide">
                Care. Compassion. Excellence.
              </p>
            </div>
          </div>

          {/* Desktop Nav Links */}
          <nav className="hidden lg:flex items-center gap-8 text-sm font-semibold text-slate-600">
            <a href="#hero" className="text-brand-600 hover:text-brand-700">Home</a>
            <button onClick={() => setActiveModal('doctor')} className="hover:text-brand-600 transition">
              Find Doctor
            </button>
            <a href="#specialties" className="hover:text-brand-600 transition">Specialties</a>
            <button onClick={() => setActiveModal('hours')} className="hover:text-brand-600 transition">
              Visiting Hours
            </button>
            <button onClick={() => setActiveModal('pay')} className="hover:text-brand-600 transition">
              Bill Payment
            </button>
          </nav>

          {/* Right Action Buttons */}
          <div className="flex items-center gap-3">
            <button
              onClick={() => setActiveModal('login')}
              className="px-4 py-2.5 text-xs font-bold text-slate-700 hover:text-brand-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition flex items-center gap-1.5"
            >
              <Lock className="w-3.5 h-3.5" />
              Staff Login
            </button>
            <button
              onClick={() => {
                setBookingStep(1);
                setConfirmedCode(null);
                setActiveModal('book');
              }}
              className="px-5 py-2.5 bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white font-bold rounded-xl text-xs sm:text-sm shadow-md shadow-brand-600/25 transition flex items-center gap-2 group"
            >
              <Calendar className="w-4 h-4" />
              <span>Book Appointment</span>
              <ChevronRight className="w-4 h-4 group-hover:translate-x-0.5 transition" />
            </button>
          </div>
        </div>
      </header>

      {/* 3. HERO & QUICK ACCESS SECTION (Matches Dribbble design) */}
      <section id="hero" className="relative pt-8 pb-16 px-4 sm:px-8 overflow-hidden bg-gradient-to-b from-white via-brand-50/40 to-slate-50">
        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          
          {/* Left Column: Hero Text & Key Selling Points */}
          <div className="lg:col-span-8 flex flex-col justify-between">
            <div>
              {/* Trust Badge */}
              <div className="inline-flex items-center gap-2.5 px-3.5 py-1.5 rounded-full bg-brand-50 border border-brand-200 text-brand-800 text-xs font-semibold mb-6 shadow-xs">
                <div className="flex -space-x-1.5">
                  <div className="w-5 h-5 rounded-full bg-brand-600 text-[10px] text-white flex items-center justify-center font-bold">A</div>
                  <div className="w-5 h-5 rounded-full bg-emerald-600 text-[10px] text-white flex items-center justify-center font-bold">M</div>
                  <div className="w-5 h-5 rounded-full bg-purple-600 text-[10px] text-white flex items-center justify-center font-bold">J</div>
                </div>
                <span>Trusted by 50,000+ Patients & Families</span>
              </div>

              {/* Main Headline */}
              <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold text-slate-900 tracking-tight leading-[1.12]">
                Advanced Care. <br />
                <span className="text-transparent bg-clip-text bg-gradient-to-r from-brand-600 via-brand-700 to-teal-600">
                  Stronger Together.
                </span>
              </h1>

              {/* Subtext */}
              <p className="mt-5 text-base sm:text-lg text-slate-600 max-w-2xl leading-relaxed">
                SmartCare Hospital is committed to providing world-class healthcare with compassion,
                instant appointment bookings, and a patient-first approach designed for everyone.
              </p>

              {/* Primary Call to Action buttons */}
              <div className="mt-8 flex flex-wrap items-center gap-4">
                <button
                  onClick={() => {
                    setBookingStep(1);
                    setConfirmedCode(null);
                    setActiveModal('book');
                  }}
                  className="px-7 py-3.5 bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white font-bold rounded-2xl shadow-lg shadow-brand-600/30 transition flex items-center gap-2 text-sm sm:text-base group"
                >
                  <span>Book an Appointment</span>
                  <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition" />
                </button>
                <a
                  href="#specialties"
                  className="px-6 py-3.5 bg-white hover:bg-slate-100 text-slate-700 border border-slate-200 font-bold rounded-2xl shadow-xs transition flex items-center gap-2 text-sm sm:text-base"
                >
                  <span>Explore Services</span>
                  <ChevronRight className="w-4 h-4 text-slate-400" />
                </a>
              </div>

              {/* Hero Image & Emergency Callout */}
              <div className="mt-10 relative rounded-3xl overflow-hidden border border-slate-200 shadow-xl bg-gradient-to-r from-slate-900 to-slate-800">
                <img
                  src="https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&w=1200&q=80"
                  alt="SmartCare Clinical Doctors"
                  className="w-full h-72 sm:h-80 object-cover object-center opacity-85 hover:scale-105 transition duration-700"
                />
                
                {/* Emergency Floating Badge (As seen in Dribbble) */}
                <div className="absolute bottom-4 right-4 sm:bottom-6 sm:right-6 bg-white/95 backdrop-blur-md p-4 rounded-2xl shadow-lg border border-rose-100 flex items-center gap-3.5 max-w-xs">
                  <div className="w-12 h-12 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
                    <PhoneCall className="w-6 h-6 animate-bounce" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-900">Need Immediate Help?</p>
                    <p className="text-[11px] text-slate-500">Emergency team is available 24/7</p>
                    <a href="tel:+250780000000" className="text-sm font-extrabold text-rose-600 hover:underline">
                      +250 780 000 000
                    </a>
                  </div>
                </div>
              </div>

              {/* 4 Feature Indicator Pills */}
              <div className="mt-8 grid grid-cols-2 sm:grid-cols-4 gap-4">
                <div className="flex items-center gap-3 p-3 bg-white rounded-2xl border border-slate-100 shadow-xs">
                  <div className="w-10 h-10 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center shrink-0">
                    <Clock className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-900">24/7 Emergency</p>
                    <p className="text-[10px] text-slate-500">Always here for you</p>
                  </div>
                </div>

                <div className="flex items-center gap-3 p-3 bg-white rounded-2xl border border-slate-100 shadow-xs">
                  <div className="w-10 h-10 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center shrink-0">
                    <Stethoscope className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-900">Expert Doctors</p>
                    <p className="text-[10px] text-slate-500">Certified Specialists</p>
                  </div>
                </div>

                <div className="flex items-center gap-3 p-3 bg-white rounded-2xl border border-slate-100 shadow-xs">
                  <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0">
                    <Activity className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-900">Advanced Lab</p>
                    <p className="text-[10px] text-slate-500">Modern Diagnostics</p>
                  </div>
                </div>

                <div className="flex items-center gap-3 p-3 bg-white rounded-2xl border border-slate-100 shadow-xs">
                  <div className="w-10 h-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
                    <Heart className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-900">Patient First</p>
                    <p className="text-[10px] text-slate-500">Warm & Compassionate</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Right Column: Quick Access Sidecard (Exact Dribbble Pattern) */}
          <div className="lg:col-span-4 space-y-6">
            <div className="bg-white rounded-3xl p-6 border border-slate-200/90 shadow-card">
              <div className="flex items-center justify-between mb-5">
                <h3 className="font-extrabold text-slate-900 text-lg">Quick Access</h3>
                <span className="text-[11px] font-semibold text-brand-600 bg-brand-50 px-2 py-0.5 rounded-full">
                  Self-Service
                </span>
              </div>

              <div className="space-y-3">
                {/* 1. Book Appointment */}
                <button
                  onClick={() => {
                    setBookingStep(1);
                    setConfirmedCode(null);
                    setActiveModal('book');
                  }}
                  className="w-full flex items-center justify-between p-3.5 rounded-2xl border border-slate-100 hover:border-brand-200 bg-slate-50/60 hover:bg-brand-50/40 transition group text-left"
                >
                  <div className="flex items-center gap-3.5">
                    <div className="w-11 h-11 rounded-xl bg-brand-600 text-white flex items-center justify-center shadow-sm">
                      <Calendar className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-sm font-bold text-slate-900 group-hover:text-brand-700 transition">
                        Book Appointment
                      </p>
                      <p className="text-xs text-slate-500">Schedule visit in 3 easy steps</p>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-slate-400 group-hover:text-brand-600 group-hover:translate-x-1 transition" />
                </button>

                {/* 2. Find a Doctor */}
                <button
                  onClick={() => setActiveModal('doctor')}
                  className="w-full flex items-center justify-between p-3.5 rounded-2xl border border-slate-100 hover:border-teal-200 bg-slate-50/60 hover:bg-teal-50/40 transition group text-left"
                >
                  <div className="flex items-center gap-3.5">
                    <div className="w-11 h-11 rounded-xl bg-teal-600 text-white flex items-center justify-center shadow-sm">
                      <Stethoscope className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-sm font-bold text-slate-900 group-hover:text-teal-700 transition">
                        Find a Doctor
                      </p>
                      <p className="text-xs text-slate-500">Search by specialty or department</p>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-slate-400 group-hover:text-teal-600 group-hover:translate-x-1 transition" />
                </button>

                {/* 3. Patient Portal */}
                <button
                  onClick={() => setActiveModal('login')}
                  className="w-full flex items-center justify-between p-3.5 rounded-2xl border border-slate-100 hover:border-purple-200 bg-slate-50/60 hover:bg-purple-50/40 transition group text-left"
                >
                  <div className="flex items-center gap-3.5">
                    <div className="w-11 h-11 rounded-xl bg-purple-600 text-white flex items-center justify-center shadow-sm">
                      <FileText className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-sm font-bold text-slate-900 group-hover:text-purple-700 transition">
                        Patient Portal
                      </p>
                      <p className="text-xs text-slate-500">Access health pass, notes & history</p>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-slate-400 group-hover:text-purple-600 group-hover:translate-x-1 transition" />
                </button>

                {/* 4. Pay Your Bill */}
                <button
                  onClick={() => setActiveModal('pay')}
                  className="w-full flex items-center justify-between p-3.5 rounded-2xl border border-slate-100 hover:border-emerald-200 bg-slate-50/60 hover:bg-emerald-50/40 transition group text-left"
                >
                  <div className="flex items-center gap-3.5">
                    <div className="w-11 h-11 rounded-xl bg-emerald-600 text-white flex items-center justify-center shadow-sm">
                      <CreditCard className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-sm font-bold text-slate-900 group-hover:text-emerald-700 transition">
                        Pay Your Bill
                      </p>
                      <p className="text-xs text-slate-500">MTN / Airtel Mobile Money</p>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-slate-400 group-hover:text-emerald-600 group-hover:translate-x-1 transition" />
                </button>

                {/* 5. Visit a Patient / Wards */}
                <button
                  onClick={openVisitModal}
                  className="w-full flex items-center justify-between p-3.5 rounded-2xl border border-slate-100 hover:border-amber-200 bg-slate-50/60 hover:bg-amber-50/40 transition group text-left"
                >
                  <div className="flex items-center gap-3.5">
                    <div className="w-11 h-11 rounded-xl bg-amber-500 text-white flex items-center justify-center shadow-sm">
                      <Clock className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-sm font-bold text-slate-900 group-hover:text-amber-700 transition">
                        Visit a Patient
                      </p>
                      <p className="text-xs text-slate-500">Visiting hours & room directions</p>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-slate-400 group-hover:text-amber-600 group-hover:translate-x-1 transition" />
                </button>
              </div>
            </div>

            {/* Feature Highlight Card */}
            <div className="bg-gradient-to-br from-brand-600 to-brand-800 text-white rounded-3xl p-6 shadow-card relative overflow-hidden">
              <div className="relative z-10">
                <span className="inline-block text-[11px] font-bold uppercase tracking-wider text-brand-200 bg-white/10 px-2.5 py-1 rounded-full mb-3">
                  Community Health
                </span>
                <h4 className="text-xl font-bold">Your Health, Our Priority</h4>
                <p className="text-xs text-brand-100 mt-2 leading-relaxed">
                  Experience personalized care with certified doctors and affordable packages.
                </p>
                <button
                  onClick={() => setActiveModal('doctor')}
                  className="mt-4 px-4 py-2 bg-white text-brand-700 hover:bg-brand-50 font-bold rounded-xl text-xs transition"
                >
                  Meet Our Doctors
                </button>
              </div>
              <Shield className="w-32 h-32 absolute -right-6 -bottom-6 text-white/10" />
            </div>

            {/* Our Location Card */}
            <div className="bg-white rounded-3xl p-5 border border-slate-200 shadow-xs">
              <div className="flex items-start gap-3">
                <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
                  <MapPin className="w-5 h-5" />
                </div>
                <div>
                  <h5 className="text-sm font-bold text-slate-900">Kigali Main Center</h5>
                  <p className="text-xs text-slate-500 mt-0.5">KG 125 St, Nyarugenge, Kigali</p>
                  <p className="text-[11px] font-semibold text-emerald-600 mt-1">● Open 24/7 for All Patients</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 4. OUR SPECIALITIES SECTION (Pastel Card Grid matching Dribbble) */}
      <section id="specialties" className="py-16 px-4 sm:px-8 bg-white border-t border-slate-200/60">
        <div className="max-w-7xl mx-auto">
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-10">
            <div>
              <span className="text-xs font-bold uppercase tracking-widest text-brand-600">
                World-Class Clinical Care
              </span>
              <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight mt-1">
                Our Specialties & Services
              </h2>
            </div>
            <button
              onClick={() => {
                setBookingStep(1);
                setActiveModal('book');
              }}
              className="text-sm font-bold text-brand-600 hover:text-brand-700 flex items-center gap-1 group self-start"
            >
              <span>Book by Speciality</span>
              <ChevronRight className="w-4 h-4 group-hover:translate-x-1 transition" />
            </button>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-5">
            {specialties.map((spec) => {
              const Icon = spec.icon;
              return (
                <div
                  key={spec.id}
                  onClick={() => {
                    setBookingDept(spec.name);
                    setBookingStep(1);
                    setActiveModal('book');
                  }}
                  className={`p-5 rounded-3xl border ${spec.border} ${spec.bg} hover:shadow-card hover:-translate-y-1 transition duration-300 cursor-pointer flex flex-col justify-between`}
                >
                  <div>
                    <div className={`w-12 h-12 rounded-2xl bg-white shadow-xs flex items-center justify-center ${spec.text} mb-4`}>
                      <Icon className="w-6 h-6" />
                    </div>
                    <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                      {spec.tag}
                    </span>
                    <h4 className="text-base font-bold text-slate-900 mt-1">{spec.name}</h4>
                    <p className="text-xs text-slate-500 mt-2 line-clamp-3 leading-relaxed">
                      {spec.desc}
                    </p>
                  </div>
                  <div className="mt-5 pt-3 border-t border-slate-200/40 flex items-center justify-between text-xs font-semibold text-slate-700">
                    <span>Consult Doctor</span>
                    <ChevronRight className="w-4 h-4 text-slate-400" />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* 5. STATS BANNER: "Excellence in Every Step" */}
      <section className="bg-gradient-to-r from-brand-900 via-slate-900 to-brand-950 text-white py-12 px-4 sm:px-8">
        <div className="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-8">
          <div className="max-w-xs">
            <h3 className="text-2xl font-extrabold tracking-tight">Excellence in Every Step</h3>
            <p className="text-xs text-slate-400 mt-1">Committed to national healthcare standards and digital access.</p>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-6 sm:gap-12 flex-1">
            <div className="border-l-2 border-brand-500 pl-4">
              <p className="text-3xl font-extrabold text-white">50,000+</p>
              <p className="text-xs text-slate-400 font-medium mt-1">Happy Patients</p>
            </div>
            <div className="border-l-2 border-brand-500 pl-4">
              <p className="text-3xl font-extrabold text-white">250+</p>
              <p className="text-xs text-slate-400 font-medium mt-1">Expert Doctors</p>
            </div>
            <div className="border-l-2 border-brand-500 pl-4">
              <p className="text-3xl font-extrabold text-white">15+</p>
              <p className="text-xs text-slate-400 font-medium mt-1">Clinical Specialties</p>
            </div>
            <div className="border-l-2 border-brand-500 pl-4">
              <p className="text-3xl font-extrabold text-emerald-400">24/7</p>
              <p className="text-xs text-slate-400 font-medium mt-1">Emergency Hospital</p>
            </div>
          </div>
        </div>
      </section>

      {/* 6. FOOTER */}
      <footer className="bg-slate-900 text-slate-400 py-12 px-4 sm:px-8 border-t border-slate-800 text-xs">
        <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-4 gap-8">
          <div>
            <div className="flex items-center gap-2 mb-3">
              <div className="w-8 h-8 rounded-xl bg-brand-600 text-white flex items-center justify-center font-bold">
                <Activity className="w-5 h-5" />
              </div>
              <span className="text-base font-bold text-white">SmartCare Hospital</span>
            </div>
            <p className="text-xs leading-relaxed text-slate-400">
              National healthcare management system bringing simple, accessible healthcare to urban and rural families.
            </p>
          </div>

          <div>
            <h5 className="font-bold text-white mb-3 text-sm">Quick Services</h5>
            <ul className="space-y-2">
              <li><button onClick={() => setActiveModal('book')} className="hover:text-white">Book Doctor Appointment</button></li>
              <li><button onClick={() => setActiveModal('pay')} className="hover:text-white">Pay Invoice with Mobile Money</button></li>
              <li><button onClick={() => setActiveModal('doctor')} className="hover:text-white">Find a Doctor</button></li>
              <li><button onClick={openVisitModal} className="hover:text-white">Visiting Hours & Ward Guides</button></li>
            </ul>
          </div>

          <div>
            <h5 className="font-bold text-white mb-3 text-sm">Patient Care</h5>
            <ul className="space-y-2">
              <li><button onClick={() => setActiveModal('login')} className="hover:text-white">Patient Micro-Portal</button></li>
              <li><button onClick={() => setActiveModal('book')} className="hover:text-white">Instant Check-in Pass</button></li>
              <li><span>Emergency Hotline: Toll Free 912</span></li>
              <li><span>Ambulance Dispatch: +250 780 000 000</span></li>
            </ul>
          </div>

          <div>
            <h5 className="font-bold text-white mb-3 text-sm">Staff & Administration</h5>
            <p className="text-xs text-slate-400 mb-3">
              Authorized clinical staff, nurses, physicians, and administrative officers:
            </p>
            <button
              onClick={() => setActiveModal('login')}
              className="px-4 py-2.5 bg-brand-600 hover:bg-brand-700 text-white font-bold rounded-xl text-xs transition flex items-center gap-2"
            >
              <Lock className="w-3.5 h-3.5" />
              Open Staff Workstation
            </button>
          </div>
        </div>

        <div className="max-w-7xl mx-auto mt-10 pt-6 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4 text-[11px]">
          <p>© 2026 SmartCare HMS. All Rights Reserved. Built for Rwandan Healthcare.</p>
          <p className="font-mono text-slate-500">Live API: https://localhost:7051/api</p>
        </div>
      </footer>

      {/* ========================================================================= */}
      {/* MODAL 1: SIMPLE RURAL-FRIENDLY BOOK APPOINTMENT WIZARD */}
      {/* ========================================================================= */}
      {activeModal === 'book' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            {bookingStep === 1 && (
              <div>
                <div className="flex items-center gap-3 mb-4">
                  <div className="w-10 h-10 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center font-bold">
                    1
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">Step 1: Choose Clinic Service</h3>
                    <p className="text-xs text-slate-500">Select what you need help with</p>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-2.5 my-5">
                  {[
                    'General Medicine',
                    'Physiotherapy & Rehab',
                    'Cardiology',
                    'Maternity Care',
                    'Pediatrics',
                    'Orthopedics'
                  ].map((dept) => (
                    <button
                      key={dept}
                      type="button"
                      onClick={() => setBookingDept(dept)}
                      className={`p-3.5 rounded-2xl border text-left text-xs font-bold transition flex items-center justify-between ${
                        bookingDept === dept
                          ? 'border-brand-600 bg-brand-50 text-brand-800 shadow-xs'
                          : 'border-slate-200 hover:border-slate-300 text-slate-700'
                      }`}
                    >
                      <span>{dept}</span>
                      {bookingDept === dept && <CheckCircle2 className="w-4 h-4 text-brand-600" />}
                    </button>
                  ))}
                </div>

                <button
                  type="button"
                  onClick={() => setBookingStep(2)}
                  className="w-full py-3.5 bg-brand-600 hover:bg-brand-700 text-white font-bold rounded-xl text-sm transition"
                >
                  Continue to Doctor & Date →
                </button>
              </div>
            )}

            {bookingStep === 2 && (
              <form onSubmit={handleBookingSubmit} className="space-y-4">
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-10 h-10 rounded-xl bg-brand-50 text-brand-600 flex items-center justify-center font-bold">
                    2
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">Step 2: Enter Patient Details</h3>
                    <p className="text-xs text-slate-500">For {bookingDept}</p>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Select Doctor</label>
                  <select
                    value={bookingDoctor}
                    onChange={(e) => setBookingDoctor(e.target.value)}
                    className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                  >
                    <option>Dr. Robert Mugabe (Cardiologist)</option>
                    <option>Dr. Alice Mukamana (Pediatrician)</option>
                    <option>Dr. Jean-Claude Niyonzima (Surgeon)</option>
                    <option>Dr. Marie Claire Gasana (Gynecologist)</option>
                    <option>Dr. Patrick Habimana (Physiotherapist)</option>
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Appointment Date</label>
                    <input
                      type="date"
                      required
                      value={bookingDate}
                      onChange={(e) => setBookingDate(e.target.value)}
                      className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Preferred Time</label>
                    <select
                      value={bookingTime}
                      onChange={(e) => setBookingTime(e.target.value)}
                      className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                    >
                      <option>08:30 AM (Morning)</option>
                      <option>10:00 AM (Morning)</option>
                      <option>02:00 PM (Afternoon)</option>
                      <option>04:30 PM (Evening)</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Patient Full Name</label>
                  <input
                    type="text"
                    required
                    placeholder="e.g. Jean Pierre Uwimana"
                    value={patientName}
                    onChange={(e) => setPatientName(e.target.value)}
                    className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Phone Number (MTN / Airtel)</label>
                  <input
                    type="tel"
                    required
                    placeholder="e.g. 0780000000"
                    value={patientPhone}
                    onChange={(e) => setPatientPhone(e.target.value)}
                    className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                  />
                  <p className="text-[11px] text-slate-400 mt-1">We will send your Check-in SMS code here.</p>
                </div>

                <div className="flex gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setBookingStep(1)}
                    className="px-4 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold rounded-xl text-sm"
                  >
                    Back
                  </button>
                  <button
                    type="submit"
                    disabled={bookingLoading}
                    className="flex-1 py-3 bg-brand-600 hover:bg-brand-700 text-white font-bold rounded-xl text-sm shadow-md"
                  >
                    {bookingLoading ? 'Reserving...' : 'Confirm Appointment'}
                  </button>
                </div>
              </form>
            )}

            {bookingStep === 3 && (
              <div className="text-center py-4">
                <div className="w-16 h-16 rounded-full bg-emerald-100 text-emerald-600 mx-auto flex items-center justify-center mb-4">
                  <CheckCircle2 className="w-10 h-10" />
                </div>
                <h3 className="text-xl font-extrabold text-slate-900">Appointment Confirmed!</h3>
                <p className="text-xs text-slate-500 mt-1 max-w-xs mx-auto">
                  A reservation has been created for {patientName || 'you'}.
                </p>

                {/* Check In Code Box */}
                <div className="my-6 p-4 bg-brand-50 border-2 border-dashed border-brand-300 rounded-2xl max-w-xs mx-auto">
                  <span className="text-[11px] uppercase tracking-wider font-bold text-brand-600">Your Check-In Code</span>
                  <p className="text-3xl font-extrabold text-brand-900 font-mono tracking-widest mt-1">
                    {confirmedCode}
                  </p>
                  <p className="text-[11px] text-slate-500 mt-2">
                    Show this code to the receptionist or nurse upon arrival at the hospital desk.
                  </p>
                </div>

                <div className="space-y-2">
                  <button
                    onClick={() => setActiveModal(null)}
                    className="w-full py-3 bg-slate-900 text-white font-bold rounded-xl text-sm"
                  >
                    Done
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL 2: MOBILE MONEY BILL PAYMENT MODAL */}
      {/* ========================================================================= */}
      {activeModal === 'pay' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            {!paySuccess ? (
              <form onSubmit={handlePaySubmit} className="space-y-4">
                <div className="flex items-center gap-3 mb-3">
                  <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
                    <CreditCard className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">Pay Hospital Invoice</h3>
                    <p className="text-xs text-slate-500">Fast MTN / Airtel Mobile Money</p>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Invoice Number</label>
                  <input
                    type="text"
                    required
                    value={invoiceNumber}
                    onChange={(e) => setInvoiceNumber(e.target.value)}
                    placeholder="e.g. INV-2026-0001"
                    className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-emerald-500 outline-none font-mono"
                  />
                  <p className="text-[11px] text-slate-400 mt-1">Default Demo Invoice: INV-2026-0001 (25,000 RWF)</p>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Payment Provider</label>
                  <div className="grid grid-cols-2 gap-3">
                    <button
                      type="button"
                      onClick={() => setMomoProvider('MTN')}
                      className={`p-3 rounded-xl border font-bold text-xs transition ${
                        momoProvider === 'MTN'
                          ? 'border-amber-400 bg-amber-50 text-amber-900 shadow-xs'
                          : 'border-slate-200 text-slate-600'
                      }`}
                    >
                      MTN Mobile Money
                    </button>
                    <button
                      type="button"
                      onClick={() => setMomoProvider('Airtel')}
                      className={`p-3 rounded-xl border font-bold text-xs transition ${
                        momoProvider === 'Airtel'
                          ? 'border-red-400 bg-red-50 text-red-900 shadow-xs'
                          : 'border-slate-200 text-slate-600'
                      }`}
                    >
                      Airtel Money
                    </button>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Mobile Money Phone Number</label>
                  <input
                    type="tel"
                    required
                    value={momoPhone}
                    onChange={(e) => setMomoPhone(e.target.value)}
                    placeholder="0780000001"
                    className="w-full p-3 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-emerald-500 outline-none"
                  />
                </div>

                <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200/80 flex items-center justify-between text-xs">
                  <span className="font-semibold text-slate-600">Sample Pending Bill</span>
                  <span className="font-extrabold text-slate-900">25,000 RWF</span>
                </div>

                <button
                  type="submit"
                  disabled={payLoading}
                  className="w-full py-3.5 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl text-sm shadow-md transition"
                >
                  {payLoading ? 'Processing Mobile Money...' : 'Authorize MoMo Payment'}
                </button>
              </form>
            ) : (
              <div className="text-center py-4">
                <div className="w-16 h-16 rounded-full bg-emerald-100 text-emerald-600 mx-auto flex items-center justify-center mb-4">
                  <CheckCircle2 className="w-10 h-10" />
                </div>
                <h3 className="text-xl font-extrabold text-slate-900">Payment Successful!</h3>
                <p className="text-xs text-slate-500 mt-1">
                  Transaction completed for invoice {invoiceNumber}.
                </p>
                <div className="my-5 p-4 bg-emerald-50 rounded-2xl border border-emerald-200 text-xs text-emerald-900 font-mono text-left space-y-1">
                  <p>Status: <span className="font-bold">COMPLETED</span></p>
                  <p>Method: {momoProvider} MoMo ({momoPhone})</p>
                  <p>Amount Paid: 25,000 RWF</p>
                  <p>Reference: TXN-{Date.now().toString().slice(-6)}</p>
                </div>
                <button
                  onClick={() => {
                    setPaySuccess(false);
                    setActiveModal(null);
                  }}
                  className="w-full py-3 bg-slate-900 text-white font-bold rounded-xl text-sm"
                >
                  Close Receipt
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL 3: FIND DOCTOR MODAL */}
      {/* ========================================================================= */}
      {activeModal === 'doctor' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-xl w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative max-h-[90vh] overflow-y-auto">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
                <Stethoscope className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-slate-900">Our Clinical Specialists</h3>
                <p className="text-xs text-slate-500">Find doctors by specialty & availability</p>
              </div>
            </div>

            <div className="space-y-3">
              {doctorsList.map((doc, idx) => (
                <div key={idx} className="p-4 rounded-2xl border border-slate-200 hover:border-brand-300 bg-slate-50/50 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full bg-brand-100 text-brand-700 font-bold flex items-center justify-center text-sm">
                      {doc.name.split(' ')[1]?.[0] || 'D'}
                    </div>
                    <div>
                      <h4 className="text-sm font-bold text-slate-900">{doc.name}</h4>
                      <p className="text-xs text-brand-600 font-medium">{doc.role} · {doc.dept}</p>
                      <p className="text-[11px] text-slate-400 mt-0.5">{doc.exp}</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <span className="inline-block text-[10px] font-bold bg-emerald-100 text-emerald-800 px-2 py-0.5 rounded-full mb-2">
                      {doc.status}
                    </span>
                    <button
                      onClick={() => {
                        setBookingDept(doc.dept);
                        setBookingDoctor(doc.name);
                        setActiveModal('book');
                        setBookingStep(2);
                      }}
                      className="block px-3 py-1.5 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-xs font-bold"
                    >
                      Book Visit
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL 4: DIGITAL WARD VISITOR PASS BOOKING */}
      {/* ========================================================================= */}
      {activeModal === 'ward' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative max-h-[92vh] overflow-y-auto">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            {visitStep === 1 && (
              <div>
                <div className="flex items-center gap-3 mb-4">
                  <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
                    <Shield className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">Verify Patient to Visit</h3>
                    <p className="text-xs text-slate-500">Step 1: Enter details provided by the family</p>
                  </div>
                </div>

                <form onSubmit={handleVerifyPatient} className="space-y-3">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">
                      Patient MRN or Full Name
                    </label>
                    <input
                      type="text"
                      required
                      value={visitLookupId}
                      onChange={(e) => {
                        setVisitLookupId(e.target.value);
                        setSelectedAdmitted(null);
                      }}
                      placeholder="e.g. SC-0001 or Jean Uwimana"
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">
                      Patient Phone Number or Family Visit PIN
                    </label>
                    <input
                      type="text"
                      required
                      value={visitVerifySecret}
                      onChange={(e) => {
                        setVisitVerifySecret(e.target.value);
                        setSelectedAdmitted(null);
                      }}
                      placeholder="e.g. 0780000001 or FAM-1001"
                      className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    />
                  </div>

                  {visitLookupError && (
                    <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl font-medium">
                      {visitLookupError}
                    </div>
                  )}

                  {!selectedAdmitted && (
                    <button
                      type="submit"
                      className="w-full py-3 bg-slate-900 hover:bg-slate-800 text-white font-bold rounded-xl text-sm transition"
                    >
                      Verify Patient & Check Slots
                    </button>
                  )}
                </form>

                {/* Verified Single Patient Card (Only shown after 2-factor match) */}
                {selectedAdmitted && (
                  <div className="mt-4 p-4 rounded-2xl border border-emerald-300 bg-emerald-50/70 space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                        <span className="text-sm font-bold text-slate-900">{selectedAdmitted.name}</span>
                      </div>
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">
                        {selectedAdmitted.slotsLeft} slot{selectedAdmitted.slotsLeft > 1 ? 's' : ''} left
                      </span>
                    </div>
                    <p className="text-xs text-slate-700">
                      {selectedAdmitted.ward} · {selectedAdmitted.floor} · <strong>{selectedAdmitted.bed}</strong>
                    </p>
                    <button
                      type="button"
                      onClick={() => setVisitStep(2)}
                      className="mt-2 w-full py-3 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-xl text-sm transition shadow-md"
                    >
                      Continue to Visiting Slot →
                    </button>
                  </div>
                )}

                <div className="mt-4 p-3 bg-blue-50 text-blue-900 rounded-2xl text-[11px] leading-relaxed">
                  <strong>Privacy Protected:</strong> Patient records are hidden from public view. Only visitors with the patient&apos;s MRN/Name and Phone or Family PIN can book a pass.
                  <span className="block mt-1 font-mono text-[10px] text-blue-700">
                    Demo Test: SC-0001 + 0780000001 (or FAM-1001)
                  </span>
                </div>
              </div>
            )}

            {visitStep === 2 && (
              <form onSubmit={handleVisitSubmit} className="space-y-4">
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center font-bold">
                    2
                  </div>
                  <div>
                    <h3 className="text-lg font-bold text-slate-900">Choose Time & Visitor Info</h3>
                    <p className="text-xs text-slate-500">
                      Visiting <strong>{selectedAdmitted?.name}</strong> ({selectedAdmitted?.bed})
                    </p>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Visit Date</label>
                    <input
                      type="date"
                      required
                      value={visitDate}
                      onChange={(e) => setVisitDate(e.target.value)}
                      className="w-full p-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Number of Visitors</label>
                    <select
                      value={visitorCount}
                      onChange={(e) => setVisitorCount(Number(e.target.value))}
                      className="w-full p-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    >
                      <option value={1}>1 Visitor</option>
                      {selectedAdmitted?.slotsLeft >= 2 && <option value={2}>2 Visitors (Max)</option>}
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1.5">Visiting Session</label>
                  <div className="space-y-2">
                    {[
                      '06:00 AM – 08:00 AM (Morning)',
                      '12:00 PM – 02:00 PM (Lunch)',
                      '05:00 PM – 07:30 PM (Evening)'
                    ].map((sess) => (
                      <button
                        key={sess}
                        type="button"
                        onClick={() => setVisitSession(sess)}
                        className={`w-full p-2.5 rounded-xl border text-left text-xs font-bold flex items-center justify-between transition ${
                          visitSession === sess
                            ? 'border-amber-500 bg-amber-50 text-amber-900'
                            : 'border-slate-200 text-slate-700 hover:border-slate-300'
                        }`}
                      >
                        <span>{sess}</span>
                        {visitSession === sess && <CheckCircle2 className="w-4 h-4 text-amber-600" />}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Your Full Name</label>
                    <input
                      type="text"
                      required
                      placeholder="e.g. Diane Mukamana"
                      value={visitorName}
                      onChange={(e) => setVisitorName(e.target.value)}
                      className="w-full p-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-bold text-slate-700 mb-1">Relationship</label>
                    <select
                      value={visitorRelation}
                      onChange={(e) => setVisitorRelation(e.target.value)}
                      className="w-full p-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                    >
                      <option>Family Member</option>
                      <option>Spouse</option>
                      <option>Parent / Child</option>
                      <option>Friend</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 mb-1">Phone Number (for SMS Pass)</label>
                  <input
                    type="tel"
                    required
                    placeholder="0780000000"
                    value={visitorPhone}
                    onChange={(e) => setVisitorPhone(e.target.value)}
                    className="w-full p-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-amber-500 outline-none"
                  />
                </div>

                <div className="flex gap-3 pt-1">
                  <button
                    type="button"
                    onClick={() => setVisitStep(1)}
                    className="px-4 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold rounded-xl text-sm"
                  >
                    Back
                  </button>
                  <button
                    type="submit"
                    className="flex-1 py-3 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-xl text-sm shadow-md"
                  >
                    Generate Visitor Pass
                  </button>
                </div>
              </form>
            )}

            {visitStep === 3 && (
              <div className="text-center py-2">
                <div className="w-14 h-14 rounded-full bg-emerald-100 text-emerald-600 mx-auto flex items-center justify-center mb-3">
                  <CheckCircle2 className="w-8 h-8" />
                </div>
                <h3 className="text-xl font-extrabold text-slate-900">Digital Visitor Pass Ready!</h3>
                <p className="text-xs text-slate-500 mt-1">
                  Show this pass at Hospital Security Gate #1 when you arrive.
                </p>

                {/* Official Visitor Pass Card */}
                <div className="my-5 p-5 bg-gradient-to-br from-slate-900 to-brand-950 text-white rounded-2xl text-left shadow-xl border border-slate-800 relative overflow-hidden">
                  <div className="flex items-start justify-between gap-4 border-b border-white/10 pb-3.5">
                    <div>
                      <span className="text-[10px] font-bold uppercase tracking-widest text-amber-400">
                        SmartCare Approved Ward Pass
                      </span>
                      <p className="text-2xl font-extrabold font-mono tracking-wider mt-0.5">{visitorPassCode}</p>
                    </div>
                    <div className="w-14 h-14 bg-white rounded-xl p-1.5 flex items-center justify-center text-slate-900 shrink-0">
                      <QrCode className="w-11 h-11" />
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3 mt-3.5 text-xs">
                    <div>
                      <span className="text-[10px] text-slate-400 uppercase block">Visiting Patient</span>
                      <span className="font-bold">{selectedAdmitted?.name}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-400 uppercase block">Ward & Bed</span>
                      <span className="font-bold text-amber-300">{selectedAdmitted?.floor} · {selectedAdmitted?.bed}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-400 uppercase block">Visitor ({visitorCount})</span>
                      <span className="font-bold">{visitorName} ({visitorRelation})</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-slate-400 uppercase block">Approved Slot</span>
                      <span className="font-bold">{visitDate} · {visitSession.split('(')[0].trim()}</span>
                    </div>
                  </div>
                </div>

                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={() => window.print()}
                    className="flex-1 py-3 bg-slate-100 hover:bg-slate-200 text-slate-800 font-bold rounded-xl text-xs flex items-center justify-center gap-2"
                  >
                    <Printer className="w-4 h-4" />
                    Print / Save Pass
                  </button>
                  <button
                    type="button"
                    onClick={() => setActiveModal(null)}
                    className="flex-1 py-3 bg-slate-900 hover:bg-slate-800 text-white font-bold rounded-xl text-xs"
                  >
                    Done
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL 4B: VISITING HOURS & WARD GUIDELINES (FROM NAVBAR) */}
      {/* ========================================================================= */}
      {activeModal === 'hours' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
                <Clock className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-slate-900">Hospital Visiting Hours</h3>
                <p className="text-xs text-slate-500">Daily inpatient visitation schedule</p>
              </div>
            </div>

            <div className="space-y-3 my-4 text-xs">
              <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200 flex justify-between items-center">
                <span className="font-bold text-slate-800">Morning Session</span>
                <span className="font-mono text-brand-700 font-bold">06:00 AM – 08:00 AM</span>
              </div>
              <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200 flex justify-between items-center">
                <span className="font-bold text-slate-800">Midday Lunch Session</span>
                <span className="font-mono text-brand-700 font-bold">12:00 PM – 02:00 PM</span>
              </div>
              <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200 flex justify-between items-center">
                <span className="font-bold text-slate-800">Evening Session</span>
                <span className="font-mono text-brand-700 font-bold">05:00 PM – 07:30 PM</span>
              </div>
            </div>

            <div className="p-3 bg-blue-50 text-blue-900 rounded-2xl text-[11px] leading-relaxed">
              <strong>Ward Policy:</strong> Maximum 2 visitors per patient bed at a time. Want to guarantee your entry? Book a Digital Visitor Pass from Quick Access.
            </div>

            <div className="mt-5 flex gap-3">
              <button
                onClick={openVisitModal}
                className="flex-1 py-3 bg-amber-500 hover:bg-amber-600 text-white font-bold rounded-xl text-xs transition"
              >
                Book a Visitor Pass →
              </button>
              <button
                onClick={() => setActiveModal(null)}
                className="px-5 py-3 bg-slate-100 hover:bg-slate-200 text-slate-800 font-bold rounded-xl text-xs"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL 5: STAFF & CLINICAL WORKSTATION SIGN IN */}
      {/* ========================================================================= */}
      {activeModal === 'login' && (
        <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 shadow-2xl border border-slate-100 relative max-h-[95vh] overflow-y-auto">
            <button
              onClick={() => setActiveModal(null)}
              className="absolute top-5 right-5 w-8 h-8 rounded-full bg-slate-100 text-slate-500 hover:bg-slate-200 flex items-center justify-center font-bold"
            >
              <X className="w-4 h-4" />
            </button>

            <div className="text-center mb-6">
              <div className="w-12 h-12 rounded-2xl bg-brand-50 text-brand-600 mx-auto flex items-center justify-center mb-2 shadow-xs">
                <Lock className="w-6 h-6" />
              </div>
              <h3 className="text-xl font-extrabold text-slate-900">Hospital Staff Portal</h3>
              <p className="text-xs text-slate-500">Sign in to your clinical or administrative workstation</p>
            </div>

            {loginError && (
              <div className="mb-4 p-3.5 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
                <span>⚠️</span>
                <span>{loginError}</span>
              </div>
            )}

            <form onSubmit={handleStaffLogin} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">Username</label>
                <div className="relative">
                  <User className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
                  <input
                    type="text"
                    required
                    value={loginUser}
                    onChange={(e) => setLoginUser(e.target.value)}
                    placeholder="e.g. superadmin, doctor, nurse"
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">Password</label>
                <div className="relative">
                  <Lock className="w-4 h-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
                  <input
                    type="password"
                    required
                    value={loginPass}
                    onChange={(e) => setLoginPass(e.target.value)}
                    placeholder="••••••••"
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-brand-500 outline-none"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={loginLoading}
                className="w-full py-3 bg-brand-600 hover:bg-brand-700 text-white font-bold rounded-xl text-sm shadow-md transition disabled:opacity-50"
              >
                {loginLoading ? 'Authenticating...' : 'Sign In to Workstation'}
              </button>
            </form>

            {/* Quick Demo Switcher */}
            <div className="mt-6 pt-5 border-t border-slate-100">
              <p className="text-[11px] font-bold text-slate-400 uppercase tracking-wider text-center mb-3">
                Quick Demo Switcher (Password: Demo@12345)
              </p>
              <div className="grid grid-cols-3 gap-1.5 text-xs">
                {demoRoles.map((r) => (
                  <button
                    key={r.id}
                    type="button"
                    onClick={() => selectDemoRole(r.id)}
                    className={`py-1.5 px-2 rounded-lg font-bold text-[11px] transition text-center ${
                      loginUser.toLowerCase() === r.id
                        ? 'bg-brand-600 text-white shadow-xs'
                        : 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                    }`}
                  >
                    {r.label}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
