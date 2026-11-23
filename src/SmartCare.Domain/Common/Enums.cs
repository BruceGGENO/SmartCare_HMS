namespace SmartCare.Domain.Common;

public enum Gender { Male, Female, Other }
public enum AllergySeverity { Mild, Moderate, Severe }
public enum HistoryKind { Condition, Surgery, Family }
public enum EncounterType { Outpatient, Inpatient, Emergency }
public enum AdmissionStatus { Admitted, DischargePending, Discharged }
public enum AppointmentStatus { Booked, CheckedIn, InConsultation, Completed, NoShow, Cancelled }
public enum MessageChannel { Sms, Email }
public enum MessageStatus { Pending, Sent, Failed }
public enum ShiftType { Morning, Evening, Night, OnCall }
public enum LeaveStatus { Pending, Approved, Rejected }
public enum PriceCategory { Consultation, Lab, Radiology, Procedure, Bed, Pharmacy, Package }
public enum InvoiceStatus { Open, PartiallyPaid, Paid, Void }
public enum PaymentMethod { Cash, Card, MobileMoney, Bank }
public enum ClaimStatus { Draft, Submitted, Approved, Rejected, Paid }
public enum StockMovementType { Receipt, Dispense, Adjustment, WriteOff, Transfer, Return }
public enum PrescriptionStatus { Active, PartiallyDispensed, Dispensed, Cancelled }
public enum PurchaseOrderStatus { Draft, Sent, Received }
public enum TestKind { Lab, Radiology }
public enum OrderStatus { Ordered, InProgress, Completed, Cancelled }
public enum SampleStatus { Collected, Received, InProcess, Completed }
public enum BedStatus { Available, Reserved, Occupied, Cleaning, OutOfService }
public enum TransferPriority { Routine, Urgent, Emergency }
public enum TransferStage { Requested, BedAssigned, Dispatched, Received, Completed, Cancelled }
/// <summary>As in the SRS (MOD-08). Final is the moment the doctor locks the clinical content; the checklist is created right after.</summary>
public enum DischargeStage { Draft, Final, ClearancesPending, Completed }
public enum ClearanceStatus { Pending, Cleared, Blocked }
public enum JourneyStation { Registration, Triage, Doctor, Lab, Pharmacy, Exit }
public enum JourneyEventType { Enter, Leave }
public enum ForecastMethod { Ml, Heuristic }
public enum LedgerEntryType { Payment, Usage, Adjustment }
public enum AuditAction { Create, Read, Update, Delete, Denied }
