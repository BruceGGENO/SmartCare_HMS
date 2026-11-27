# SmartCare UI (React + Vite + Tailwind CSS)

Hospital Management System Frontend for SmartCare Web API.

## Prerequisites
- Node.js 18+ (tested with Node 20 / 22)
- npm

## Setup & Run
1. Install dependencies:
   ```bash
   npm install
   ```
2. Start development server:
   ```bash
   npm run dev
   ```
   The UI runs on `http://localhost:5173`.

3. Ensure the ASP.NET Core Web API is running on `http://localhost:5000` (or update `VITE_API_URL` in `.env`).

## Features
- **JWT & Permission-Based Authorization**: Dynamically checks permissions against claims (`appointments.read`, `billing.pay`, `revenue.manage`, etc.)
- **Appointments & Patient Check-In** (MOD-02 & MOD-10)
- **Waiting Room Live Queue & Bottleneck Heatmap** (MOD-09)
- **Discharge Turnaround Tracker & Multi-Department Clearances** (MOD-08)
- **Billing & Itemized Invoicing** (MOD-04)
- **Revenue vs. Usage Prepaid Package Accounting** (MOD-12)
- **Mukunzi Document Storage & Audit Log Viewer** (MOD-06 & Audit)
